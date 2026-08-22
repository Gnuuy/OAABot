using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class IDN : IFChatCommand
{
    [JsonIgnore]
    public string Command => "IDN";
    
    public string method { get; set; }
    public string account { get; set; }
    public string ticket { get; set; }
    public string character { get; set; }
    public string cname { get; set; }
    public string cversion { get; set; }
}