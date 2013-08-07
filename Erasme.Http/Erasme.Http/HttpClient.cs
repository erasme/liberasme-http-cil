// HttpClient.cs
// 
//  Simple HTTP client to send a request
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
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Erasme.Http
{
	public class HttpClient: IDisposable
	{
		string hostname;
		NetworkStream networkStream = null;
		ReadBufferedStream stream;
		HttpClientResponse lastResponse = null;

		HttpClient()
		{
		}

		public static HttpClient Create(string hostname, int port)
		{
			HttpClient client = new HttpClient();
			client.Open(hostname, port);
			return client;
		}

		public void Open(string hostname, int port)
		{
			this.hostname = hostname;
			IPAddress[] addresses = Dns.GetHostAddresses(hostname);
			if(addresses.Length == 0)
				throw new Exception("Cant resolv '"+hostname+"'");
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(addresses[0], port);
			networkStream = new NetworkStream(socket, true);
			stream = new ReadBufferedStream(networkStream, 4096);
		}

		void CleanResponse()
		{
			// finish reading the previous response input stream if needed
			if(lastResponse != null) {
				byte[] trashBuffer = new byte[1024];
				while(lastResponse.InputStream.Read(trashBuffer, 0, trashBuffer.Length) > 0) {
				}
				lastResponse = null;
			}
		}

		public void SendRequest(HttpClientRequest request)
		{
			// finish reading the previous response input stream if needed
			CleanResponse();

			if(!request.Headers.ContainsKey("host"))
				request.Headers["host"] = hostname;
			request.CopyTo(networkStream);
			request.Sent = true;
		}

		public HttpClientResponse GetResponse()
		{
			string command = HttpUtility.ReadLine(networkStream);
			Dictionary<string,string> headers = HttpUtility.ReadHeaders(networkStream);

			lastResponse = new HttpClientResponse(command, headers, stream);
			return lastResponse;
		}

		public void Close()
		{
			CleanResponse();
			networkStream.Close();
		}

		public void Dispose()
		{
			if(networkStream != null) {
				Close();
				networkStream = null;
			}
		}
	}
}

