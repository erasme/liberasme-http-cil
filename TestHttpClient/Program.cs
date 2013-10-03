using System;
using Erasme.Http;

namespace TestHttpClient
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if(args.Length != 1) {
				Console.WriteLine("Usage: TestHttpClient.exe [URL]");
			}
			else {
				using(WebRequest request = new WebRequest(args[0], allowAutoRedirect: true)) {
					HttpClientResponse response = request.GetResponse();
					Console.WriteLine(response.Protocol + " " + response.Status);
					foreach(string key in response.Headers.Keys) {
						Console.WriteLine(key+": "+response.Headers[key]);
					}
					Console.WriteLine();
					Console.WriteLine(response.ReadAsString());
				}
			}
		}
	}
}
