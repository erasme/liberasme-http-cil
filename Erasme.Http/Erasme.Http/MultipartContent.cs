// MultipartContent.cs
// 
//  Define a Multipart content for the HttpClientResponse.Content
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

namespace Erasme.Http
{
	public class MultipartContent: HttpContent
	{
		string boundary;
		List<HttpContent> list = new List<HttpContent>();

		public MultipartContent()
		{
			boundary = RandomBoundary(32);
			Headers.ContentType = "multipart/form-data; boundary="+boundary;
		}

		public static string RandomBoundary(int length)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("--");
			string randChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			Random rand = new Random();
			for(int i = 2; i < length; i++)
				sb.Append(randChars[rand.Next(randChars.Length)]);
			return sb.ToString();
		}

		public void Add(HttpContent content)
		{
			list.Add(content);
		}

		public override void CopyTo(Stream stream)
		{
			byte[] buffer;
			if(list.Count > 0) {
				foreach(HttpContent content in list) {
					buffer = Encoding.UTF8.GetBytes("--"+boundary+"\r\n");
					stream.Write(buffer, 0, buffer.Length);
					// write content header
					MemoryStream memStream = new MemoryStream();
					HttpUtility.HeadersToStream(content.Headers, memStream);
					memStream.Seek(0, SeekOrigin.Begin);
					memStream.CopyTo(stream);
					// write content
					content.CopyTo(stream);
					// end content with \r\n
					buffer = Encoding.UTF8.GetBytes("\r\n");
					stream.Write(buffer, 0, buffer.Length);
				}
			}
			// write last boundary
			buffer = Encoding.UTF8.GetBytes("--"+boundary+"--\r\n");
			stream.Write(buffer, 0, buffer.Length);
		}

		public override bool TryComputeLength(out long length)
		{
			length = 0;
			long totalLength = 0;
			long contentLength;
			foreach(HttpContent content in list) {
				if(!content.TryComputeLength(out contentLength))
					return false;
				totalLength += contentLength;
			}
			// add headers size
			MemoryStream memStream = new MemoryStream();
			foreach(HttpContent content in list) {
				HttpUtility.HeadersToStream(content.Headers, memStream);
			}
			totalLength += memStream.Length;

			// all boundaries
			totalLength += (boundary.Length+6)*list.Count;
			// last boundary
			totalLength += boundary.Length+6;
			length = totalLength;
			return true;
		}

		public override void Dispose()
		{
			// dispose all parts 
			foreach(HttpContent content in list)
				content.Dispose();
		}
	}
}

