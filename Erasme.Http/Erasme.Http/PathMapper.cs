// PathMapper.cs
// 
//  IHttpHandler that dispatch request using the path
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013-2014 Departement du Rhone
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
		Dictionary<string,IHttpHandler> handlers = new Dictionary<string, IHttpHandler>();

		public PathMapper()
		{
		}

		public Task ProcessRequestAsync(HttpContext context)
		{
			foreach(string basePath in handlers.Keys) {
				if((context.Request.Path == basePath) || 
				   (basePath.EndsWith("/") && context.Request.Path.StartsWith(basePath)) ||
				   (!basePath.EndsWith("/") && context.Request.Path.StartsWith(basePath+"/"))) {
					// set the relative path
					context.Request.Path = context.Request.Path.Substring(basePath.Length);
					if(context.Request.Path == "")
						context.Request.Path = "/";
					return handlers[basePath].ProcessRequestAsync(context);
				}
			}
			return Task.FromResult<Object>(null);
		}

		public void Add(string basePath, IHttpHandler handler)
		{
			handlers[basePath] = handler;
		}

		public void Add(string basePath, HttpContent content)
		{
			handlers[basePath] = new StaticContentHandler(content);
		}

		public void Dispose()
		{
			foreach(IHttpHandler handler in handlers.Values) {
				IDisposable disposable = handler as IDisposable;
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

