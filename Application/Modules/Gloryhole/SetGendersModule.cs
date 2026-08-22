using Application.Interfaces;
using Domain;
using Domain.Enums;

namespace Application.Modules.Gloryhole;

public class SetGendersModule : ModuleBase
{
    private static readonly string[] AllowedGenders =
        ["female", "male", "transgender", "herm", "shemale", "maleherm", "cuntboy", "unspecified"];

    private readonly IGloryholeManager _gloryholeManager;

    public override bool HasHelpfulInfo => true;
    public override string Command => "set_genders";
    public override string CommandDescription => "Set which genders your booth will service.";

    public SetGendersModule(IOutboundMessageQueue outboundQueue, IGloryholeManager gloryholeManager)
        : base(outboundQueue)
    {
        _gloryholeManager = gloryholeManager;
    }

    public override bool CanHandle(InboundMessage message) =>
        base.CanHandle(message) && message.Type == MessageType.Private;

    public override async Task HandleAsync(InboundMessage message)
    {
        var character = message.CharacterName!;
        var args = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length == 0)
        {
            await SendPrivateAsync(character,
                "Usage: !set_genders <female|male|transgender|herm|shemale|maleherm|cuntboy|unspecified|all>");
            return;
        }

        string? reply;

        lock (_gloryholeManager.SyncRoot)
        {
            var booth = _gloryholeManager.Booths.FirstOrDefault(b => b.IsOwner(character));
            if (booth is null)
            {
                reply = "You must be the owner of a booth to set genders.";
            }
            else
            {
                var normalized = args.Select(a => a.ToLowerInvariant().Replace("-", "").Replace(" ", "")).ToList();

                if (normalized.Any(x => x == "all"))
                {
                    foreach (var key in AllowedGenders)
                        booth.RemovePreferredGender(key);

                    reply = $"Booth #{booth.Number} preferences set to ({string.Join("/", booth.PreferredGenders)}).";
                }
                else
                {
                    foreach (var key in AllowedGenders)
                        booth.RemovePreferredGender(key);

                    var invalid = new List<string>();
                    foreach (var key in normalized)
                    {
                        if (AllowedGenders.Contains(key))
                            booth.AddPreferredGender(key);
                        else
                            invalid.Add(key);
                    }

                    reply = invalid.Count > 0
                        ? "Invalid gender(s): " + string.Join(", ", invalid)
                        : $"Booth #{booth.Number} preferences set to ({string.Join("/", booth.PreferredGenders)}).";
                }
            }
        }

        await SendPrivateAsync(character, reply);
    }
}
