using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules;

public class SetStatusModule : ModuleBase
{
    private readonly IModeratorRepository _moderatorRepository;
    private readonly IBotStatusRepository _statusRepository;

    public override bool HasHelpfulInfo => true;
    public override bool ModeratorOnly => true;
    public override string Command => "set_status";
    public override string CommandDescription => "(Moderators only) Set the bot's F-Chat status message. Persists across restarts.";

    public SetStatusModule(
        IOutboundMessageQueue outboundQueue,
        IModeratorRepository moderatorRepository,
        IBotStatusRepository statusRepository)
        : base(outboundQueue)
    {
        _moderatorRepository = moderatorRepository;
        _statusRepository = statusRepository;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;

        if (!await _moderatorRepository.IsModeratorAsync(character))
        {
            await SendPrivateAsync(character, "You do not have the privilege to use this command.");
            return;
        }

        var statusMessage = message.Message.Trim();
        if (statusMessage.Length == 0)
        {
            await SendPrivateAsync(character, "Usage: !set_status <message>");
            return;
        }

        await _statusRepository.SaveStatusMessageAsync(statusMessage);
        await SendStatusAsync(statusMessage);

        await SendPrivateAsync(character, "Status updated.");
    }
}
