using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Erasme.Http
{
    public class ResponseOutputStream: Stream
    {
        HttpContext context;
        OutputChunkedStream chunkedStream;
        bool isStarted = false;

        public ResponseOutputStream(HttpContext context)
        {
            this.context = context;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return context.Client.Stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int ReadByte()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var task = StartAsync();
            task.Wait();
            chunkedStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await StartAsync();
            await chunkedStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
            if (isStarted)
                chunkedStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (isStarted)
                await chunkedStream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public Task CloseAsync()
        {
            return CloseAsync(CancellationToken.None);
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (isStarted)
                await chunkedStream.CloseAsync();
        }

        public override void Close()
        {
            CloseAsync().Wait();
        }

        async Task StartAsync()
        {
            if (isStarted)
                return;
            isStarted = true;
            chunkedStream = new OutputChunkedStream(context.Client.Stream);
            // send the corresponding header
            await HttpSendResponse.SendHeadersAsync(context);
            context.Response.Sent = true;
        }
    }
}
