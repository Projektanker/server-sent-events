using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.Client
{
    public delegate Task<HttpResponseMessage> EventSourceConnectionFactory(
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken);
}