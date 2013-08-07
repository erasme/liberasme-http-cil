// LengthLimitedStream.cs
// 
//  Define input Stream that limit the content that can be read to a given length.
//  This is used for HTTP content with a known Content-Length
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public class LengthLimitedStream: Stream, ISharedBufferStream
	{
		long position;
		long length;
		BufferContext bufferContext;

		public LengthLimitedStream(BufferContext bufferContext, long length)
		{
			this.bufferContext = bufferContext;
			this.length = length;
			position = 0;
		}

		public LengthLimitedStream(Stream stream, long length)
		{
			bufferContext = new BufferContext();
			bufferContext.Buffer = new byte[4096];
			bufferContext.Stream = stream;
			bufferContext.Count = 0;
			bufferContext.Offset = 0;
			this.length = length;
			position = 0;
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return false;
			}
		}

		public override long Length {
			get {
				return length;
			}
		}

		public override long Position {
			get {
				return position;
			}
			set {
				throw new NotSupportedException();
			}
		}

		public override int ReadByte()
		{
			if(length - position <= 0)
				return -1;
			if(bufferContext.Count == 0)
				bufferContext.Fill().Wait();
			if(bufferContext.Count == 0)
				return -1;
			else {
				position++;
				bufferContext.Count--;
				return bufferContext.Buffer[bufferContext.Offset++];
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Task<int> task = ReadAsync(buffer, offset, count);
			task.Wait();
			return task.Result;
		}

		public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int remains = (int)(length - position);
			if(remains <= 0)
				return 0;
			if(bufferContext.Count == 0)
				await bufferContext.Fill();
			int size = Math.Min(bufferContext.Count, Math.Min(count, remains));
			if(buffer != null)
				Buffer.BlockCopy(bufferContext.Buffer, bufferContext.Offset, buffer, offset, size);
			bufferContext.Offset += size;
			bufferContext.Count -= size;
			position += size;
			return size;
		}

		public async Task<ArraySegment<byte>> SharedBufferReadAsync(int count)
		{
			int remains = (int)(length - position);
			if(remains <= 0)
				return new ArraySegment<byte>(bufferContext.Buffer, 0, 0);
			if(bufferContext.Count == 0)
				await bufferContext.Fill();
			int size = Math.Min(bufferContext.Count, Math.Min(count, remains));
			ArraySegment<byte> segment = new ArraySegment<byte>(bufferContext.Buffer, bufferContext.Offset, size);
			bufferContext.Offset += size;
			bufferContext.Count -= size;
			position += size;
			return segment;
		}

		public void SharedBufferRewind(int count)
		{
			bufferContext.Offset -= count;
			bufferContext.Count += count;
			position -= count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
	}
}
