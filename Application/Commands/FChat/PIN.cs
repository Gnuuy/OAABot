using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Commands.FChat;

public class PIN : IFChatCommand
{
    [JsonIgnore]
    public string Command => "PIN";
}