using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class PRI : IFChatCommand
{
    [JsonIgnore]
    public string Command => "PRI";
    
    public string character { get; set; }
    public string recipient { get; set; }
    public string message { get; set; }
}