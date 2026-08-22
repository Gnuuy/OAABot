using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class NLN : IFChatCommand
{
    [JsonIgnore]
    public string Command => "NLN";

    public string identity { get; set; }
    public string gender { get; set; }
    public string status { get; set; }
}
