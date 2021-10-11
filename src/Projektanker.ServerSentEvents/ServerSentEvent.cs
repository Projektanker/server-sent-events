namespace Projektanker.ServerSentEvents
{
    public class ServerSentEvent
    {
        public string EventType { get; set; } = "message";
        public string Data { get; set; } = string.Empty;
    }
}