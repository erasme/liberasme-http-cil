// StringContent.cs
// 
//  Define a UTF-8 string content for the HttpClientResponse.Content
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
using System.Threading.Tasks;

namespace Erasme.Http
{
	public class StringContent: HttpContent
	{
		byte[] buffer = null;

		public StringContent()
		{
			Headers.ContentType = "text/plain; charset=utf-8";
		}

		public StringContent(string str)
		{
			buffer = Encoding.UTF8.GetBytes(str);
			Headers.ContentType = "text/plain; charset=utf-8";
		}

		public override bool TryComputeLength(out long length)
		{
			if(buffer == null) {
				length = 0;
				return true;
			}
			else {
				length = buffer.Length;
				return true;
			}
		}

		public override Task CopyToAsync(Stream stream)
		{
			if(buffer == null)
				return Task.FromResult<Object>(null);
			else
				return stream.WriteAsync(buffer, 0, buffer.Length);
		}

		public override void CopyTo(Stream stream)
		{
			if(buffer != null)
				stream.Write(buffer, 0, buffer.Length);
		}

		public override void Dispose()
		{
			buffer = null;
		}
	}
}

