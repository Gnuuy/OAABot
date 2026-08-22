using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class STA : IFChatCommand
{
    [JsonIgnore]
    public string Command => "STA";

    public string character { get; set; }
    public string status { get; set; }
    public string statusmsg { get; set; }
}
