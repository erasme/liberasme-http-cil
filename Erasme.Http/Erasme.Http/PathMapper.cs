// PathMapper.cs
// 
//  IHttpHandler that dispatch request using the path
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013-2014 Departement du Rhone
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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Erasme.Http
{
	public class PathMapper: IHttpHandler, IDisposable
	{
		readonly List<KeyValuePair<string, IHttpHandler>> handlers = new List<KeyValuePair<string, IHttpHandler>>();

		public async Task ProcessRequestAsync(HttpContext context)
		{
			foreach (var keyValue in handlers)				
			{
				if (context.Response.StatusCode != -1)
					break;
				if ((context.Request.Path == keyValue.Key) ||
				   (keyValue.Key.EndsWith("/", StringComparison.InvariantCulture) &&
					context.Request.Path.StartsWith(keyValue.Key, StringComparison.InvariantCulture)) ||
				   (!keyValue.Key.EndsWith("/", StringComparison.InvariantCulture) &&
					context.Request.Path.StartsWith(keyValue.Key + "/", StringComparison.InvariantCulture)))
				{
					// set the relative path
					string oldPath = context.Request.Path;
					context.Request.Path = context.Request.Path.Substring(keyValue.Key.Length);
					if (context.Request.Path == "")
						context.Request.Path = "/";
					await keyValue.Value.ProcessRequestAsync(context);
					context.Request.Path = oldPath;
				}
			}
		}

		public void Add(string basePath, IHttpHandler handler)
		{
			handlers.Add(new KeyValuePair<string,IHttpHandler>(basePath, handler));
		}

		public void Add(string basePath, HttpContent content)
		{
			handlers.Add(new KeyValuePair<string, IHttpHandler>(basePath, new StaticContentHandler(content)));
		}

		public void Dispose()
		{
			foreach(var keyValue in handlers) {
				var disposable = keyValue.Value as IDisposable;
				if(disposable != null) {
					try {
						disposable.Dispose();
					}
					catch(Exception) {
					}
				}
			}
		}
	}
}

