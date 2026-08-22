using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class JCH : IFChatCommand
{
    [JsonIgnore]
    public string Command => "JCH";
    
    public string channel {get ; set;}
}