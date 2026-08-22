using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class LIS : IFChatCommand
{
    [JsonIgnore]
    public string Command => "LIS";

    // Each entry is [name, gender, status, statusMessage] per F-Chat's initial user list format.
    public List<List<string>> characters { get; set; }
}
