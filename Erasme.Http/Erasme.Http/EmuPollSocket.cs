// EmuPollSocket.cs
// 
//  Provide a emulated WebSocket based on HTTP polling requests
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
using Erasme.Json;

namespace Erasme.Http
{
	public class EmuPollSocket: WebSocket
	{
		static Dictionary<string,EmuPollSocket> sockets = new Dictionary<string, EmuPollSocket>();
		DateTime lastSeen = DateTime.Now;
		TimeSpan keepAliveInterval;

		object instanceLock = new object();
		CancellationTokenSource receiveTokenSource = null;
		Queue<string> receiveQueue = new Queue<string>();
		Queue<string> sendQueue = new Queue<string>();

		internal EmuPollSocket(HttpContext context, TimeSpan keepAliveInterval): base(context)
		{
			this.keepAliveInterval = keepAliveInterval;
			string id;
			do {
				id = Guid.NewGuid().ToString();
			} while(sockets.ContainsKey(id));
			Id = id;
			sockets[id] = this;
			State = WebSocketState.Open;
			lastSeen = DateTime.Now;
		}

		public string Id { get; private set; }

		public override async Task<WebSocketReceiveResult> ReceiveAsync(
			ArraySegment<byte> buffer, CancellationToken cancellationToken)
		{
			while(true) {
				string message = null;
				lock(instanceLock) {
					if(receiveQueue.Count > 0)
						message = receiveQueue.Dequeue();
				}

				if(message != null) {
					byte[] msgBytes = Encoding.UTF8.GetBytes(message);
					Array.Copy(msgBytes, 0, buffer.Array, buffer.Offset, Math.Min(buffer.Count, msgBytes.Length));
					WebSocketReceiveResult result = new WebSocketReceiveResult(
					Math.Min(buffer.Count, msgBytes.Length), WebSocketMessageType.Text, true);
					return result;
				}
				else {
					lock(instanceLock) {
						receiveTokenSource = new CancellationTokenSource();
					}
					try {
						await Task.Delay(keepAliveInterval, receiveTokenSource.Token);
					}
					// if task was cancel, it is perhaps because we received a message
					catch(TaskCanceledException) {}
					lock(instanceLock) {
						receiveTokenSource = null;
					}
					// no poll seen since 2 keepalive interval, time to
					// close the connection
					if((DateTime.Now - lastSeen).Ticks >= 2*keepAliveInterval.Ticks)
						return null;
				}
			}
		}

		public override Task SendAsync(
			ArraySegment<byte> buffer,
			WebSocketMessageType messageType,
			bool endOfMessage, CancellationToken cancellationToken)
		{
			if(messageType == WebSocketMessageType.Text) {
				lock(instanceLock) {
					sendQueue.Enqueue(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));
				}
				return Task.FromResult<Object>(null);
			}
			else
				throw new NotImplementedException();
		}

		public override Task CloseAsync(
			WebSocketCloseStatus closeStatus, string statusDescription,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <summary>
        /// Takes care of the initial handshaking between the client and the server
        /// </summary>
        internal static async Task<WebSocket> AcceptAsync(HttpContext context, TimeSpan keepAliveInterval)
        {
			EmuPollSocket emuSocket = new EmuPollSocket(context, keepAliveInterval);
			JsonValue json = new JsonObject();
			json["status"] = "open";
			json["id"] = emuSocket.Id;
			json["keepAliveInterval"] = keepAliveInterval.TotalSeconds;

			context.Response.StatusCode = 200;
			context.Response.Headers["cache-control"] = "no-cache, must-revalidate";
			context.Response.Headers["connection"] = "close";
			context.Response.Content = new JsonContent(json);
			await HttpSendResponse.SendAsync(context);
			context.Client.Close();
			context.WebSocket = emuSocket;
			return context.WebSocket;
        }

		internal static EmuPollSocket GetEmuPollSocket(HttpContext context)
		{
			if(context.Request.QueryString.ContainsKey("socket") && 
			   (context.Request.QueryString["socket"] == "poll") &&
			   context.Request.QueryString.ContainsKey("command") &&
			   ((context.Request.QueryString["command"] == "poll") ||
			    (context.Request.QueryString["command"] == "send") ||
			    (context.Request.QueryString["command"] == "close")) &&
			   context.Request.QueryString.ContainsKey("id") &&
			   sockets.ContainsKey(context.Request.QueryString["id"]))
				return sockets[context.Request.QueryString["id"]];
			else
				return null;
		}

		internal Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.QueryString["command"] == "send") {
				context.Response.StatusCode = 200;
				context.Response.Headers["cache-control"] = "no-cache, must-revalidate";
				JsonValue json = new JsonObject();
				json["status"] = "open";
				context.Response.Content = new JsonContent(json);

				if(context.Request.QueryString.ContainsKey("messages")) {
					string message = Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.QueryString["messages"]));
					lock(instanceLock) {
						receiveQueue.Enqueue(message);
						if(receiveTokenSource != null)
							receiveTokenSource.Cancel();
					}
				}
			}
			else if(context.Request.QueryString["command"] == "poll") {
				lastSeen = DateTime.Now;
				context.Response.StatusCode = 200;
				context.Response.Headers["cache-control"] = "no-cache, must-revalidate";
				JsonValue json = new JsonObject();
				json["status"] = "open";
				JsonArray jsonMessages = new JsonArray();
				json["messages"] = jsonMessages;
				lock(instanceLock) {
					while(sendQueue.Count > 0)
						jsonMessages.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(sendQueue.Dequeue())));
				}
				context.Response.Content = new JsonContent(json);
			}
			else if(context.Request.QueryString["command"] == "close") {
				// TODO
			}
			return Task.FromResult<Object>(null);
		}
	}
}

