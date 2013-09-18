// HttpServerRequest.cs
// 
//  Define a HTTP request received by a HttpServerClient of an HttpServer.
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
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Erasme.Json;

namespace Erasme.Http
{
	public class HttpServerRequest
	{
		DateTime startTime;
		long startWriteCounter;
		long startReadCounter;
		HttpServerClient client;

		public HttpServerRequest(HttpServerClient client, string command, HttpHeaders headers, DateTime startTime, long startReadCounter)
		{
			this.client = client;
			this.startTime = startTime;
			this.startReadCounter = startReadCounter;
			this.startWriteCounter = client.WriteCounter;

			HttpUtility.ParseCommand(command, out method, out fullPath, out path, out queryString, out protocol);
			AbsolutePath = Path;

			// handle HTTP headers
			Headers = headers;
			// handle cookies
			if(Headers.ContainsKey("cookie"))
				Cookies = HttpUtility.ParseCookie(Headers["cookie"]);
			else
				Cookies = new Dictionary<string, string>();
			// handle body stream
			if(Headers.ContainsKey("content-length")) {
				long contentLength = Convert.ToInt64(Headers["content-length"]);
				InputStream = new LengthLimitedStream(client.BufferContext, contentLength);
			}
			else if(Headers.ContainsKey("transfer-encoding") && (Headers["transfer-encoding"].ToLower() == "chunked"))
				InputStream = new InputChunkedStream(client.BufferContext);
			else
				InputStream = new LengthLimitedStream(client.BufferContext, 0);
		}

		/// <summary>
		/// Gets the local end point of the HTTP request.
		/// </summary>
		/// <value>
		/// The local end point.
		/// </value>
		public EndPoint LocalEndPoint {
			get {
				return client.LocalEndPoint;
			}
		}

		/// <summary>
		/// Gets the remote end point of the HTTP request.
		/// </summary>
		/// <value>
		/// The remote end point.
		/// </value>
		public EndPoint RemoteEndPoint {
			get {
				return client.RemoteEndPoint;
			}
		}

		/// <summary>
		/// Gets all the HTTP headers.
		/// </summary>
		/// <value>
		/// The headers.
		/// </value>
		public HttpHeaders Headers { get; internal set; }

		string protocol;
		/// <summary>
		/// Gets the HTTP protocol (ex: HTTP/1.0, HTTP/1.1 ...)
		/// </summary>
		/// <value>
		/// The protocol.
		/// </value>
		public string Protocol {
			get {
				return protocol;
			}
		}

		string method;
		/// <summary>
		/// Gets the HTTP request method (GET, POST, PUT, DELETE...).
		/// </summary>
		/// <value>
		/// The method.
		/// </value>
		public string Method {
			get {
				return method;
			}
		}

		Dictionary<string,string> queryString;
		/// <summary>
		/// Get the HTTP GET parameters
		/// </summary>
		/// <value>
		/// The query string.
		/// </value>
		public Dictionary<string,string> QueryString {
			get {
				return queryString;
			}
		}

		/// <summary>
		/// Get the HTTP cookies
		/// </summary>
		/// <value>
		/// The cookies.
		/// </value>
		public Dictionary<string,string> Cookies { get; internal set; }

		string fullPath;
		/// <summary>
		/// Get HTTP request full path (with the query string)
		/// </summary>
		/// <value>
		/// The full path.
		/// </value>
		public string FullPath {
			get {
				return fullPath;
			}
		}

		string path;
		/// <summary>
		/// Get the HTTP path (without the query string).
		/// This path can be relative if modified by an HttpHandler
		/// like PathMapper
		/// </summary>
		/// <value>
		/// The path.
		/// </value>
		public string Path {
			get {
				return path;
			}
			set {
				path = value;
			}
		}

		/// <summary>
		/// Get the absolute HTTP path. This value is never modified.
		/// </summary>
		/// <value>
		/// The absolute path.
		/// </value>
		public string AbsolutePath { get; private set; }

		/// <summary>
		/// If the HTTP request has a body (for POST or PUT request),
		/// gets the body input stream.
		/// </summary>
		/// <value>
		/// The input stream.
		/// </value>
		public Stream InputStream { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is web socket upgrade request.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is web socket request; otherwise, <c>false</c>.
		/// </value>
		public bool IsWebSocketRequest {
			get {
				return (
					// WebSocket protocol 10
					(Headers.ContainsKey("upgrade") && Headers["upgrade"].ToLower() == "websocket") ||
					// polling emulated WebSocket
					(QueryString.ContainsKey("socket") && (QueryString["socket"] == "poll") &&
					 QueryString.ContainsKey("command") && (QueryString["command"] == "open")));
			}
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a string
		/// </summary>
		/// <returns>
		/// The string.
		/// </returns>
		public string ReadAsString()
		{
			return Encoding.UTF8.GetString(ReadAsBytes());
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a string
		/// </summary>
		/// <returns>
		/// The string async.
		/// </returns>
		public async Task<string> ReadAsStringAsync()
		{
			return Encoding.UTF8.GetString(await ReadAsBytesAsync());
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a byte array
		/// </summary>
		/// <returns>
		/// The byte array.
		/// </returns>
		public byte[] ReadAsBytes()
		{
			byte[] buffer = new byte[InputStream.Length];
			int offset = 0;
			int count = 0;
			int size;
			do {
				size = InputStream.Read(buffer, offset, (int)InputStream.Length - count);
				count += size;
			} while((size > 0) &&(count < InputStream.Length));
			return buffer;
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a byte array
		/// </summary>
		/// <returns>
		/// The byte array async.
		/// </returns>
		public async Task<byte[]> ReadAsBytesAsync()
		{
			byte[] buffer = new byte[InputStream.Length];
			int offset = 0;
			int count = 0;
			int size;
			do {
				size = await InputStream.ReadAsync(buffer, offset, (int)InputStream.Length - count);
				count += size;
			} while((size > 0) &&(count < InputStream.Length));
			return buffer;
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a JsonValue
		/// </summary>
		/// <returns>
		/// The JsonValue.
		/// </returns>
		public JsonValue ReadAsJson()
		{
			return JsonValue.Parse(ReadAsString());
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a JsonValue
		/// </summary>
		/// <returns>
		/// The JsonValue async.
		/// </returns>
		public async Task<JsonValue> ReadAsJsonAsync()
		{
			return JsonValue.Parse(await ReadAsStringAsync());
		}

		public MultipartReader ReadAsMultipart()
		{
			return new MultipartReader(InputStream, Headers["content-type"]);
		}

		/// <summary>
		/// Gets the time when this request start processed.
		/// </summary>
		/// <value>
		/// The start time.
		/// </value>
		public DateTime StartTime {
			get {
				return startTime;
			}
		}

		/// <summary>
		/// Gets the write counter.
		/// </summary>
		/// <value>
		/// The write counter.
		/// </value>
		public long WriteCounter {
			get {
				return client.WriteCounter - startWriteCounter;
			}
		}

		/// <summary>
		/// Gets the read counter.
		/// </summary>
		/// <value>
		/// The read counter.
		/// </value>
		public long ReadCounter {
			get {
				return client.ReadCounter - startReadCounter;
			}
		}
	}
}
