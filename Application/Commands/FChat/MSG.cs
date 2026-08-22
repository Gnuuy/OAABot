using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class MSG : IFChatCommand
{
    [JsonIgnore]
    public string Command => "MSG";
    
    public string character { get; set; }
    public string message { get; set; }
    public string channel { get; set; }
}