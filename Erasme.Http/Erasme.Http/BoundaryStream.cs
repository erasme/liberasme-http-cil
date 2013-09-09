// BoundaryStream.cs
// 
//  Stream that limit the input to a given boundary string
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
	public class BoundaryStream: Stream
	{
		ISharedBufferStream stream;
		long position = 0;

		byte[] boundaryBytes;
		bool isEnd = false;
		bool isLastBoundary = false;

		int boundaryFlushPos = 0;
		int boundaryFlushLength = 0;
		int boundaryPos = 0;

		byte[] currentBuffer = null;
		int currentBufferCount = 0;
		int currentBufferOffset = 0;

		public BoundaryStream(ISharedBufferStream stream, string boundary)
		{
			this.stream = stream;
			boundaryBytes = Encoding.ASCII.GetBytes("\r\n--"+boundary);
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

		public bool IsLastBoundary {
			get {
				return isLastBoundary;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Task<int> task = ReadAsync(buffer, offset, count, CancellationToken.None);
			task.Wait();
			return task.Result;
		}

		bool TestBoundary(byte[] buffer, int offset, int count, out int used)
		{
			used = 0;
			for(int i = offset; i < count+offset; i++) {
				byte data = buffer[i];
				// test boundary match
				if(boundaryPos >= boundaryBytes.Length) {
					int delta = boundaryPos - boundaryBytes.Length;
					if(delta == 0) {
						if(data == 0x2d)
							isLastBoundary = true;
						else if(data != 0x0d)
							throw new Exception("Invalid boundary stream");
					}
					else if(delta == 1) {
						// normal boundary succeed
						if(!isLastBoundary && (data == 0x0a)) {
							used = boundaryPos+1;
							boundaryPos = 0;
							return true;
						}
						else if(!isLastBoundary || (isLastBoundary && (data != 0x2d)))
							throw new Exception("Invalid boundary stream");
					}
					else if(delta == 2) {
						if(data != 0x0d)
							throw new Exception("Invalid boundary stream");
					}
					else if(delta == 3) {
						// last boundary succeed
						if(data == 0x0a) {
							used = boundaryPos + 1;
							boundaryPos = 0;
							return true;
						}
						else
							throw new Exception("Invalid boundary stream");
					}
				}
				else {
					// boundary fails
					if(boundaryBytes[boundaryPos] != data) {
						boundaryPos = 0;
						return false;
					}
				}
				used++;
				boundaryPos++;
			}
			return false;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			while(true) {
				if(isEnd)
					return 0;
				// do we need to flush some boundary buffer ?
				if(boundaryFlushLength > 0) {
					int size = Math.Min(boundaryFlushLength, count);
					if(buffer != null)
						Buffer.BlockCopy(boundaryBytes, boundaryFlushPos, buffer, offset, size);
					boundaryFlushLength -= size;
					boundaryFlushPos += size;
					return size;
				}
				// do we need to load a new buffer ?
				if(currentBufferCount == 0) {
					ArraySegment<byte> segment = await stream.SharedBufferReadAsync(count);
					if(segment.Count == 0)
						throw new Exception("Invalid boundary stream");
					currentBuffer = segment.Array;
					currentBufferOffset = segment.Offset;
					currentBufferCount = segment.Count;
				}
				int used;
				// if a boundary match start on the previous buffer, finish the match
				if(boundaryPos > 0) {
					int lastBoundaryPos = boundaryPos;
					// ok, is was a boundary
					if(TestBoundary(currentBuffer, currentBufferOffset, currentBufferCount, out used)) {
						isEnd = true;
						// if we read too much, rewined
						if(currentBufferCount - used > 0)
							stream.SharedBufferRewind(currentBufferCount - used);
						return 0;
					}
					else {
						// it was a partial boundary
						if(boundaryPos > 0) {
							currentBuffer = null;
							currentBufferCount = 0;
							currentBufferOffset = 0;
							continue;
						}
						// it was not a boundary
						else {
							boundaryFlushLength = lastBoundaryPos + used;
							boundaryFlushPos = 0;
							boundaryPos = 0;
							currentBufferOffset += used;
							currentBufferCount -= used;
							// flush what we can
							int size = Math.Min(boundaryFlushLength, count);
							if(buffer != null)
								Buffer.BlockCopy(boundaryBytes, boundaryFlushPos, buffer, offset, size);
							boundaryFlushLength -= size;
							boundaryFlushPos += size;
							return size;
						}
					}
				}
				// go to the first possible boundary match
				int i = 0;
				int minSize = Math.Min(count, currentBufferCount);
				while(i < minSize) {
					if(currentBuffer[currentBufferOffset+i] == 0x0d) {
						// match succeed
						if(TestBoundary(currentBuffer, currentBufferOffset + i, currentBufferCount-i, out used)) {
							isEnd = true;
							int overflow = currentBufferCount - (i+used);
							// if we read too much, rewined
							if(overflow > 0)
								stream.SharedBufferRewind(overflow);
							// copy what we found before the boundary
							if(buffer != null)
								Buffer.BlockCopy(currentBuffer, currentBufferOffset, buffer, offset, i);
							return i;
						}
						else {
							// partial boundary match
							if(boundaryPos > 0) {
								// return the part before the match
								if(buffer != null)
									Buffer.BlockCopy(currentBuffer, currentBufferOffset, buffer, offset, i);
								currentBufferCount -= i + used;
								currentBufferOffset += used;
								return i;
							}
							// match fails continue search
							else {
								i++;
							}
						}
					}
					else {
						i++;
					}
				}
				// ok minSize buffer is clean, return it
				if(i == minSize) {
					if(buffer != null)
						Buffer.BlockCopy(currentBuffer, currentBufferOffset, buffer, offset, minSize);
					currentBufferOffset += minSize;
					currentBufferCount -= minSize;
					return minSize;
				}
			}
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

