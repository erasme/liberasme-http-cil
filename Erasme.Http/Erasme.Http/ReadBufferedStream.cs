// ReadBufferedStream.cs
// 
//  Stream to buffer the read of a given Stream. Similar to BufferedStream
//  but with improved performance for ReadByte.
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
	public class ReadBufferedStream: Stream
	{
		Stream stream;
		byte[] buffer;
		int bufferPos = 0;
		int bufferCount = 0;

		object instanceLock = new object();
		long writeCounter = 0;
		long readCounter = 0;

		public ReadBufferedStream(Stream stream, int bufferLength)
		{
			this.stream = stream;
			buffer = new byte[bufferLength];
		}

		public override bool CanTimeout {
			get {
				return stream.CanTimeout;
			}
		}

		public override int ReadTimeout {
			get {
				return stream.ReadTimeout;
			}
			set {
				stream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				return stream.WriteTimeout;
			}
			set {
				stream.WriteTimeout = value;
			}
		}

		public long ReadCounter {
			get {
				lock(instanceLock) {
					return readCounter;
				}
			}
		}

		public long WriteCounter {
			get {
				lock(instanceLock) {
					return writeCounter;
				}
			}
		}

		public override bool CanRead {
			get {
				return stream.CanRead;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return stream.CanWrite;
			}
		}

		public override long Length {
			get {
				throw new NotSupportedException();
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		void UpdateBuffer()
		{
			bufferCount = stream.Read(buffer, 0, buffer.Length);
			bufferPos = 0;
			lock(instanceLock) {
				readCounter += bufferCount;
			}
		}

		public override int ReadByte()
		{
			if(bufferCount - bufferPos <= 0)
				UpdateBuffer();
			if(bufferCount == 0)
				return -1;
			return buffer[bufferPos++];
		}
			 
		public override int Read(byte[] buffer, int offset, int count)
		{
			// first flush the buffer
			int bufferSize = bufferCount - bufferPos;
			int size;
			if(bufferSize > 0) {
				size = Math.Min(count, bufferSize);
				Array.Copy(this.buffer, bufferPos, buffer, offset, size);
				bufferPos += size;
			}
			else {
				size = stream.Read(buffer, offset, count);
				lock(instanceLock) {
					readCounter += size;
				}
			}
			return size;
		}
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			stream.Write(buffer, offset, count);
			lock(instanceLock) {
				writeCounter += count;
			}
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await stream.WriteAsync(buffer, offset, count, cancellationToken);
			lock(instanceLock) {
				writeCounter += count;
			}
		}

		public override void Flush()
		{
			stream.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return stream.FlushAsync(cancellationToken);
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

