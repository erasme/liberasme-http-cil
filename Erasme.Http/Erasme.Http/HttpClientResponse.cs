// HttpClientResponse.cs
// 
//  Define a HTTP response received by a HttpClient
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Erasme.Json;

namespace Erasme.Http
{
	public class HttpClientResponse
	{
		public HttpClientResponse(string status, HttpHeaders headers, BufferContext bufferContext)
		{
			HttpUtility.ParseStatus(status, out protocol, out statusCode, out statusDescription);
			Headers = headers;
			if(Headers.ContainsKey("cookie"))
				Cookies = HttpUtility.ParseCookie(Headers["cookie"]);
			else
				Cookies = new Dictionary<string, string>();

			if(Headers.ContainsKey("content-length")) {
				long contentLength = Convert.ToInt64(Headers["content-length"]);
				InputStream = new LengthLimitedStream(bufferContext, contentLength);
			}
			else if(Headers.ContainsKey("transfer-encoding") && (Headers["transfer-encoding"].ToLower() == "chunked"))
				InputStream = new InputChunkedStream(bufferContext);
			else
				InputStream = new LengthLimitedStream(bufferContext, 0);
		}

		/// <summary>
		/// Gets or sets the headers of the response.
		/// </summary>
		/// <value>
		/// The headers.
		/// </value>
		public HttpHeaders Headers { get; internal set; }

		/// <summary>
		/// Gets or sets the cookies to return with the response.
		/// </summary>
		/// <value>
		/// The cookies.
		/// </value>
		public Dictionary<string,string> Cookies { get; internal set; }

		string protocol;
		/// <summary>
		/// Gets the HTTP protocol.
		/// </summary>
		/// <value>
		/// The protocol.
		/// </value>
		public string Protocol {
			get {
				return protocol;
			}
		}

		int statusCode;
		/// <summary>
		/// Gets or sets the HTTP status code of the response.
		/// </summary>
		/// <value>
		/// The status code.
		/// </value>
		public int StatusCode {
			get {
				return statusCode;
			}
		}

		string statusDescription;
		/// <summary>
		/// Gets or sets the HTTP status description of the response.
		/// </summary>
		/// <value>
		/// The status description.
		/// </value>
		public string StatusDescription {
			get {
				return statusDescription;
			}
		}

		public string Status {
			get {
				if(StatusDescription == String.Empty)
					return StatusCode+" "+HttpUtility.GetStatusDetail(StatusCode);
				else
					return StatusCode+" "+StatusDescription;
			}
		}

		/// <summary>
		/// If the HTTP request has a body (for POST or PUT request),
		/// gets the body input stream.
		/// </summary>
		/// <value>
		/// The input stream.
		/// </value>
		public Stream InputStream { get; private set; }

		/// <summary>
		/// Convenient method to read the InputStream and return a string
		/// </summary>
		/// <returns>
		/// The string.
		/// </returns>
		public string ReadAsString()
		{
			using(StreamReader reader = new StreamReader(InputStream, Encoding.UTF8))
				return reader.ReadToEnd();
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a string
		/// </summary>
		/// <returns>
		/// The string async.
		/// </returns>
		public Task<string> ReadAsStringAsync()
		{
			using(StreamReader reader = new StreamReader(InputStream, Encoding.UTF8))
				return reader.ReadToEndAsync();
		}

		/// <summary>
		/// Convenient method to read the InputStream and return a byte array
		/// </summary>
		/// <returns>
		/// The byte array.
		/// </returns>
		public byte[] ReadAsBytes()
		{
			MemoryStream stream;
			try {
				long length = InputStream.Length;
				stream = new MemoryStream((int)length);
			}
			catch(NotSupportedException) {
				stream = new MemoryStream();
			}
			InputStream.CopyTo(stream);
			byte[] buffer;
			if(stream.GetBuffer().LongLength != stream.Length) {
				buffer = new byte[stream.Length];
				Array.Copy(stream.GetBuffer(), buffer, (int)stream.Length);
			}
			else
				buffer = stream.GetBuffer();
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
	}
}

