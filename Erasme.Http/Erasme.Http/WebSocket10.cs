// WebSocket10.cs
// 
//  Implement HTTP WebSocket for protocol version 10 and 13
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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net.Sockets;

namespace Erasme.Http
{
	public class WebSocket10: WebSocket
	{
		public enum Opcode: byte
		{
			Continuation = 0,
			Text = 1,
			Binary = 2,
			Reserved1 = 3,
			Reserved2 = 4,
			Reserved3 = 5,
			Reserved4 = 6,
			Reserved5 = 7,
			Close = 8,
			Ping = 9,
			Pong = 10,
			ReservedControl1 = 11,
			ReservedControl2 = 12,
			ReservedControl3 = 13,
			ReservedControl4 = 14,
			ReservedControl5 = 15
		}

		// GUID for the 10 WebSocket protocol
		const string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		byte[] readBuffer = new byte[4096];
		int readPos = 0;
		bool opcodeDone = false;
		Opcode opcode = Opcode.Text;
		bool fin = true;
		bool mask = false;
			
		bool payloadDone = false;
		int payloadBytes = 1;
		ulong payload = 0;
		ArraySegment<byte> buffer;
			
		bool maskingKeyDone = false;
		byte[] maskingKey = new byte[4];

		DateTime lastSeen = DateTime.Now;
		TimeSpan keepAliveInterval;

		internal WebSocket10(HttpContext context, TimeSpan keepAliveInterval): base(context)
		{
			State = WebSocketState.Open;
			this.keepAliveInterval = keepAliveInterval;
			lastSeen = DateTime.Now;
		}

		/// <summary>
        /// Takes care of the initial handshaking between the the client and the server
        /// </summary>
        internal static async Task<WebSocket> AcceptAsync(HttpContext context, TimeSpan keepAliveInterval)
        {
			byte[] buffer;

			// handle key
			SHA1 sha1 = SHA1.Create();
			byte[] digest = sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(context.Request.Headers["sec-websocket-key"]+GUID));
			string acceptKey = Convert.ToBase64String(digest);

			MemoryStream memStream = new MemoryStream();
			buffer = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\nupgrade: websocket\r\nconnection: upgrade\r\nsec-websocket-accept: "+acceptKey+"\r\nsec-websocket-location: ws://"+(context.Request.Headers["host"]+context.Request.AbsolutePath)+"\r\n\r\n");
			memStream.Write(buffer, 0, buffer.Length);
			memStream.Seek(0, SeekOrigin.Begin);

			await memStream.CopyToAsync(context.Client.Stream);
			await context.Client.Stream.FlushAsync();

			context.WebSocket = new WebSocket10(context, keepAliveInterval);
			return context.WebSocket;
        }

		public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
		{
			if(State == WebSocketState.Open) {
				byte[] buffer;
				if(statusDescription != null)
					buffer = Encoding.UTF8.GetBytes(statusDescription);
				else
					buffer = new byte[0];
				await SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Close, true, cancellationToken);
			}
			if(State == WebSocketState.CloseReceived) {
				byte[] buffer;
				if(statusDescription != null)
					buffer = Encoding.UTF8.GetBytes(statusDescription);
				else
					buffer = new byte[0];
				await SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Close, true, cancellationToken);
			}
		}

		public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
		{
			if(State == WebSocketState.Closed)
				return null;

			Task<int> readTask = null;
			this.buffer = buffer;
			while(true) {
				bool pingNeeded = false;
				int size = 0;
				try {
					Task timeoutTask = Task.Delay(keepAliveInterval);
					if(readTask == null)
						readTask = Context.Client.Stream.ReadAsync(readBuffer, 0, readBuffer.Length);

					Task resTask = await Task.WhenAny(timeoutTask, readTask);

					if(resTask == timeoutTask) {
						// no message was started
						if(!opcodeDone) {
							TimeSpan deltaTime = DateTime.Now - lastSeen;

							// no message received no even a pong, close the connection
							if(deltaTime.TotalSeconds > keepAliveInterval.TotalSeconds * 2) {
								Context.Client.Stream.Close();
								State = WebSocketState.Closed;
								return null;
							}
							// time for a ping
							else {
								pingNeeded = true;
							}
						}
						// message in progress but too slow to come
						// in a keep alive interval
						// close the connection
						else {
							Context.Client.Stream.Close();
							State = WebSocketState.Closed;
							return null;
						}
					}
					else {
						size = readTask.Result;
						readTask = null;
					}
				}
				catch(Exception) {
					Context.Client.Stream.Close();
					State = WebSocketState.Closed;
					return null;
				}

				if(pingNeeded) {
					await SendPingAsync();
					continue;
				}
				if(size == 0) {
					State = WebSocketState.Closed;
					return null;
				}

				lastSeen = DateTime.Now;

				for(int i = 0; i < size; i++) {
					// read the opcode part
					if(!opcodeDone) {
						fin = ((readBuffer[i] & (1 << 7)) != 0);
						opcode = (Opcode)(readBuffer[i] & 0xf);
						opcodeDone = true;
					}
					// read the payload part
					else if(!payloadDone) {
						if(readPos == 0) {
							payload = 0;
							mask = ((readBuffer[i] & 128) != 0);
							if((readBuffer[i] & 127) == 127) {
								payloadBytes = 8;
								readPos++;
							}
							else if((readBuffer[i] & 127) == 126) {
								payloadBytes = 2;
								readPos++;
							}
							else {
								payloadBytes = 1;
								payload = (UInt64)(readBuffer[i] & 127);
								payloadDone = true;
								if(!mask && (payload == 0)) {
									readPos = 0;
									opcodeDone = false;
									payloadDone = false;
									maskingKeyDone = false;

									WebSocketReceiveResult result = await BuildResultAsync();
									if(result != null)
										return result;
								}
							}
						}
						else {
							payload <<= 8;
							payload += readBuffer[i];
							if(readPos >= payloadBytes) {
								payloadDone = true;
								readPos = 0;											
								if(!mask && (payload == 0)) {
									readPos = 0;
									opcodeDone = false;
									payloadDone = false;
									maskingKeyDone = false;

									WebSocketReceiveResult result = await BuildResultAsync();
									if(result != null)
										return result;
								}
							}
							else
								readPos++;
						}
					}
					// read the masking key if needed
					else if(mask && !maskingKeyDone) {
						maskingKey[readPos] = readBuffer[i];
						if(++readPos >= 4) {
							maskingKeyDone = true;
							readPos = 0;										
							if(payload == 0) {
								readPos = 0;
								opcodeDone = false;
								payloadDone = false;
								maskingKeyDone = false;

								WebSocketReceiveResult result = await BuildResultAsync();
								if(result != null)
									return result;
							}
						}
					}
					// else read frame data
					else {
						if(mask)
							buffer.Array[buffer.Offset + readPos] = (byte)(readBuffer[i] ^ maskingKey[readPos % 4]);
						else
							buffer.Array[buffer.Offset + readPos ] = readBuffer[i];
						readPos++;
						if((UInt64)readPos >= payload) {
							// frame done
							readPos = 0;
							opcodeDone = false;
							payloadDone = false;
							maskingKeyDone = false;

							WebSocketReceiveResult result = await BuildResultAsync();
							if(result != null)
								return result;
						}
					}
				}
			}
		}

		async Task<WebSocketReceiveResult> BuildResultAsync()
		{
			if(opcode == Opcode.Ping) {
				// send pong
				await SendPongAsync(buffer.Array, (int)buffer.Offset, (int)payload);
				return null;
			}
			else if(opcode == Opcode.Pong) {
				return null;
			}
			else if(opcode == Opcode.Close) {
				string closeStatusDescription = null;
				WebSocketCloseStatus closeStatus = WebSocketCloseStatus.Empty;
				if(payload >= 2) {
					int code = (buffer.Array[buffer.Offset] << 8) | buffer.Array[buffer.Offset + 1];
					closeStatus = (WebSocketCloseStatus)code;
					if(payload > 2)
						closeStatusDescription = Encoding.UTF8.GetString(buffer.Array, (int)(buffer.Offset + 2), (int)(payload - 2));
				}
				if(State == WebSocketState.CloseSent) {
					State = WebSocketState.Closed;
					return null;
				}
				else {
					State = WebSocketState.CloseReceived;
					return new WebSocketReceiveResult((int)payload, WebSocketMessageType.Close, true, closeStatus, closeStatusDescription);
				}
			}
			else if(opcode == Opcode.Text)
				return new WebSocketReceiveResult((int)payload, WebSocketMessageType.Text, fin, null, null);
			else if(opcode == Opcode.Binary)
				return new WebSocketReceiveResult((int)payload, WebSocketMessageType.Binary, fin, null, null);
			else if(opcode == Opcode.Continuation) {
				// TODO
				return null;
			}
			else
				throw new Exception("WebSocket opcode not handled");
		}

		public override async Task SendAsync(
			ArraySegment<byte> buffer, WebSocketMessageType messageType,
			bool endOfMessage, CancellationToken cancellationToken)
		{
			byte[] headerBuffer = new byte[4];
			Opcode opcode = Opcode.Text;
			if(messageType == WebSocketMessageType.Text)
				opcode = Opcode.Text;
			else if(messageType == WebSocketMessageType.Binary)
				opcode = Opcode.Binary;
			else if(messageType == WebSocketMessageType.Close) {
				opcode = Opcode.Close;
				if(State == WebSocketState.CloseReceived)
					State = WebSocketState.Closed;
				else
					State = WebSocketState.CloseSent;
			}

			headerBuffer[0] = (byte)opcode;
			headerBuffer[0] |= 128;

			if(buffer.Count < 126) {
				headerBuffer[1] = (byte)buffer.Count;
				await Context.Client.Stream.WriteAsync(headerBuffer, 0, 2);
			}
			else if(buffer.Count < 65536) {
				headerBuffer[1] = 126;
				headerBuffer[2] = (byte)((buffer.Count >> 8) & 0xff);
				headerBuffer[3] = (byte)(buffer.Count & 0xff);
				await Context.Client.Stream.WriteAsync(headerBuffer, 0, 4);
			}
			await Context.Client.Stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
			await Context.Client.Stream.FlushAsync();
		}

		Task SendPongAsync(byte[] data, int offset, int count)
		{
			byte[] buffer = new byte[2 + count];
			buffer[0] = (byte)Opcode.Pong;
			buffer[0] |= 128;
			buffer[1] = (byte)count;
			Array.Copy(data, offset, buffer, 2, count);
			return Context.Client.Stream.WriteAsync(buffer, 0, buffer.Length);
		}
		
		Task SendPingAsync()
		{
			byte[] buffer = new byte[2 + 6];
			Random rand = new Random();
			rand.NextBytes(buffer);
			buffer[0] = (byte)Opcode.Ping;
			buffer[0] |= 128;
			buffer[1] = 6;
			return Context.Client.Stream.WriteAsync(buffer, 0, buffer.Length);
		}
	}
}

