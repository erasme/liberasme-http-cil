// HttpRouting.cs
// 
//  Simple HTTP server to test Erasme.Http library
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public class HttpRouting : IHttpHandler
	{
		public delegate void Route(Hashtable p, HttpContext c);
		public delegate Task RouteAsync(Hashtable p, HttpContext c);
		public delegate object TypeParser(string value);

		public Dictionary<string, TypeParser> Types = new Dictionary<string, TypeParser>();

		public Dictionary<string, Route> Get = new Dictionary<string, Route>();
		public Dictionary<string, RouteAsync> GetAsync = new Dictionary<string, RouteAsync>();
		public Dictionary<string, Route> Post = new Dictionary<string, Route>();
		public Dictionary<string, RouteAsync> PostAsync = new Dictionary<string, RouteAsync>();
		public Dictionary<string, Route> Put = new Dictionary<string, Route>();
		public Dictionary<string, RouteAsync> PutAsync = new Dictionary<string, RouteAsync>();
		public Dictionary<string, Route> Delete = new Dictionary<string, Route>();
		public Dictionary<string, RouteAsync> DeleteAsync = new Dictionary<string, RouteAsync>();

		bool FindMatchingKey(IEnumerable<string> keys, string path, out string matchingKey, out Hashtable parameters)
		{
			matchingKey = null;
			parameters = null;
			string[] pathParts = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string key in keys)
			{
				if (key == path)
				{
					matchingKey = key;
					return true;
				}
				else
				{
					string[] keyParts = key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
					if (keyParts.Length != pathParts.Length)
						continue;
					int i = 0;
					for (; i < pathParts.Length; i++)
					{
						string k = keyParts[i];
						string p = pathParts[i];
						if ((k.Length > 2) && (k[0] == '{') && (k[k.Length - 1] == '}'))
						{
							dynamic value = p;
							string param = k.Substring(1, k.Length - 2);
							int pos = param.IndexOf(':');
							if (pos >= 0)
							{
								string paramType = param.Substring(pos + 1);
								param = param.Substring(0, pos);

								if (paramType == "int")
								{
									int paramValue;
									if (Int32.TryParse(p, out paramValue))
										value = paramValue;
									else
										break;
								}
								else if (Types.ContainsKey(paramType))
								{
									object res = Types[paramType](p);
									if (res != null)
										value = res;
									else
										break;
								}
							}
							if (parameters == null)
								parameters = new Hashtable();
							parameters[param] = value;
						}
						else if (p != k)
						{
							break;
						}
					}
					if (i >= pathParts.Length)
					{
						matchingKey = key;
						return true;
					}
				}
			}
			return false;
		}

		public async Task ProcessRequestAsync(HttpContext context)
		{
			Hashtable parameters;
			string key;
			if (context.Request.Method == "GET")
			{
				if (FindMatchingKey(GetAsync.Keys, context.Request.Path, out key, out parameters))
					await GetAsync[key](parameters, context);
				else if (FindMatchingKey(Get.Keys, context.Request.Path, out key, out parameters))
					Get[key](parameters, context);
			}
			else if (context.Request.Method == "POST")
			{
				if (FindMatchingKey(PostAsync.Keys, context.Request.Path, out key, out parameters))
					await PostAsync[key](parameters, context);
				else if (FindMatchingKey(Post.Keys, context.Request.Path, out key, out parameters))
					Post[key](parameters, context);
			}
			else if (context.Request.Method == "PUT")
			{
				if (FindMatchingKey(PutAsync.Keys, context.Request.Path, out key, out parameters))
					await PutAsync[key](parameters, context);
				else if (FindMatchingKey(Put.Keys, context.Request.Path, out key, out parameters))
					Put[key](parameters, context);
			}
			else if (context.Request.Method == "DELETE")
			{
				if (FindMatchingKey(DeleteAsync.Keys, context.Request.Path, out key, out parameters))
					await DeleteAsync[key](parameters, context);
				else if (FindMatchingKey(Delete.Keys, context.Request.Path, out key, out parameters))
					Delete[key](parameters, context);
			}
		}
	}
}