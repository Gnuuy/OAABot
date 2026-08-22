using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class TPN : IFChatCommand
{
    [JsonIgnore]
    public string Command => "TPN";

    public string character { get; set; }
    public string status { get; set; }
}
