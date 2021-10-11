using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Projektanker.ServerSentEvents.Client
{
    public class ServerSentEventBuilder
    {
        private const string _defaultEventType = "message";

        public string EventType { get; set; } = string.Empty;

        public IList<string> Data { get; } = new List<string>();

        public bool TryBuild([NotNullWhen(true)] out ServerSentEvent? sse)
        {
            if (Data.Count == 0)
            {
                sse = null;
                return false;
            }

            sse = new ServerSentEvent
            {
                EventType = string.IsNullOrEmpty(EventType)
                    ? _defaultEventType
                    : EventType,
                Data = string.Join('\n', Data),
            };

            return true;
        }

        public void Reset()
        {
            EventType = _defaultEventType;
            Data.Clear();
        }
    }
}