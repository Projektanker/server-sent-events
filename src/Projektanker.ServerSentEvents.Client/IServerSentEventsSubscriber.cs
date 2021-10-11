using System;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.Client
{
    public interface IServerSentEventsSubscriber
    {
        Task OnOpen();

        Task OnMessage(ServerSentEvent serverSentEvent);

        Task OnError(Exception exception);
    }
}