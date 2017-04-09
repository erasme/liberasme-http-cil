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
				Uri uri = new Uri(args[0]);
				using(HttpClient client = HttpClient.Create(uri.Host, uri.Port)) {
					HttpClientRequest request = new HttpClientRequest();
					request.Method = "GET";
					request.Path = uri.PathAndQuery;
					client.SendRequest(request);
					HttpClientResponse response = client.GetResponse();

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
