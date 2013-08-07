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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Erasme.Http;
using Erasme.Json;

namespace TestHttpServer
{
	/// <summary>
	/// Sample echo websocket service
	/// </summary>
	class EchoHandler: IHttpHandler
	{
		class EchoClient: WebSocketHandler
		{
			static WebSocketHandlerCollection<EchoClient> clients = new WebSocketHandlerCollection<EchoClient>();

			public override void OnOpen()
			{
				clients.Add(this);
			}

			public override void OnMessage(string message)
			{
				clients.Broadcast(message);
			}

			public override void OnError()
			{
			}

			public override void OnClose()
			{
				clients.Remove(this);
			}
		}

		public async Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.IsWebSocketRequest)
				// accept the web socket and process it
				await context.AcceptWebSocketRequest(new EchoClient());
		}
	}

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
	/// Sample simple HTTP service that raises exception
	/// </summary>
	class BrokenHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			throw new Exception("Something went wrong...");
		}
	}

	/// <summary>
	/// Sample POST HTTP service handler
	/// </summary>
	class TestPostHandler: IHttpHandler
	{
		public async Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "POST") {
				string body = await context.Request.ReadAsStringAsync();
				//Console.WriteLine("TestPostHandler.ProcessRequest content: "+body);
				context.Response.StatusCode = 200;
				context.Response.Content = new StringContent(body);
			}
		}
	}

	/// <summary>
	/// Sample POST JSON HTTP service handler
	/// </summary>
	class JsonPostHandler: IHttpHandler
	{
		public async Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "POST") {
				JsonValue json = await context.Request.ReadAsJsonAsync();
				// add a field to the JSON
				json["serverName"] = "Funky server";
				context.Response.StatusCode = 200;
				context.Response.Content = new JsonContent(json);
			}
		}
	}

	/// <summary>
	/// Sample JSON HTTP service
	/// </summary>
	class JsonHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			//Console.WriteLine("JsonHandler.ProcessRequest Path: "+context.Request.Path+", RelativePath: "+context.Request.RelativePath);
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
	/// Sample service that return connected clients
	/// </summary>
	class StatusHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				JsonArray json = new JsonArray();
				foreach(Erasme.Http.HttpServerClient client in context.Client.Server.Clients) {
					JsonObject jsonClient = new JsonObject();
					jsonClient["remote"] = client.RemoteEndPoint.ToString();
					jsonClient["local"] = client.LocalEndPoint.ToString();
					jsonClient["websocket"] = (client.WebSocket != null);
					jsonClient["duration"] = (DateTime.Now - client.StartTime).TotalSeconds;
					jsonClient["readcounter"] = client.ReadCounter;
					jsonClient["writecounter"] = client.WriteCounter;
					jsonClient["requestcounter"] = client.RequestCounter;

					if(client.Context != null) {
						jsonClient["path"] = client.Context.Request.AbsolutePath;
						jsonClient["user"] = client.Context.User;
					}
					json.Add(jsonClient);
				}
				context.Response.Content = new JsonContent(json);
			}
			return Task.FromResult<Object>(null);
		}
	}

	/// <summary>
	/// Sample static files service
	/// </summary>
	class FileHandler: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			//Console.WriteLine("File Path: "+context.Request.Path);
			if(context.Request.Method == "GET") {
				context.Response.StatusCode = 200;
				context.Response.SupportRanges = true;
				context.Response.Content = new FileContent("./files/"+context.Request.Path);
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
				while(true) {
					MultipartPart part = await reader.ReadPartAsync();
					if(part == null)
						break;
					// read the text file content and display it
					if(part.Headers.ContentDisposition["name"] == "file") {
						Console.WriteLine("Read File");
						StreamReader streamReader = new StreamReader(part.Stream);
						Console.WriteLine(await streamReader.ReadToEndAsync());
					}
				}
				context.Response.StatusCode = 200;
				context.Response.Content = new StringContent("done");
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
	/// Write request statistique to the console
	/// </summary>
	class ConsoleLogger: IHttpHandler
	{
		public Task ProcessRequestAsync(HttpContext context)
		{
			// log date
			string log = "["+String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)+"] ";
			// remote address
			log += context.Request.RemoteEndPoint.ToString()+" ";
			// user
			if(context.User != null)
				log += context.User+" ";
			else
				log += "- ";
			// request 
			log += "\""+context.Request.Method+" "+context.Request.FullPath+"\" ";
			// response
			if(context.WebSocket != null)
				log += "WS ";
			else
				log += context.Response.StatusCode+" ";
			// bytes received
			log += context.Request.ReadCounter+"/"+context.Request.WriteCounter+" ";
			// time
			log += Math.Round((DateTime.Now - context.Request.StartTime).TotalMilliseconds).ToString(CultureInfo.InvariantCulture)+"ms";

			// write the log
			Console.WriteLine(log);

			return Task.FromResult<Object>(null);
		}
	}

	/// <summary>
	/// Test http server. Override WebSocket handlers to log what happends
	/// </summary>
	class TestHttpServer: HttpServer
	{
		public TestHttpServer(int port): base(port)
		{
		}

		protected override void OnWebSocketHandlerMessage(WebSocketHandler handler, string message)
		{
			// log the message

			// log date
			string log = "["+String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)+"] ";
			// remote address
			log += handler.Context.Request.RemoteEndPoint.ToString()+" ";
			// user
			if(handler.Context.User != null)
				log += handler.Context.User+" ";
			else
				log += "- ";
			// request 
			log += "\"WSMI "+handler.Context.Request.FullPath+"\" \""+message+"\"";

			// write the log
			Console.WriteLine(log);

			// handle the message
			base.OnWebSocketHandlerMessage(handler, message);
		}

		protected override void WebSocketHandlerSend(WebSocketHandler handler, string message)
		{
			base.WebSocketHandlerSend(handler, message);

			// log the message

			// log date
			string log = "["+String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)+"] ";
			// remote address
			log += handler.Context.Request.RemoteEndPoint.ToString()+" ";
			// user
			if(handler.Context.User != null)
				log += handler.Context.User+" ";
			else
				log += "- ";
			// request 
			log += "\"WSMO "+handler.Context.Request.FullPath+"\" \""+message+"\"";

			// write the log
			Console.WriteLine(log);
		}

		protected override void OnProcessRequestError(HttpContext context, Exception exception)
		{
			base.OnProcessRequestError(context, exception);

			// log date
			string log = "["+String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)+"] ";
			// remote address
			log += context.Request.RemoteEndPoint.ToString()+" ";
			// user
			if(context.User != null)
				log += context.User+" ";
			else
				log += "- ";

			// request 
			log += "\""+context.Request.Method+" "+context.Request.FullPath+"\" ";
			// response
			if(context.WebSocket != null)
				log += "WS ";
			else
				log += context.Response.StatusCode+" ";
			// bytes received
			log += context.Request.ReadCounter+"/"+context.Request.WriteCounter+" ";
			// time
			log += Math.Round((DateTime.Now - context.Request.StartTime).TotalMilliseconds).ToString(CultureInfo.InvariantCulture)+"ms\n";
			// exception details
			log += exception.ToString();

			// write the log
			Console.WriteLine(log);
		}
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			Thread.CurrentThread.Name = "HttpServer";

			HttpServer server = new TestHttpServer(3333);

			PathMapper mapper = new PathMapper();
			server.Add(mapper);
			mapper.Add("/test", new TestHandler());
			mapper.Add("/testpost", new TestPostHandler());
			mapper.Add("/json", new JsonHandler());
			mapper.Add("/jsonpost", new JsonPostHandler());
			mapper.Add("/echo", new EchoHandler());
			mapper.Add("/status", new StatusHandler());
			mapper.Add("/files", new FileHandler());
			mapper.Add("/multipart", new MultiPartHandler());
			mapper.Add("/broken", new BrokenHandler());
			server.Add(new HttpSendResponse());
			server.Add(new ConsoleLogger());

			server.Start();
			Console.WriteLine("Press enter to stop...");
			Console.ReadLine();
			server.Stop();
		}
	}
}
