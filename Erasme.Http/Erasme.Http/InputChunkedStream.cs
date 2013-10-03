// InputChunkedStream.cs
// 
//  Stream to read HTTP chunked streams
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
	public class InputChunkedStream: Stream, ISharedBufferStream
	{
		long position;
		int chunkLength;
		int chunkPos;
		bool end = false;
		bool firstChunk  = true;
		MemoryStream stringBuffer = new MemoryStream(128);

		BufferContext bufferContext;

		public InputChunkedStream(BufferContext bufferContext)
		{
			this.bufferContext = bufferContext;
			position = 0;
			end = false;
			chunkLength = 0;
			chunkPos = 0;
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
				throw new NotSupportedException();
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
			if(end) {
				return -1;
			}
			while(true) {
				if(chunkPos < chunkLength) {
					if(bufferContext.Count == 0) {
						bufferContext.Fill().Wait();
					}
					if(bufferContext.Count == 0) {
						end = true;
						return -1;
					}
					bufferContext.Offset++;
					bufferContext.Count--;
					position++;
					chunkPos++;
					return bufferContext.Buffer[bufferContext.Offset-1];
				}
				// load a new chunk
				else {
					stringBuffer.SetLength(0);
					Task<string> task = HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
					task.Wait();
					string chunkLine = task.Result;
					if(!firstChunk) {
						if(chunkLine != String.Empty)
							throw new Exception("Invalid chunked Stream");
						stringBuffer.SetLength(0);
						task = HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
						task.Wait();
						chunkLine = task.Result;
					}
					else
						firstChunk = false;
					if(chunkLine == null)
						throw new Exception("Invalid chunked Stream");
					chunkPos = 0;
					chunkLength = Int32.Parse(chunkLine, System.Globalization.NumberStyles.HexNumber);
					if(chunkLength == 0) {
						end = true;
						return -1;
					}
				}
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
			if(end)
				return 0;
			while(true) {
				if(chunkPos < chunkLength) {
					if(bufferContext.Count == 0)
						await bufferContext.Fill();
					if(bufferContext.Count == 0) {
						end = true;
						return 0;
					}
					int size = Math.Min(bufferContext.Count, Math.Min(count, chunkLength-chunkPos));
					if(buffer != null)
						Buffer.BlockCopy(bufferContext.Buffer, bufferContext.Offset, buffer, offset, size);
					bufferContext.Offset += size;
					bufferContext.Count -= size;
					position += size;
					chunkPos += size;
					return size;
				}
				// load a new chunk
				else {
					stringBuffer.SetLength(0);
					string chunkLine = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
					if(!firstChunk) {
						if(chunkLine != String.Empty)
							throw new Exception("Invalid chunked Stream");
						stringBuffer.SetLength(0);
						chunkLine = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
					}
					else
						firstChunk = false;
					if(chunkLine == null)
						throw new Exception("Invalid chunked Stream");
					chunkPos = 0;
					chunkLength = Int32.Parse(chunkLine, System.Globalization.NumberStyles.HexNumber);
					if(chunkLength == 0) {
						end = true;
						return 0;
					}
				}
			}
		}

		public async Task<ArraySegment<byte>> SharedBufferReadAsync(int count)
		{
			if(end)
				return new ArraySegment<byte>(bufferContext.Buffer, 0, 0);
			while(true) {
				if(chunkPos < chunkLength) {
					if(bufferContext.Count == 0)
						await bufferContext.Fill();
					if(bufferContext.Count == 0) {
						end = true;
						return new ArraySegment<byte>(bufferContext.Buffer, 0, 0);
					}
					int size = Math.Min(bufferContext.Count, Math.Min(count, chunkLength-chunkPos));
					ArraySegment<byte> segment = new ArraySegment<byte>(bufferContext.Buffer, bufferContext.Offset, size);
					bufferContext.Offset += size;
					bufferContext.Count -= size;
					position += size;
					chunkPos += size;
					return segment;
				}
				// load a new chunk
				else {
					stringBuffer.SetLength(0);
					string chunkLine = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
					if(!firstChunk) {
						if(chunkLine != String.Empty)
							throw new Exception("Invalid chunked Stream");
						stringBuffer.SetLength(0);
						chunkLine = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
					}
					else
						firstChunk = false;
					if(chunkLine == null)
						throw new Exception("Invalid chunked Stream");
					chunkPos = 0;
					chunkLength = Int32.Parse(chunkLine, System.Globalization.NumberStyles.HexNumber);
					if(chunkLength == 0) {
						end = true;
						return new ArraySegment<byte>(bufferContext.Buffer, 0, 0);
					}
				}
			}
		}

		public void SharedBufferRewind(int count)
		{
			bufferContext.Offset -= count;
			bufferContext.Count += count;
			position -= count;
			chunkPos -= count;
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
