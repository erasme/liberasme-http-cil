// HttpServerClient.cs
// 
//  A connection client of the HttpServer.
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone - 2015 Metropole de Lyon
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
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Erasme.Http
{
	public class HttpServerClient
	{
		HttpServer server;
		Socket socket;
		NetStream stream;
		Stream dataStream;
		WebSocket webSocket = null;
		DateTime startTime = DateTime.Now;
		EndPoint remoteEndPoint;
		EndPoint localEndPoint;
		long requestCounter = 0;
		BufferContext bufferContext;
		MemoryStream stringBuffer = new MemoryStream(128);

		object instanceLock = new object();
		HttpContext context = null;

		public HttpServerClient(HttpServer server, Socket socket)
		{
			this.server = server;
			this.socket = socket;
			this.socket.NoDelay = true;
			this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);

			requestCounter = 0;
			stream = new NetStream(socket, true);
			localEndPoint = socket.LocalEndPoint;
			remoteEndPoint = socket.RemoteEndPoint;
			KeepAliveCountdown = 100;
			KeepAliveTimeout = 10;

			// handle HTTPS
			if(server.ServerCertificate != null) {
				SslStream sslStream = new SslStream(stream, true);
				sslStream.ReadTimeout = 0;
				sslStream.WriteTimeout = 0;
				sslStream.AuthenticateAsServer(
					server.ServerCertificate, false, SslProtocols.Tls, false);
				dataStream = sslStream;
			}
			else {
				dataStream = stream;
			}

			bufferContext = new BufferContext();
			bufferContext.Offset = 0;
			bufferContext.Count = 0;
			bufferContext.ReadCounter = 0;
			bufferContext.Buffer = new byte[4096];
			bufferContext.Stream = dataStream;
		}

		public void Reset(Socket socket)
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);

			requestCounter = 0;
			stream.SetSocket(socket);
			localEndPoint = socket.LocalEndPoint;
			remoteEndPoint = socket.RemoteEndPoint;
			KeepAliveCountdown = 100;
			KeepAliveTimeout = 10;
			webSocket = null;
			startTime = DateTime.Now;

			// handle HTTPS
			if(server.ServerCertificate != null) {
				//dataStream.Close();
				SslStream sslStream = new SslStream(stream, true);
				sslStream.ReadTimeout = 0;
				sslStream.WriteTimeout = 0;
				sslStream.AuthenticateAsServer(
					server.ServerCertificate, false, SslProtocols.Tls, false);
				dataStream = sslStream;
				bufferContext.Stream = dataStream;
			}

			bufferContext.Offset = 0;
			bufferContext.Count = 0;
			bufferContext.ReadCounter = 0;
		}

		/// <summary>
		/// Gets the local end point of the HTTP client.
		/// </summary>
		/// <value>
		/// The local end point.
		/// </value>
		public EndPoint LocalEndPoint {
			get {
				return localEndPoint;
			}
		}

		/// <summary>
		/// Gets the remote end point of the HTTP client.
		/// </summary>
		/// <value>
		/// The remote end point.
		/// </value>
		public EndPoint RemoteEndPoint {
			get {
				return remoteEndPoint;
			}
		}

		internal BufferContext BufferContext {
			get {
				return bufferContext;
			}
		}

		internal MemoryStream StringBuffer {
			get {
				return stringBuffer;
			}
		}

		public long RequestCounter {
			get {
				return Interlocked.Read(ref requestCounter);
			}
		}

		public long WriteCounter {
			get {
				return stream.WriteCounter;
			}
		}

		public long ReadCounter {
			get {
				return stream.ReadCounter;
			}
		}

		internal Stream Stream {
			get {
				return dataStream;
			}
		}

		Socket Socket {
			get {
				return socket;
			}
		}

		public WebSocket WebSocket {
			get {
				lock(instanceLock) {
					return webSocket;
				}
			}
			internal set {
				lock(instanceLock) {
					webSocket = value;
				}
			}
		}

		public DateTime StartTime {
			get {
				return startTime;
			}
		}

		public HttpServer Server {
			get {
				return server;
			}
		}

		public HttpContext Context {
			get {
				lock(instanceLock) {
					return context;
				}
			}
			private set {
				lock(instanceLock) {
					context = value;
				}
			}
		}

		public int KeepAliveCountdown { get; set; }

		public int KeepAliveTimeout { get; set; }

		async Task<HttpServerRequest> ReadRequestAsync()
		{
			long startReadCounter = ReadCounter;

			stringBuffer.SetLength(0);
			string command = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
			if(command == null)
				return null;
			DateTime startTime = DateTime.Now;
			stringBuffer.SetLength(0);
			// read the headers
			HttpHeaders headers = new HttpHeaders();
			if(await Http.HttpUtility.ReadHeadersAsync(bufferContext, stringBuffer, headers)) {
				// handle Expect: 100-continue
				if(headers.ContainsKey("expect") && headers["expect"].StartsWith("100-continue")) {
					byte[] buffer = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
					await Stream.WriteAsync(buffer, 0, buffer.Length);
				}
				return new HttpServerRequest(this, command, headers, startTime, startReadCounter);
			}
			else
				return null;
		}

		public async Task ProcessAsync()
		{
			try {
				while(KeepAliveCountdown >= 0) {
					HttpServerRequest request;
					try {
						request = await ReadRequestAsync();
					}
					catch(IOException) {
						break;
					}
					if(request == null)
						break;

					try {
						// Process the request
						Context = new HttpContext(this, request);
						// test if it is an emulated socket
						EmuPollSocket emuSocket = EmuPollSocket.GetEmuPollSocket(Context);

						if(emuSocket != null)
							await emuSocket.ProcessRequestAsync(Context);
						else
							await Server.ProcessRequestAsync(Context);

						if((webSocket == null) || (emuSocket != null)) {
							// in case nobody send the response...
							if(!Context.Response.Sent)
								await HttpSendResponse.SendAsync(Context);
						}
						// process multiple request is not possible with websocket
						else
							break;
					}
					catch(IOException) {
						break;
					}
					finally {
						// free the response content
						if((Context.Response != null) && (Context.Response.Content != null))
							Context.Response.Content.Dispose();
					}
					Interlocked.Increment(ref requestCounter);
				}
			}
			catch(SocketException) {
			}
			finally {
				stream.Close();
			}
		}

		public void Close()
		{
			stream.Close();
		}
	}
}

