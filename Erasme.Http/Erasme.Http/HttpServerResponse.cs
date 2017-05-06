// HttpServerResponse.cs
// 
//  Define a HTTP response to send in response of an HttpServerRequest received
//  by a HttpServerClient of an HttpServer.
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2017 Daniel LACROIX
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
using System.Collections.Generic;

namespace Erasme.Http
{
	public struct Cookie
	{
		public string Name;
		public string Value;
		public DateTime? Expires;
		public string Path;
		public string Domain;
	}

	public class HttpServerResponse
	{
		public HttpServerResponse()
		{
			Headers = new HttpHeaders();
			Cookies = new List<Cookie>();
			StatusCode = -1;
			Status = null;
			Content = null;
			Sent = false;
			SupportGzip = null;
			SupportRanges = false;
		}

		public bool Sent { get; set; }

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
		public List<Cookie> Cookies { get; internal set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Erasme.Http.HttpResponse"/> support gzip.
		/// If gzip is supported, the response might be gzipped
		/// </summary>
		/// <value>
		/// <c>true</c> if support gzip; otherwise, <c>false</c>.
		/// </value>
		public bool? SupportGzip { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Erasme.Http.HttpResponse"/> support HTTP ranges.
		/// This means that the content must be seekable and stable between HTTP requests.
		/// Default value is false
		/// </summary>
		/// <value>
		/// <c>true</c> if support ranges; otherwise, <c>false</c>.
		/// </value>
		public bool SupportRanges { get; set; }

		/// <summary>
		/// Gets or sets the HTTP status code of the response.
		/// </summary>
		/// <value>
		/// The status code.
		/// </value>
		public int StatusCode { get; set; }

		/// <summary>
		/// Gets or sets the HTTP status description string.
		/// </summary>
		/// <value>
		/// The status description.
		/// </value>
		public string StatusDescription { get; set; }

		string status = null;
		public string Status {
			get {
				if(status == null) {
					if(StatusDescription == null)
						return StatusCode+" "+HttpUtility.GetStatusDetail(StatusCode);
					else
						return StatusCode+" "+StatusDescription;
				}
				else
					return status;
			}
			set {
				status = value;
			}
		}

		/// <summary>
		/// Gets or sets the content of the HTTP reponse.
		/// </summary>
		/// <value>
		/// The content.
		/// </value>
		public HttpContent Content { get; set; }
	}
}

