using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.Server
{
    public static class HeartbeatServerSentEventsSource
    {
        public static async IAsyncEnumerable<ServerSentEvent> Heartbeat([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            int counter = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return new ServerSentEvent
                {
                    EventType = "heartbeat",
                    Data = counter.ToString(CultureInfo.InvariantCulture),
                };

                counter++;

                await Task.Delay(1000, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}