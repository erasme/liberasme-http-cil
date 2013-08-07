// WebSocket.cs
// 
//  Abstract class for all HTTP WebSockets
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
using System.Threading;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public abstract class WebSocket: IDisposable
	{
		protected WebSocket(HttpContext context)
		{
			Context = context;
		}

		public HttpContext Context { get; internal set; }

		public static TimeSpan DefaultKeepAliveInterval {
			get {
				return TimeSpan.FromSeconds(10);
			}
		}

		public WebSocketState State { get; protected set; }

		public abstract Task<WebSocketReceiveResult> ReceiveAsync(
			ArraySegment<byte> buffer, CancellationToken cancellationToken);

		public abstract Task SendAsync(
			ArraySegment<byte> buffer,
			WebSocketMessageType messageType,
			bool endOfMessage, CancellationToken cancellationToken);

		public abstract Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);

		public void Dispose()
		{
		}
	}
}

