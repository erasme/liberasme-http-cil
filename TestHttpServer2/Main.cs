// Main.cs
// 
//  Simple HTTP server to test Erasme.Http library
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
using System.Collections.Generic;
using System.Security.Cryptography;
using Erasme.Http;
using Erasme.Json;

namespace TestHttpServer2
{
	/// <summary>
	/// Sample text HTTP service
	/// </summary>
	class TestHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				context.Response.Content = new StringContent("Hello World !\n");
			}
			return Task.FromResult<Object>(null);
		}
	}

	/// <summary>
	/// Sample JSON HTTP service
	/// </summary>
	class JsonHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				JsonValue json = new JsonObject();
				json["key1"] = 12;
				context.Response.Content = new JsonContent(json);
			}
			return Task.FromResult<Object>(null);
		}
	}

	/// <summary>
	/// Sample multipart reader service
	/// </summary>
	class MultiPartHandler: IHttpHandler
	{
		public async Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "POST") {
				MultipartReader reader = context.Request.ReadAsMultipart();
				StringBuilder sb = new StringBuilder();
				while(true) {
					MultipartPart part = await reader.ReadPartAsync();
					if(part == null)
						break;

					string name = "unknown";
					if(part.Headers.ContentDisposition.ContainsKey("name"))
						name = part.Headers.ContentDisposition["name"];

					MD5 md5 = MD5CryptoServiceProvider.Create();
					md5.Initialize();

					FileStream file = File.Create(name);
				
					byte[] buffer = new byte[4096];
					int totalSize = 0;
					int size;
					while((size = await part.Stream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
						md5.TransformBlock(buffer, 0, size, buffer, 0);
						await file.WriteAsync(buffer, 0, size);
						totalSize += size;
					}
					md5.TransformFinalBlock(buffer, 0, 0);
					file.Close();
					string md5String = "";
					foreach(byte b in md5.Hash) {
						md5String += b.ToString("x2");
					}
					sb.Append("Part name: "+name+", size: "+totalSize+", MD5: "+md5String);
					sb.Append("\n");
				}

				context.Response.StatusCode = 200;
				double seconds = (DateTime.Now - context.Request.StartTime).TotalSeconds;
				context.Response.Content = new StringContent(
					"Done. Bytes: "+context.Request.ReadCounter+", Seconds: "+seconds+", "+Math.Round(context.Request.ReadCounter/(seconds*1000000))+" MB/s, "+Math.Round(context.Request.ReadCounter*8/(seconds*1000000))+" Mbits/s\n"+
					sb.ToString());
			}
			else if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				MultipartContent content = new MultipartContent();
				context.Response.Content = content;

				HttpContent content1 = new StringContent("Message one");
				content1.Headers["content-disposition"] = "form-data; name=\"content1\"";
				content.Add(content1);

				HttpContent content2 = new StringContent("Message two");
				content2.Headers["content-disposition"] = "form-data; name=\"content2\"";
				content.Add(content2);
			}
		}
	}

	/// <summary>
	/// Sample static files service
	/// </summary>
	class FileHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				context.Response.SupportRanges = true;
				context.Response.Content = new FileContent("./files/"+context.Request.Path);
			}
			return Task.FromResult<Object>(null);
		}
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			HttpServer server = new HttpServer(3333);
			server.StopOnException = true;

			PathMapper mapper = new PathMapper();
			server.Add(mapper);

			mapper.Add("/test", new TestHandler());
			mapper.Add("/json", new JsonHandler());
			mapper.Add("/multipart", new MultiPartHandler());
			mapper.Add("/files", new FileHandler());


			server.Start();
			Console.WriteLine("Press enter to stop...");
			Console.ReadLine();
			server.Stop();
		}
	}
}
