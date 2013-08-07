// MultipartReader.cs
// 
//  Reader to read HTTP content Stream with multipart contents.
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Erasme.Http
{
	public class MultipartPart
	{
		public MimeHeaders Headers { get; set; }
		public BoundaryStream Stream { get; set; }
	}

	public class MultipartReader
	{
		ISharedBufferStream stream;
		string boundary;
		bool endOfPost = false;
		bool initDone = false;
		MultipartPart currentPart = null;

		public MultipartReader(Stream stream, string boundary)
		{
			this.stream = stream as ISharedBufferStream;
			if(this.stream == null)
				this.stream = new LengthLimitedStream(stream, Int64.MaxValue);
			this.boundary = boundary;
		}

		async Task Start()
		{
			if(boundary != null) {
				// if a boundary is a content-type, find the boundary in the content-type
				if(boundary.LastIndexOf("boundary=") != -1) {
					boundary = boundary.Substring(boundary.LastIndexOf("boundary=")+9, boundary.Length-(boundary.LastIndexOf("boundary=")+9));
				}
			}
			// go to the first boundary
			string line = await HttpUtility.ReadLineAsync(stream);

			// if boundary was not known, take it from the first line
			if(boundary == null) {
				if(line.StartsWith("--"))
					boundary = line.Substring(2, line.Length-2);
				else
					throw new Exception("Invalid multipart POST request ("+line+")");
			}
			else {
				if(line == "--"+boundary+"--")
					endOfPost = true;
				else if(line != "--"+boundary)
					throw new Exception("Invalid multipart POST request ("+line+")");
			}
		}

		public MultipartPart ReadPart()
		{
			Task<MultipartPart> task = ReadPartAsync();
			task.Wait();
			return task.Result;
		}

		public async Task<MultipartPart> ReadPartAsync()
		{
			if(!initDone) {
				await Start();
				initDone = true;
			}

			// if previous content part was not read, finish reading
			if(currentPart != null) {
				while(await currentPart.Stream.ReadAsync(null, 0,  Int32.MaxValue) > 0) {}
				// check if is was the last boundary
				if(currentPart.Stream.IsLastBoundary)
					endOfPost = true;
			}
			// last boundary seen, no new part
			if(endOfPost)
				return null;

			currentPart = new MultipartPart();
			currentPart.Headers = new MimeHeaders();
			if(!await HttpUtility.ReadHeadersAsync(stream, currentPart.Headers))
				throw new Exception("Invalid boundary stream");
			currentPart.Stream = new BoundaryStream(stream, boundary);
			return currentPart;
		}
	}
}

