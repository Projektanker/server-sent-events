using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.Client
{
    public sealed class EventSource : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _ct;
        private readonly ServerSentEventBuilder _builder;
        private readonly EventSourceConnectionFactory _connectionFactory;
        private readonly IServerSentEventsSubscriber _subscriber;

        private StreamReader? _streamReader;

        public EventSource(EventSourceConnectionFactory connection, IServerSentEventsSubscriber subscriber)
        {
            _connectionFactory = connection;
            _subscriber = subscriber;

            _builder = new ServerSentEventBuilder();
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;

            Task.Run(Process, _cts.Token);
        }

        public ReadyState ReadyState { get; private set; }

        public void Dispose()
        {
            _cts.Cancel();
            _streamReader?.Dispose();
            _cts.Dispose();
        }

        private static string GetField(string value, int indexOfColon)
        {
            return value[..indexOfColon];
        }

        private static string GetValue(string value, int indexOfColon)
        {
            return value[(indexOfColon + 1)..].TrimStart();
        }

        private async Task<StreamReader> Connect()
        {
            var response = await _connectionFactory.Invoke(HttpCompletionOption.ResponseHeadersRead, _ct);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();

            return new StreamReader(stream, Encoding.UTF8);
        }

        private async Task Process()
        {
            while (true)
            {
                try
                {
                    ThrowIfCancellationRequested();

                    ReadyState = ReadyState.Connecting;
                    using (_streamReader = await Connect())
                    {
                        ReadyState = ReadyState.Open;
                        await _subscriber.OnOpen();

                        await HandleStream(_streamReader);
                    }
                }
                catch (OperationCanceledException)
                {
                    ReadyState = ReadyState.Closed;
                    throw;
                }
                catch (Exception exception)
                {
                    ReadyState = ReadyState.Connecting;
                    await _subscriber.OnError(exception);
                    await Task.Delay(3000);
                }
            }
        }

        // https://html.spec.whatwg.org/multipage/server-sent-events.html#event-stream-interpretation
        private async Task HandleStream(TextReader stream)
        {
            try
            {
                while (await stream.ReadLineAsync() is string line)
                {
                    if (line.Length == 0)
                    {
                        await DispatchEvent();
                    }
                    else if (line.StartsWith(':'))
                    {
                        // ignore line
                    }
                    else if (line.IndexOf(':', StringComparison.Ordinal) is int index && index > 0)
                    {
                        var field = GetField(line, index);
                        var value = GetValue(line, index);
                        ProcessField(field, value);
                    }
                    else
                    {
                        ProcessField(line);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                ThrowIfCancellationRequested();
                throw;
            }
        }

        private async Task DispatchEvent()
        {
            if (_builder.TryBuild(out var sse))
            {
                await _subscriber.OnMessage(sse);
            }

            _builder.Reset();
        }

        private void ProcessField(string field, string value = "")
        {
            if (field == "event")
            {
                _builder.EventType = value;
            }
            else if (field == "data")
            {
                _builder.Data.Add(value);
            }
        }

        private void ThrowIfCancellationRequested()
        {
            _ct.ThrowIfCancellationRequested();
        }
    }
}