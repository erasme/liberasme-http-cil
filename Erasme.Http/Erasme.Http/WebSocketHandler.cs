// WebSocketHandler.cs
// 
//  Base class for WebSocket handlers. This base class allow to write
//  WebSocket handlers by overriding event callbacks:
//    OnOpen, OnClose, OnMessage, OnError
//  This allow to have an API similar to the Javascript one.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Erasme.Http
{
	public class WebSocketHandler
	{
		struct SendMessage
		{
			public WebSocketMessageType Type;
			public byte[] Content;
		}

		object instanceLock = new object();
		Task sendTask = null;
		Queue<SendMessage> sendList = new Queue<SendMessage>();
		Exception error = null;

		public WebSocketHandler()
		{
			// set a default value
			MaxIncomingMessageSize = 4096;
		}

		public HttpServer Server {
			get {
				return WebSocket.Context.Client.Server;
			}
		}

		public int MaxIncomingMessageSize { get; set; }

		public Exception Error {
			get {
				lock(instanceLock) {
					return error;
				}
			}
			set {
				lock(instanceLock) {
					error = value;
				}
			}
		}

		public HttpContext Context { get; set; }

		public WebSocket WebSocket { get; private set; }

		public async void Close()
		{
			await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
		}

		public virtual void OnClose()
		{
		}

		public virtual void OnError()
		{
		}

		public virtual void OnMessage(string message)
		{
		}

		public virtual void OnMessage(byte[] message)
		{
		}

		public virtual void OnOpen()
		{
		}

		public void Send(string message)
		{
			// allow the HttpServer to control the message
			Server.WebSocketHandlerSend(this, message);
		}

		internal void SendInternal(string message)
		{
			SendMessage sendMessage = new SendMessage();
			sendMessage.Type = WebSocketMessageType.Text;
			sendMessage.Content = Encoding.UTF8.GetBytes(message);
			lock(instanceLock) {
				sendList.Enqueue(sendMessage);
				if(sendTask == null)
					sendTask = SendMessagesAsync();
			}
		}

		public void Send(byte[] message)
		{
			// allow the HttpServer to control the message
			Server.WebSocketHandlerSend(this, message);
		}

		internal void SendInternal(byte[] message)
		{
			SendMessage sendMessage = new SendMessage();
			sendMessage.Type = WebSocketMessageType.Binary;
			sendMessage.Content = message;
			lock(instanceLock) {
				sendList.Enqueue(sendMessage);
				if(sendTask == null)
					sendTask = SendMessagesAsync();
			}
		}

		async Task SendMessagesAsync()
		{
			while(true) {
				SendMessage message;
				lock(instanceLock) {
					if(sendList.Count > 0)
						message = sendList.Dequeue();
					else {
						sendTask = null;
						break;
					}
				}
				await WebSocket.SendAsync(
						new ArraySegment<byte>(message.Content),
						message.Type, true, CancellationToken.None);
			}
		}

		public async Task ProcessWebSocketRequestAsync(HttpContext context)
		{
			Context = context;
			WebSocket = await context.AcceptWebSocketAsync();
			byte[] buffer = new byte[MaxIncomingMessageSize];

			Server.OnWebSocketHandlerOpen(this);

			while(WebSocket.State == WebSocketState.Open) {

				WebSocketReceiveResult receiveResult = await WebSocket.ReceiveAsync(
					new ArraySegment<byte>(buffer), CancellationToken.None);
				if(receiveResult == null)
					break;

				if(receiveResult.MessageType == WebSocketMessageType.Close)
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
				else if(receiveResult.MessageType == WebSocketMessageType.Binary)
                    await WebSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Binary frame not supported", CancellationToken.None);
				else {
					int count = receiveResult.Count;
					bool tooBig = false;

                    while(!tooBig && (receiveResult.EndOfMessage == false)) {
						if(count >= buffer.Length) {
							string closeMessage = string.Format("Maximum message size: {0} bytes", buffer.Length);
							await WebSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage, CancellationToken.None);
						}
						else {
							receiveResult = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), CancellationToken.None);
							count += receiveResult.Count;
						}
                    }
					if(!tooBig) {
						string message = Encoding.UTF8.GetString(buffer, 0, count);
						Server.OnWebSocketHandlerMessage(this, message);
					}
				}
			}
			Server.OnWebSocketHandlerClose(this);
		}
	}
}

