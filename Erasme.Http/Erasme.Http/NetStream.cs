// NetStream.cs
// 
//  Convert a Socket to a Stream + provide some read/write counters.
//  Similar to NetworkStream but with some performance improvement
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public class NetStream: Stream
	{
		Socket socket;
		bool ownSocket;
		SocketAsyncEventArgs readEventArgs;
		SocketAsyncEventArgs writeEventArgs;

		long writeCounter = 0;
		long readCounter = 0;

		public NetStream(Socket socket, bool ownSocket)
		{
			SetSocket(socket);
			this.ownSocket = ownSocket;
			readEventArgs = new SocketAsyncEventArgs();
			readEventArgs.Completed += OnReveiceAsyncCompleted;
			writeEventArgs = new SocketAsyncEventArgs();
			writeEventArgs.Completed += OnSendAsyncCompleted;
		}

		public void SetSocket(Socket socket)
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			this.socket.Blocking = false;
			writeCounter = 0;
			readCounter = 0;
		}

		public override bool CanTimeout {
			get {
				return true;
			}
		}

		public override int ReadTimeout {
			get {
				return socket.ReceiveTimeout;
			}
			set {
				socket.ReceiveTimeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				return socket.SendTimeout;
			}
			set {
				socket.SendTimeout = value;
			}
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
				return true;
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

		public long ReadCounter {
			get {
				return Interlocked.Read(ref readCounter);
			}
		}

		public long WriteCounter {
			get {
				return Interlocked.Read(ref writeCounter);
			}
		}
			 
		public override int Read(byte[] buffer, int offset, int count)
		{
			Task<int> task = ReadAsync(buffer, offset, count);
			task.Wait();
			return task.Result;
		}
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			WriteAsync(buffer, offset, count).Wait();
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if(socket.Available > 0) {
				int size = socket.Receive(buffer, offset, Math.Min(socket.Available, count), SocketFlags.None);
				Interlocked.Add(ref readCounter, size);
				return Task<int>.FromResult(size);
			}
			else {
				TaskCompletionSource<int> source = new TaskCompletionSource<int>();
				readEventArgs.SetBuffer(buffer, offset, count);
				readEventArgs.UserToken = source;
				if(!socket.ReceiveAsync(readEventArgs)) {
					Interlocked.Add(ref writeCounter, readEventArgs.BytesTransferred);
					return Task<int>.FromResult(readEventArgs.BytesTransferred);
				}
				else {
					return source.Task;
				}
			}
		}

		void OnReveiceAsyncCompleted(object sender, SocketAsyncEventArgs e)
		{
			Interlocked.Add(ref readCounter, e.BytesTransferred);
			((TaskCompletionSource<int>)e.UserToken).SetResult(e.BytesTransferred);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int size = socket.Send(buffer, offset, count, SocketFlags.None);
			Interlocked.Add(ref writeCounter, size);
			if(size < count) {
				TaskCompletionSource<object> source = new TaskCompletionSource<object>(); 
				writeEventArgs.SetBuffer(buffer, offset+size, count-size);
				writeEventArgs.UserToken = source;
				if(!socket.SendAsync(writeEventArgs)) {
					Interlocked.Add(ref writeCounter, count-size);
					return Task<Object>.FromResult((Object)null);
				}
				else
					return source.Task;
			}
			else
				return Task<Object>.FromResult((Object)null);
		}

		void OnSendAsyncCompleted(object sender, SocketAsyncEventArgs e)
		{
			Interlocked.Add(ref writeCounter, e.BytesTransferred);
			((TaskCompletionSource<object>)e.UserToken).SetResult(null);
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

		public override void Close()
		{
			if(this.ownSocket)
				socket.Close();
		}
	}
}


