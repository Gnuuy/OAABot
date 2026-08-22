using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class ERR : IFChatCommand
{
    [JsonIgnore] public string Command => "ERR";
    
    public string message { get; set; }
    public int number { get; set; }
}