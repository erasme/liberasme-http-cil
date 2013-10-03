// WebRequest.cs
// 
//  Simple class to handle an HTTP request
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

namespace Erasme.Http
{
	public class WebRequest: HttpClientRequest, IDisposable
	{
		HttpClient client;

		public WebRequest(string url, bool allowAutoRedirect = false): this(new Uri(url), allowAutoRedirect)
		{
		}

		public WebRequest(Uri uri, bool allowAutoRedirect = false)
		{
			AllowAutoRedirect = allowAutoRedirect;
			client = HttpClient.Create(uri.DnsSafeHost, uri.Port, uri.Scheme == "https");
			Path = uri.AbsolutePath+uri.Query;
		}

		public bool AllowAutoRedirect { get; set; }

		public HttpClientResponse GetResponse()
		{
			HttpClientResponse response;
			int redirectCount = 0;
			bool redirectNeeded;

			do {
				redirectNeeded = false;
				client.SendRequest(this);
				response = client.GetResponse();
			
				// handle HTTP redirect
				if((AllowAutoRedirect) &&
					((response.StatusCode == 301) || (response.StatusCode == 302) || (response.StatusCode == 303) || (response.StatusCode == 307)) &&
					(response.Headers.ContainsKey("location"))) {
					if(response.StatusCode == 303)
						Method = "GET";
					client.Close();
					client.Dispose();

					Uri uri = new Uri(response.Headers["location"]);
					client = HttpClient.Create(uri.DnsSafeHost, uri.Port, uri.Scheme == "https");
					Path = uri.AbsolutePath + uri.Query;
					Headers["host"] = uri.DnsSafeHost;

					// TODO: handle the request body. Need to seek from the beginning

					redirectNeeded = true;
					redirectCount++;
					if(redirectCount > 5)
						throw new Exception("Too many HTTP redirections ("+redirectCount+")");
				}
			} while(redirectNeeded);
			return response;
		}

		public void Dispose()
		{
			client.Close();
			client.Dispose();
		}
	}
}

