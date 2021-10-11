using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Projektanker.ServerSentEvents.Server
{
    public class ServerSentEventsActionResult : IActionResult
    {
        private readonly ServerSentEventsWriter _writer;
        private readonly Action<HttpResponse>? _configureResponse;

        public ServerSentEventsActionResult(ServerSentEventsSource eventsSource, Action<HttpResponse>? configureResponse = null)
        {
            _writer = new ServerSentEventsWriter(eventsSource);
            _configureResponse = configureResponse;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            try
            {
                response.ContentType = "text/event-stream";
                response.StatusCode = (int)HttpStatusCode.OK;

                _configureResponse?.Invoke(response);

                await _writer.WriteAsync(response.Body, context.HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Connection closed
            }
            finally
            {
                await response.Body.DisposeAsync();
            }
        }
    }
}