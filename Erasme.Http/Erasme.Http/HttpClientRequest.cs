// HttpClientRequest.cs
// 
//  Represent a HTTP request to send by a HttpClient
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2017 Metropole de Lyon
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
	public class HttpClientRequest
	{
		public HttpClientRequest()
		{
			Headers = new HttpHeaders();
			Protocol = "HTTP/1.1";
			Method = "GET";
			QueryString = new Dictionary<string, string>();
			Cookies = new Dictionary<string, string>();
			Sent = false;
		}

		public bool Sent { get; set; }

		/// <summary>
		/// Gets all the HTTP headers.
		/// </summary>
		/// <value>
		/// The headers.
		/// </value>
		public HttpHeaders Headers { get; internal set; }

		/// <summary>
		/// Gets the HTTP protocol (ex: HTTP/1.0, HTTP/1.1 ...)
		/// </summary>
		/// <value>
		/// The protocol.
		/// </value>
		public string Protocol { get; set; }

		/// <summary>
		/// Gets the HTTP request method (GET, POST, PUT, DELETE...).
		/// </summary>
		/// <value>
		/// The method.
		/// </value>
		public string Method { get; set; }

		/// <summary>
		/// Get the HTTP GET parameters
		/// </summary>
		/// <value>
		/// The query string.
		/// </value>
		public Dictionary<string,string> QueryString { get; internal set; }

		/// <summary>
		/// Get the HTTP cookies
		/// </summary>
		/// <value>
		/// The cookies.
		/// </value>
		public Dictionary<string,string> Cookies { get; internal set; }

		/// <summary>
		/// Get the HTTP path (without the query string).
		/// </summary>
		/// <value>
		/// The path.
		/// </value>
		public string Path { get; set; }

		/// <summary>
		/// The body of the HTTP request has a body (for POST or PUT request).
		/// </summary>
		/// <value>
		/// The content.
		/// </value>
		public HttpContent Content { get; set; }

		public async Task CopyToAsync(Stream stream)
		{
			if (Content != null)
			{
				long contentLength = 0;
				Content.TryComputeLength(out contentLength);
				Headers["content-length"] = contentLength.ToString();
				if (!Headers.ContainsKey("content-type"))
					Headers["content-type"] = Content.Headers.ContentType.ToString();
			}

			// compute the headers into memory
			string fullPath = Path;
			if (QueryString.Count > 0)
				fullPath += "?" + HttpUtility.QueryStringToString(QueryString);
			Stream memStream = new MemoryStream();
			byte[] buffer = Encoding.UTF8.GetBytes(Method + " " + fullPath + " " + Protocol + "\r\n");
			await memStream.WriteAsync(buffer, 0, buffer.Length);
			HttpUtility.HeadersToStream(Headers, memStream);

			// send the headers
			memStream.Seek(0, SeekOrigin.Begin);
			await memStream.CopyToAsync(stream);

			// send the content
			if (Content != null)
				await Content.CopyToAsync(stream);
		}

		public void CopyTo(Stream stream)
		{
			CopyToAsync(stream).Wait();
		}
	}
}
