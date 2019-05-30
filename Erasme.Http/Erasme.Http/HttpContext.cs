// HttpContext.cs
// 
//  Define the context of an HTTP request received by a HttpServerClient.
//  Provide the client, the request and the response.
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013-2014 Departement du Rhone
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
using System.Collections;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public class HttpContext
	{
		object instanceLock = new object();

		internal HttpContext(HttpServerClient client, HttpServerRequest request)
		{
			Client = client;
			Request = request;
			Data = new Hashtable();
			Response = new HttpServerResponse(this);
		}

		public HttpServerClient Client { get; internal set; }

		public HttpServerRequest Request { get; internal set; }

		public HttpServerResponse Response { get; internal set; }

		public Hashtable Data { get; internal set; }

		public string User { get; set; }

		public WebSocket WebSocket { 
			get {
				return Client.WebSocket;
			}
			internal set {
				Client.WebSocket = value;
			}
		}

		WebSocketHandler webSocketHandler = null;
		public WebSocketHandler WebSocketHandler { 
			get {
				lock(instanceLock) {
					return webSocketHandler;
				}
			}
			internal set {
				lock(instanceLock) {
					webSocketHandler = value;
				}
			}
		}

		public int KeepAliveCountdown {
			get {
				return Client.KeepAliveCountdown;
			}
			set {
				Client.KeepAliveCountdown = value;
			}
		}

		public int KeepAliveTimeout {
			get {
				return Client.KeepAliveTimeout;
			}
			set {
				Client.KeepAliveTimeout = value;
			}
		}

		public Task<WebSocket> AcceptWebSocketAsync()
		{
			return AcceptWebSocketAsync(TimeSpan.FromSeconds(10));
		}

		public Task<WebSocket> AcceptWebSocketAsync(TimeSpan keepAliveInterval)
		{
			// WebSocket protocol 10
			if(Request.Headers.ContainsKey("upgrade") && Request.Headers["upgrade"].ToLower() == "websocket")
				return WebSocket10.AcceptAsync(this, keepAliveInterval);
			// polling emulated WebSocket
			else if(Request.QueryString.ContainsKey("socket") && (Request.QueryString["socket"] == "poll") &&
			        Request.QueryString.ContainsKey("command") && (Request.QueryString["command"] == "open"))
				return EmuPollSocket.AcceptAsync(this, keepAliveInterval);
			else
				throw new Exception("Request is not a WebSocket");
		}

		public Task AcceptWebSocketRequestAsync(WebSocketHandler handler)
		{
			return handler.ProcessWebSocketRequestAsync(this);
		}

		public Task SendResponseAsync()
		{
			return HttpSendResponse.SendAsync(this);
		}

		public void SendResponse()
		{
			HttpSendResponse.SendAsync(this).Wait();
		}
	}
}

