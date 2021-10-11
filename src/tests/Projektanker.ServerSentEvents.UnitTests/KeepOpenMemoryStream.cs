using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Projektanker.ServerSentEvents.UnitTests
{
    public class KeepOpenMemoryStream : MemoryStream
    {
        private TaskCompletionSource? _keepOpen;

        public KeepOpenMemoryStream(byte[] buffer) : base(buffer)
        {
        }

        public KeepOpenMemoryStream(string content) : base(Encoding.UTF8.GetBytes(content))
        {
        }

        public override void Close()
        {
            base.Close();
            _keepOpen?.TrySetResult();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (_keepOpen is not null)
            {
                await _keepOpen.Task;
            }

            var result = await base.ReadAsync(destination, cancellationToken);

            if (Position == Length)
            {
                Position--;
                _keepOpen = new();
            }

            return result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_keepOpen is not null)
            {
                await _keepOpen.Task;
            }

            var result = await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            if (Position == Length)
            {
                Position--;
                _keepOpen = new();
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _keepOpen?.TrySetResult();
            }
        }
    }
}