// HttpContent.cs
// 
//  Base class to define the content of an HttpServerResponse
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013-2017 Daniel LACROIX
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
using System.Threading.Tasks;
using Erasme.Json;

namespace Erasme.Http
{
	public abstract class HttpContent: IDisposable
	{
		public HttpContent()
		{
			Headers = new MimeHeaders();
		}

		public MimeHeaders Headers { get; private set; }

		public abstract void CopyTo(Stream stream);

		public abstract bool TryComputeLength(out long length);

		public virtual Task CopyToAsync(Stream stream)
		{
			return Task.Factory.StartNew((obj) => CopyTo((Stream)obj), stream);
		}

		public virtual void Dispose()
		{
		}

		/// <summary>
		/// Special HttpContent to use when there is no content
		/// </summary>
		public static readonly HttpContent Null = new EmptyContent();

		/// <summary>
		/// Implicit converter for String
		/// </summary>
		public static implicit operator HttpContent(string str)
		{
			return new StringContent(str);
		}

		/// <summary>
		/// Implicit converter for JsonValue
		/// </summary>
		public static implicit operator HttpContent(JsonValue json)
		{
			return new JsonContent(json);
		}

		/// <summary>
		/// Implicit converter for Stream
		/// </summary>
		public static implicit operator HttpContent(Stream json)
		{
			return new StreamContent(json);
		}
	}
}

