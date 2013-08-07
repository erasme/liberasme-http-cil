using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using System.Web;
using System.Web.Http.SelfHost;

namespace TestWebSocket
{
	class TestHttpListenerServer
	{
		public TestHttpListenerServer()
		{
		}

		private async void ProcessRequest(HttpListenerContext context)
		{
/*			WebSocketContext webSocketContext = null;
			try {
				webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
				string ipAddress = context.Request.RemoteEndPoint.Address.ToString();
				Console.WriteLine("Connected: IPAddress {0}", ipAddress);
			}
			catch(Exception e) {
				context.Response.StatusCode = 500;
				context.Response.Close();
				Console.WriteLine("Exception: {0}", e);
				return;
			}
			WebSocket webSocket = webSocketContext.WebSocket;
			try {
				byte[] receiveBuffer = new byte[1024];
				while(webSocket.State == WebSocketState.Open) {
					WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
					if(receiveResult.MessageType == WebSocketMessageType.Close)
						await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
					else
						await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Text, receiveResult.EndOfMessage, CancellationToken.None);
				}
			}
			catch(Exception e) {
				Console.WriteLine("Exception: {0}", e);
			}
			finally {
				if(webSocket != null)
					webSocket.Dispose();
			}*/
		}

		public async void Start(string prefix)
		{
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add(prefix);
			listener.Start();
			Console.WriteLine("Listening...");
			while(true) {
				HttpListenerContext context = await listener.GetContextAsync();
//				if(context.Request.IsWebSocketRequest)
//					ProcessRequest(context);
//				else {
					context.Response.StatusCode = 200;
					byte[] byteArray = Encoding.ASCII.GetBytes("Hello World !\n");
					context.Response.OutputStream.Write(byteArray, 0, byteArray.Length);
					context.Response.Close();
//				}
			}
//			listener.Close();
		}
	}

/*	class TestHttpSelfHost
	{
		public TestHttpSelfHost()
		{
		}

		public void Start(string prefix)
		{
			var config = new HttpSelfHostConfiguration(prefix);


			using(HttpSelfHostServer server = new HttpSelfHostServer(config)) {
				server.OpenAsync().Wait();
				Console.WriteLine("Press Enter to quit.");
				Console.ReadLine();
			}
		}
	}*/


	class MainClass
	{
		public static void Main(string[] args)
		{
			/*// Using WebServiceHost and ServiceContract

			Uri baseAddress = new Uri("http://localhost:8000/");

			WebServiceHost svcHost = new WebServiceHost(typeof(TestContract), baseAddress);
			try
			{
				WebHttpBinding binding = new WebHttpBinding();
				svcHost.AddServiceEndpoint(typeof(ITestContract), binding, "").Behaviors.Add(new WebHttpBehavior());

				svcHost.Open();

				Console.WriteLine("Service is running");
				Console.WriteLine("Press enter to quit...");
				Console.ReadLine();

				svcHost.Close();
			}
			catch(CommunicationException cex)
			{
				Console.WriteLine("An exception occurred: {0}", cex.Message);
				svcHost.Abort();
			}
			*/

			
			// using HttpListener
			TestHttpListenerServer server = new TestHttpListenerServer();
			server.Start("http://localhost:3334/");
			Console.WriteLine("Service is running");
			Console.WriteLine("Press enter to quit...");
			Console.ReadLine();


			//TestHttpSelfHost selfHost = new TestHttpSelfHost();
			//selfHost.Start("http://localhost:8000");

			/*StringWriter stringWriter = new StringWriter();
			HttpResponse response = new HttpResponse(stringWriter);
			response.ContentType = "text/plain";
			response.ContentEncoding = Encoding.UTF8;
			response.StatusCode = 200;
			response.Headers.Add("X-Era-Session", "test");
			byte[] byteArray = Encoding.ASCII.GetBytes("Hello World !\n");
			response.OutputStream.Write(byteArray, 0, byteArray.Length);

			response.Flush();

			Console.WriteLine("Response:");
			Console.WriteLine(stringWriter.GetStringBuilder().ToString());*/


			/*byte[] byteArray = Encoding.UTF8.GetBytes("Hello World !");
			MemoryStream stream = new MemoryStream(byteArray);

			HttpContent streamContent = new StreamContent(stream);
			streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain");

			StringContent stringContent = new StringContent("Salut les gars");

			MultipartContent multipartContent = new MultipartContent();
			multipartContent.Add(streamContent);
			multipartContent.Add(stringContent);


			MemoryStream resStream = new MemoryStream();
			multipartContent.CopyToAsync(resStream).Wait();
			Console.WriteLine(Encoding.UTF8.GetString(resStream.GetBuffer()));*/

		}
	}
}