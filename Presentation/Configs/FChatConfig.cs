namespace Presentation.Configs;

public class FChatConfig
{
    public string WebSocketUrl { get; set; }
    public string UserAgent { get; set; }
    public string Account { get; set; }
    public string Password { get; set; }
    public string Character { get; set; }
    public string Version { get; set; }

    public string AutoJoinChannelCode { get; set; }
    
    public string TicketEndpoint { get; set; }
}