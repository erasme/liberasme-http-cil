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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Erasme.Json;

namespace Erasme.Http
{
	public class HttpClient: IDisposable
	{
		string hostname;
		NetStream networkStream = null;
		SslStream sslStream = null;
		Stream stream = null;
		HttpClientResponse lastResponse = null;
		BufferContext bufferContext;
		MemoryStream stringBuffer = new MemoryStream(128);

		HttpClient()
		{
			bufferContext = new BufferContext();
			bufferContext.Offset = 0;
			bufferContext.Count = 0;
			bufferContext.ReadCounter = 0;
			bufferContext.Buffer = new byte[4096];
		}

		public static HttpClient Create(Uri url)
		{
			var client = new HttpClient();
			client.Open(url.Host, url.Port, url.Scheme == "https");
			return client;
		}

		public static HttpClient Create(string hostname, int port = 80, bool secure = false)
		{
			HttpClient client = new HttpClient();
			client.Open(hostname, port, secure);
			return client;
		}

		public static async Task<HttpClient> CreateAsync(Uri url)
		{
			var client = new HttpClient();
			await client.OpenAsync(url.Host, url.Port, url.Scheme == "https");
			return client;
		}

		public static async Task<HttpClient> CreateAsync(string hostname, int port = 80, bool secure = false)
		{
			HttpClient client = new HttpClient();
			await client.OpenAsync(hostname, port, secure);
			return client;
		}

		public static bool RemoteCertificateValidation(
			object sender, X509Certificate certificate, X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			// Accept all certificates
			return true;
		}

		public static X509Certificate  LocalCertificateSelection(
			object sender, string targetHost, X509CertificateCollection localCertificates,
			X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			// take the first one
			return localCertificates[0];
		}

		void Open(string hostname, int port, bool secure)
		{
			this.hostname = hostname;
			IPAddress[] addresses = Dns.GetHostAddresses(hostname);
			if(addresses.Length == 0)
				throw new Exception("Cant resolv '"+hostname+"'");
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(addresses[0], port);
			networkStream = new NetStream(socket, true);
			if(secure) {
				sslStream = new SslStream(networkStream, true, RemoteCertificateValidation, LocalCertificateSelection);
				sslStream.AuthenticateAsClient(hostname);
				stream = sslStream;
			}
			else {
				stream = networkStream;
			}
			bufferContext.Stream = stream;
		}

		async Task OpenAsync(string hostname, int port, bool secure)
		{
			this.hostname = hostname;
			IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostname);
			if (addresses.Length == 0)
				throw new Exception("Cant resolv '" + hostname + "'");
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(addresses[0], port);
			networkStream = new NetStream(socket, true);
			if (secure)
			{
				sslStream = new SslStream(networkStream, true, RemoteCertificateValidation, LocalCertificateSelection);
				await sslStream.AuthenticateAsClientAsync(hostname);
				stream = sslStream;
			}
			else
			{
				stream = networkStream;
			}
			bufferContext.Stream = stream;
		}

		void CleanResponse()
		{
			// finish reading the previous response input stream if needed
			if(lastResponse != null) {
				while(lastResponse.InputStream.Read(null, 0, int.MaxValue) > 0) {}
				lastResponse = null;
			}
		}

		async Task CleanResponseAsync()
		{
			// finish reading the previous response input stream if needed
			if (lastResponse != null)
			{
				while (await lastResponse.InputStream.ReadAsync(null, 0, int.MaxValue) > 0) { }
				lastResponse = null;
			}
		}

		public void SendRequest(HttpClientRequest request)
		{
			SendRequestAsync(request).Wait();
		}

		public async Task SendRequestAsync(HttpClientRequest request)
		{
			// finish reading the previous response input stream if needed
			await CleanResponseAsync();

			if (!request.Headers.ContainsKey("host"))
				request.Headers["host"] = hostname;
			if (!request.Headers.ContainsKey("user-agent"))
				request.Headers["user-agent"] = "liberasme-http-cil";
			if (!request.Headers.ContainsKey("accept"))
				request.Headers["accept"] = "*/*";
			await request.CopyToAsync(stream);
			request.Sent = true;
		}

		public HttpClientResponse GetResponse()
		{
			var task = GetResponseAsync();
			task.Wait();
			return task.Result;
		}

		public async Task<HttpClientResponse> GetResponseAsync()
		{
			stringBuffer.SetLength(0);
			string command = await HttpUtility.ReadLineAsync(bufferContext, stringBuffer);
			if (command == null)
				return null;
			stringBuffer.SetLength(0);
			// read the headers
			HttpHeaders headers = new HttpHeaders();
			if(!await HttpUtility.ReadHeadersAsync(bufferContext, stringBuffer, headers))
				return null;
			lastResponse = new HttpClientResponse(command, headers, bufferContext);
			return lastResponse;
		}

		public Task WaitForRemoteCloseAsync()
		{
			byte[] trash = new byte[1];
			return stream.ReadAsync(trash, 0, 1);
		}

		public void WaitForRemoteClose()
		{
			WaitForRemoteCloseAsync().Wait();
		}

		public void Close()
		{
			CloseAsync().Wait();
		}

		public async Task CloseAsync()
		{
			await CleanResponseAsync();
			if (sslStream != null)
				sslStream.Close();
			networkStream.Close();
		}

		public void Dispose()
		{
			if(networkStream != null) {
				Close();
				networkStream = null;
			}
		}

		public static async Task<string> GetAsStringAsync(string url)
		{
			Uri uri = new Uri(url);
			if ((uri.Scheme != "http") && (uri.Scheme != "https"))
				throw new Exception("Invalid Scheme");
			string res = null;
			using (var client = HttpClient.Create(uri.Host, uri.Port, uri.Scheme == "https"))
			{
				var request = new HttpClientRequest();
				request.Path = uri.PathAndQuery;
				await client.SendRequestAsync(request);
				var response = await client.GetResponseAsync();
				if(response.StatusCode == 200)
					res = await response.ReadAsStringAsync();
				await client.CloseAsync();
			}
			return res;
		}

		public static async Task<JsonValue> GetAsJsonAsync(string url)
		{
			var stringRes = await GetAsStringAsync(url);
			return (stringRes == null) ? null : JsonValue.Parse(stringRes);
		}
	}
}

