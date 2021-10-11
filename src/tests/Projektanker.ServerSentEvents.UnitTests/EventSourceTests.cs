using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Projektanker.ServerSentEvents.Client;
using Xunit;

namespace Projektanker.ServerSentEvents.UnitTests
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Only for unit test.")]
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1501:Statement should not be on a single line", Justification = "Only for unit test.")]
    public class EventSourceTests
    {
        private readonly TaskCompletionSource _tcs = new();

        private readonly Mock<IServerSentEventsSubscriber> _subscriber = new();

        public static IEnumerable<object[]> Examples => new object[][]
        {
            new object[]
            {
                "data: YHOO\ndata: +2\ndata: 10\n\n",
                new ServerSentEvent { EventType = "message", Data = "YHOO\n+2\n10" },
            },
            new object[]
            {
                "event: example2\ndata:example-data\n\n",
                new ServerSentEvent { EventType = "example2", Data = "example-data" },
            },
            new object[]
            {
                ": comment\nevent: example3\ndata: example-data\n\n",
                new ServerSentEvent { EventType = "example3", Data = "example-data" },
            },
            new object[]
            {
                ": comment\n\ndata: example-data\n\n",
                new ServerSentEvent { EventType = "message", Data = "example-data" },
            },
            new object[]
            {
                "data\n\n",
                new ServerSentEvent { EventType = "message", Data = string.Empty },
            },
            new object[]
            {
                "data\ndata\n\n",
                new ServerSentEvent { EventType = "message", Data = "\n" },
            },
            new object[]
            {
                "data:\n\n",
                new ServerSentEvent { EventType = "message", Data = string.Empty },
            },
        };

        [Theory(Timeout = 2000)]
        [MemberData(nameof(Examples))]
        public async Task Examples_Should_Dispatch_Event(string lines, ServerSentEvent expected)
        {
            // Arrange
            var connection = Create(lines);

            ServerSentEvent? sse = null;
            _subscriber.Setup(x => x.OnMessage(It.IsAny<ServerSentEvent>()))
                .Callback<ServerSentEvent>(arg => sse = arg)
                .Returns(Task.CompletedTask)
                .Callback(_tcs.SetResult);

            _subscriber.Setup(x => x.OnError(It.IsAny<Exception>()))
                .Callback(_tcs.SetResult);

            // Act
            var disposable = new EventSource(connection.Object, _subscriber.Object);
            await _tcs.Task;
            disposable.Dispose();
            await Task.Delay(100);

            // Assert
            sse.Should().NotBeNull();
            sse.Should().BeEquivalentTo(expected);

            _subscriber.Verify(x => x.OnOpen(), Times.Once);
            _subscriber.Verify(x => x.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact(Timeout = 5000)]
        public async Task EventSource_Should_Reconnect_If_Line_Is_Null_While_Not_Disposed()
        {
            // Arrange
            var connection = Create(string.Empty, keepOpen: false);

            var seq = new MockSequence();

            // initial connect
            _subscriber.InSequence(seq).Setup(x => x.OnOpen())
                .Returns(Task.CompletedTask);

            // second connect
            _subscriber.InSequence(seq).Setup(x => x.OnOpen())
                .Callback(_tcs.SetResult)
                .Returns(Task.Delay(1000));

            // Act
            using var disposable = new EventSource(connection.Object, _subscriber.Object);
            await _tcs.Task;

            // Assert
            _subscriber.Verify(x => x.OnOpen(), Times.Exactly(2));
            _subscriber.Verify(x => x.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact(Timeout = 5000)]
        public async Task EventSource_Should_Call_OnException_And_Retry_On_Stream_Exception_While_Not_Disposed()
        {
            // Arrange
            var seq = new MockSequence();
            var stream = new Mock<Stream>(MockBehavior.Strict);
            stream.SetupAllProperties();
            stream.SetReturnsDefault(true);
            var response = CreateHttpResponseMessage(stream.Object);
            var connection = FromResponse(() => response, seq);

            connection.InSequence(seq)
                .Setup(x => x(HttpCompletionOption.ResponseHeadersRead, It.Is<CancellationToken>(arg => arg != default)))
                .ReturnsAsync(CreateHttpResponseMessage("\n"));

            int onOpenCount = 0;
            _subscriber.Setup(x => x.OnOpen())
                .Callback(() => { if (++onOpenCount == 2) { _tcs.SetResult(); } });

            // Act
            using var disposable = new EventSource(connection.Object, _subscriber.Object);
            await _tcs.Task;

            // Assert
            _subscriber.Verify(x => x.OnError(It.IsAny<Exception>()), Times.Once);
            _subscriber.Verify(x => x.OnOpen(), Times.Exactly(2));
            connection.Verify(x => x(HttpCompletionOption.ResponseHeadersRead, It.Is<CancellationToken>(arg => arg != default)), Times.Exactly(2));
        }

        [Fact(Timeout = 4000)]
        public async Task EventSource_Should_Call_OnException_And_Retry_On_Connection_Exception()
        {
            // Arrange
            var exception = new Exception();
            var connection = new Mock<EventSourceConnectionFactory>();

            var seq = new MockSequence();
            connection.InSequence(seq)
                .Setup(x => x(HttpCompletionOption.ResponseHeadersRead, It.Is<CancellationToken>(arg => arg != default)))
                .ThrowsAsync(exception);

            connection.InSequence(seq)
                .Setup(x => x(HttpCompletionOption.ResponseHeadersRead, It.Is<CancellationToken>(arg => arg != default)))
                .Callback(_tcs.SetResult)
                .ThrowsAsync(exception);

            // Act
            using var disposable = new EventSource(connection.Object, _subscriber.Object);
            await _tcs.Task;
            await Task.Delay(100);

            // Assert
            _subscriber.Verify(x => x.OnError(exception), Times.Exactly(2));
        }

        private static HttpResponseMessage CreateHttpResponseMessage(string lines, bool keepOpen = true)
        {
            var stream = keepOpen
                ? new KeepOpenMemoryStream(Encoding.UTF8.GetBytes(lines))
                : new MemoryStream(Encoding.UTF8.GetBytes(lines));

            return CreateHttpResponseMessage(stream);
        }

        private static HttpResponseMessage CreateHttpResponseMessage(Stream stream)
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream),
            };
        }

        private static Mock<EventSourceConnectionFactory> Create(string lines, bool keepOpen = true)
        {
            return FromResponse(() => CreateHttpResponseMessage(lines, keepOpen));
        }

        private static Mock<EventSourceConnectionFactory> FromResponse(Func<HttpResponseMessage> resonse, MockSequence? sequence = null)
        {
            var mock = new Mock<EventSourceConnectionFactory>();

            sequence ??= new() { Cyclic = true };

            mock.InSequence(sequence)
                .Setup(x => x(HttpCompletionOption.ResponseHeadersRead, It.Is<CancellationToken>(arg => arg != default)))
                .ReturnsAsync(resonse);

            return mock;
        }
    }
}