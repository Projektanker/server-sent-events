using System.Collections.Generic;
using System.Threading;

namespace Projektanker.ServerSentEvents.Server
{
    public delegate IAsyncEnumerable<ServerSentEvent> ServerSentEventsSource(CancellationToken cancellationToken);
}