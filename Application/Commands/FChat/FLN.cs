using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class FLN : IFChatCommand
{
    [JsonIgnore]
    public string Command => "FLN";

    public string character { get; set; }
}
