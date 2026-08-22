using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Application.Commands.FChat;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Configs;

namespace Presentation.Clients;

public class FChatClient : IFChatClient
{
    // F-Chat enforces a hard limit of one message per second per connection to deter spam;
    // exceeding it gets the connection flood-flagged/disconnected. Every outgoing command -
    // IDN/JCH/PIN included, not just chat messages - funnels through the single queue below so
    // the limit is enforced globally instead of per call site, and bursts queue up instead of
    // racing each other for the socket.
    private static readonly TimeSpan MinSendInterval = TimeSpan.FromSeconds(1);

    private readonly ILogger<FChatClient> _logger;
    private readonly FChatConfig _config;
    private readonly IFChatService _fChatService;
    private readonly ICharacterPresenceTracker _presenceTracker;

    private readonly HttpClient _httpClient;
    private ClientWebSocket? _socket;

    private readonly Channel<PendingSend> _outgoing = Channel.CreateUnbounded<PendingSend>();

    private sealed record PendingSend(string Payload, TaskCompletionSource CompletionSource);

    public  FChatClient(ILogger<FChatClient> logger, IOptions<FChatConfig> config, HttpClient httpClient,  IFChatService fChatService, ICharacterPresenceTracker presenceTracker)
    {
        _fChatService = fChatService;
        _presenceTracker = presenceTracker;

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = (config ?? throw new ArgumentNullException(nameof(config))).Value;
        _httpClient = httpClient ?? throw new ArgumentException(nameof(httpClient));

        // Long-lived for the app's lifetime; survives across reconnects since it just reads
        // whatever _socket currently points at each time it's ready to send.
        _ = Task.Run(() => SendLoopAsync());
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        // No "already connected" short-circuit here: the caller (FChatWorker's reconnect loop)
        // only calls this after a previous connection has ended, and a socket left half-open by a
        // failed ticket/identify step would otherwise report State == Open and cause every
        // subsequent reconnect attempt to silently no-op forever.
        _socket?.Dispose();
        _socket = new ClientWebSocket();
        _presenceTracker.Clear();

        if (string.IsNullOrEmpty(_config.WebSocketUrl))
        {
            _logger.LogError("[F-Chat]: ChatConfig.WebSocketUrl is missing.");
            throw new InvalidOperationException("[F-Chat]: ChatConfig.WebSocketUrl is missing.");
        }
        
        var uri = new Uri(_config.WebSocketUrl);
        _logger.LogInformation("[F-Chat]: Connecting to F-Chat Web Socket at {uri}", uri);
        
        await  _socket.ConnectAsync(uri, cancellationToken);
        _logger.LogInformation("[F-Chat]: Connected to F-Chat websocket. State: {State}", _socket.State);
        
        string ticket = await RequestTicketAsync(cancellationToken);

        IDN payload = new IDN()
        {
            method = "ticket",
            ticket = ticket,
            account = _config.Account,
            character =  _config.Character,
            cversion = _config.Version,
            cname = _config.UserAgent
        };
        
        await SendCommandAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Queues a command to be sent over the F-Chat connection. The command is not necessarily
    /// sent immediately - it takes its place behind whatever else is already queued, and the
    /// returned task completes once it has actually gone out over the wire (or failed to).
    /// </summary>
    /// <exception cref="InvalidOperationException">The socket was not open when this command's turn to send came up.</exception>
    public async Task SendCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : IFChatCommand
    {
        var json = JsonSerializer.Serialize(command);
        var payload = $"{command.Command} {json}";
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

        await _outgoing.Writer.WriteAsync(new PendingSend(payload, completionSource), cancellationToken);
        await completionSource.Task;
    }

    // Single writer to the socket for the lifetime of the client: dequeues one queued command at
    // a time, waits out whatever remains of the 1s spacing since the last actual send, then sends.
    // Runs across reconnects - ConnectAsync only ever swaps _socket out from under it.
    private async Task SendLoopAsync()
    {
        var lastSendUtc = DateTime.MinValue;

        await foreach (var pending in _outgoing.Reader.ReadAllAsync())
        {
            var remainingDelay = MinSendInterval - (DateTime.UtcNow - lastSendUtc);
            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay);
            }

            try
            {
                if (_socket is null || _socket.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException("[F-Chat]: F-Chat Websocket is not opened.");
                }

                _logger.LogInformation("[F-Chat] Outgoing: {Payload}", pending.Payload);
                await _socket.SendAsync(
                    Encoding.UTF8.GetBytes(pending.Payload),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                pending.CompletionSource.TrySetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[F-Chat]: Failed to send queued command: {Payload}", pending.Payload);
                pending.CompletionSource.TrySetException(ex);
            }
            finally
            {
                // Count failed attempts against the pacing too, so a run of failures (e.g. while
                // disconnected) can't turn into a tight retry loop - it just settles into resending
                // at the same 1/sec cadence until the connection comes back.
                lastSendUtc = DateTime.UtcNow;
            }
        }
    }

    public async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_socket is null || _socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("[F-Chat]: WebSocket is not connected.");
        }

        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested &&
               _socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("[F-Chat]: Server closed the websocket.");
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var text = Encoding.UTF8.GetString(ms.ToArray());

            // F-Chat pings the connection with a bare PIN command and expects an immediate PIN
            // reply to prove the client is still alive; missing enough of these gets the
            // connection dropped, so this bypasses the normal module pipeline entirely.
            if (text.StartsWith("PIN", StringComparison.Ordinal))
            {
                try
                {
                    await SendCommandAsync(new PIN(), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[F-Chat]: Failed to respond to PIN keepalive.");
                }

                continue;
            }

            try
            {
                await _fChatService.HandleMessageAsync(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[F-Chat]: Failed while handling incoming message: {Message}", text);
            }
        }
    }

    public async Task AutoJoinChannelOnStartUpAsync(CancellationToken cancellationToken)
    {
        await SendCommandAsync(new JCH()
        {
            channel =  _config.AutoJoinChannelCode,
        },  cancellationToken);
    }


    // Helper Functions
    
    /// <summary>
    /// Fetches a ticket from F-List's HTTP endpoint
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>string.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<string> RequestTicketAsync(CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["account"] = _config.Account,
            ["password"] = _config.Password,
            ["no_characters"] = "true",
            ["no_friends"] = "true"
        });

        var response = await _httpClient.PostAsync(_config.TicketEndpoint, content);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send ticket. Error: {Error}", await response.Content.ReadAsStringAsync());
            throw new Exception("[F-Chat]: Failed to send ticket.");
        }
        _logger.LogInformation("[F-Chat]: Successfully sent ticket. Response: {Response}", await response.Content.ReadAsStringAsync());
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("ticket", out var ticketElement))
        {
            throw new InvalidOperationException("[F-Chat]: Ticket response did not contain 'ticket'.");
        }

        var ticket = ticketElement.GetString();

        if (string.IsNullOrWhiteSpace(ticket))
        {
            throw new InvalidOperationException("[F-Chat]: Ticket was null or empty.");
        }

        return ticket;
    }
}