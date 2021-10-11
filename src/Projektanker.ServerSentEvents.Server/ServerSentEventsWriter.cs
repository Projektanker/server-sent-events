using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.Server
{
    public class ServerSentEventsWriter
    {
        private readonly ServerSentEventsSource _eventsSource;

        public ServerSentEventsWriter(ServerSentEventsSource eventsSource)
        {
            _eventsSource = eventsSource;
        }

        public async Task WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            await using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            await foreach (var sse in _eventsSource(cancellationToken))
            {
                await WriteEventType(stream, sse.EventType);
                await WriteData(stream, sse.Data);
                await WriteEmptyLine(stream);
                await stream.FlushAsync(cancellationToken);
            }
        }

        private static async Task WriteAsync(Stream stream, string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(data);
        }

        private static async Task WriteEventType(Stream stream, string eventType)
        {
            await WriteAsync(stream, $"event: {eventType}\n");
        }

        private static async Task WriteData(Stream stream, string data)
        {
            var lines = data.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(value => $"data: {value}\n");

            foreach (var line in lines)
            {
                await WriteAsync(stream, line);
            }
        }

        private static async Task WriteEmptyLine(Stream stream)
        {
            await WriteAsync(stream, "\n");
        }
    }
}