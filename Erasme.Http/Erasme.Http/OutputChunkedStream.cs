// OutputChunkedStream.cs
// 
//  Write Stream to encode HTTP content in chunked mode (when the HTTP
//  body length is not known)
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
	public class OutputChunkedStream: Stream
	{
		Stream stream;
		static byte[] lineMarker = new byte[] { 0x0d, 0x0a };

		public OutputChunkedStream(Stream stream)
		{
			this.stream = stream;
		}

		public override bool CanTimeout {
			get {
				return stream.CanTimeout;
			}
		}

		public override int ReadTimeout {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
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

		public override bool CanRead {
			get {
				return false;
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
			byte[] marker = Encoding.ASCII.GetBytes(String.Format("{0:X}\r\n", count));
			stream.Write(marker, 0, marker.Length);
			if(count > 0)
				stream.Write(buffer, offset, count);
			stream.Write(lineMarker, 0, 2);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			byte[] marker = Encoding.ASCII.GetBytes(String.Format("{0:X}\r\n", count));
			await stream.WriteAsync(marker, 0, marker.Length, cancellationToken);
			if(count > 0)
				await stream.WriteAsync(buffer, offset, count, cancellationToken);
			await stream.WriteAsync(lineMarker, 0, 2, cancellationToken);
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

