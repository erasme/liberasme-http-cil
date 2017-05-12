// HttpSendResponse.cs
// 
//  Provide tools to send the HTTP response in a given HttpContext
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2015-2017 Daniel Lacroix
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
using System.IO.Compression;

namespace Erasme.Http
{
	public struct HttpRange
	{
		public long Start;
		public long Length;
	}

	public class HttpSendResponse: IHttpHandler
	{
		public static async Task SendAsync(HttpContext context)
		{
			if(!context.Response.Sent && (context.WebSocket == null)) {
				// finish reading request input stream if needed
				while(await context.Request.InputStream.ReadAsync(null, 0, int.MaxValue) > 0) {}

				// no content = 404 not found
				if (context.Response.Content == null)
				{
					if (context.Response.StatusCode == -1)
					{
						if (context.Request.Method == "GET")
						{
							context.Response.StatusCode = 404;
							var defaultContent = new StringContent("File not found\r\n");
							defaultContent.Headers.ContentType = "text/plain; utf-8";
							context.Response.Content = defaultContent;
						}
						else
						{
							context.Response.StatusCode = 405;
							context.Response.Content = HttpContent.Null;
						}
					}
					else
						context.Response.Content = HttpContent.Null;
				}
				// default HTTP status code when a content was set
				else if (context.Response.StatusCode == -1)
					context.Response.StatusCode = 200;
				
				// decide if we support HTTP ranges
				bool supportRanges = context.Response.SupportRanges &&
					// only support HTTP ranges with StreamContent
					context.Response.Content is StreamContent;

				bool hasBytesRanges = context.Request.Headers.ContainsKey("range") &&
					context.Request.Headers["range"].ToLower().StartsWith("bytes=", StringComparison.InvariantCulture);

				// finish the headers
				if(!context.Response.Headers.ContainsKey("server"))
					context.Response.Headers["server"] = context.Client.Server.ServerName;
				if(!context.Response.Headers.ContainsKey("date"))
					context.Response.Headers["date"] = DateTime.Now.ToUniversalTime().ToString("r", System.Globalization.CultureInfo.InvariantCulture);
				if(!context.Response.Headers.ContainsKey("content-type"))
					context.Response.Headers["content-type"] = context.Response.Content.Headers.ContentType;
				// if no cache-control is not defined, define a default one
				// this will avoid problem with different default policies in browers
				if(!context.Response.Headers.ContainsKey("cache-control"))
					context.Response.Headers["cache-control"] = "no-cache, must-revalidate";

				// decide if we are using GZip encoding or not
				bool supportGzip = false;
				if(context.Response.SupportGzip != null)
					supportGzip = (bool)context.Response.SupportGzip;
				// auto decide to user GZip or not
				else {
					string contentType = context.Response.Headers["content-type"];
					supportGzip = (contentType.StartsWith("text/plain", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("text/css", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("text/html", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("application/javascript", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("application/xml", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("application/json", StringComparison.InvariantCulture) ||
					               contentType.StartsWith("image/svg+xml", StringComparison.InvariantCulture));
				}
				// test if the server allow it
				supportGzip &= context.Client.Server.AllowGZip;
				// check if the HTTP client support GZip
				supportGzip &= context.Request.Headers.ContainsKey("accept-encoding") && context.Request.Headers["accept-encoding"].Contains("gzip");
				// HTTP ranges not compatible with GZip, priority to ranges
				supportGzip &= !hasBytesRanges;

				long contentLength;
				Stream gzippedStream = null;
				if(context.Response.Content.TryComputeLength(out contentLength)) {
					if(supportGzip) {
						// if content is too small, GZip will inflate it, so
						// disable GZip in this case
						if(contentLength < 40) {
							supportGzip = false;
							context.Response.Headers["content-length"] = contentLength.ToString();
						}
						// if file is not too big, compress it in a memory stream
						else if(contentLength < 8192) {
							gzippedStream = new MemoryStream(8192);
							using(GZipStream gzipStream = new GZipStream(gzippedStream, CompressionMode.Compress, true)) {
								await context.Response.Content.CopyToAsync(gzipStream);
							}
							gzippedStream.Seek(0, SeekOrigin.Begin);
							context.Response.Headers["content-length"] = gzippedStream.Length.ToString();
						}
						// else use the HTTP chunk encoding
						else {
							contentLength = -1;
						}
					}
					else 
						context.Response.Headers["content-length"] = contentLength.ToString();
				}
				else {
					// if length is unknown, HTTP ranges are not possibles
					supportRanges = false;
					contentLength = -1;
				}
				if(supportGzip)
					context.Response.Headers["content-encoding"] = "gzip";
				// encode using HTTP chunks if content length is not known
				if(contentLength == -1)
					context.Response.Headers["transfer-encoding"] = "chunked";

				HttpRange[] ranges = null;
				if(supportRanges) {
					context.Response.Headers["accept-ranges"] = "bytes";

					if(hasBytesRanges) {
						long total;
						ranges = ParseHttpRanges(context.Request.Headers["range"], contentLength, out total);
						// only support one range
						if(ranges.Length != 1) {
							ranges = null;
							supportRanges = false;
						}
						else {
							context.Response.StatusCode = 206;
							context.Response.Headers["content-length"] = total.ToString();
							context.Response.Headers["content-range"] = "bytes "+ranges[0].Start+"-"+(ranges[0].Start+ranges[0].Length-1)+"/"+contentLength;
						}
					}
				}
				// handle connection header
				if(context.Request.Headers.ContainsKey("connection") && (context.Request.Headers["connection"].ToLower() == "keep-alive") && (context.KeepAliveCountdown > 0)) {
					// handle Keep-Alive
					context.Response.Headers["connection"] = "keep-alive";
					context.Response.Headers["keep-alive"] = "timeout="+context.KeepAliveTimeout+",max="+(context.KeepAliveCountdown--);
				}
				else {
					context.Response.Headers["connection"] = "close";
					context.KeepAliveCountdown = -1;
				}

				// send the result

				// compute the headers into memory
				Stream memStream = new MemoryStream();
				var buffer = Encoding.UTF8.GetBytes("HTTP/1.1 "+context.Response.Status+"\r\n");
				memStream.Write(buffer, 0, buffer.Length);
				HttpUtility.HeadersToStream(context.Response.Headers, context.Response.Cookies, memStream);

				// send the headers
				memStream.Seek(0, SeekOrigin.Begin);
				await memStream.CopyToAsync(context.Client.Stream);

				// send the content
				if(contentLength != -1) {
					Stream stream = context.Client.Stream;
					if(supportGzip) {
						await gzippedStream.CopyToAsync(context.Client.Stream);
						gzippedStream.Close();
					}
					else if(ranges != null) {
						var streamContent = context.Response.Content as StreamContent;
						byte[] copyBuffer = new byte[4096];
						streamContent.Stream.Seek(ranges[0].Start, SeekOrigin.Begin);
						long remains = ranges[0].Length;
						while(remains > 0) {
							int size = await streamContent.Stream.ReadAsync(copyBuffer, 0, (int)Math.Min(copyBuffer.Length, remains));
							await context.Client.Stream.WriteAsync(copyBuffer, 0, size);
							remains -= size;
						}
					}
					else
						await context.Response.Content.CopyToAsync(context.Client.Stream);
				}
				// send using HTTP chunks
				else {
					using(OutputChunkedStream chunkedStream = new OutputChunkedStream(context.Client.Stream)) {
						if(supportGzip) {
							using(GZipStream gzipStream = new GZipStream(chunkedStream, CompressionMode.Compress, true)) {
								await context.Response.Content.CopyToAsync(gzipStream);
								gzipStream.Close();
							}
						}
						else {
							await context.Response.Content.CopyToAsync(chunkedStream);
						}
						// close the chunked stream
						await chunkedStream.CloseAsync();
					}
				}
				context.Response.Sent = true;
			}
		}

		public Task ProcessRequestAsync(HttpContext context)
		{
			return SendAsync(context);
		}

		public static HttpRange[] ParseHttpRanges(string rangesString, long length, out long total)
		{
			if(!rangesString.ToLower().StartsWith("bytes=", StringComparison.InvariantCulture))
				throw new Exception("Only bytes range are supported");
			rangesString = rangesString.Substring(6);
			var rangesStr = rangesString.Split(',');

			total = 0;
			HttpRange[] ranges = new HttpRange[rangesStr.Length];
			for(int i = 0; i < ranges.Length; i++) {
				string rangeString = rangesStr[i];
				if(rangeString.StartsWith("-", StringComparison.InvariantCulture)) {
					var last = Convert.ToInt64(rangeString.Substring(1));
					ranges[i].Length = Math.Min(length, last);
					ranges[i].Start = length - ranges[i].Length;
				}
				else if(rangeString.EndsWith("-", StringComparison.InvariantCulture)) {
					var first = Convert.ToInt64(rangeString.Substring(0, rangeString.Length - 1));
					ranges[i].Start = Math.Min(length, first);
					ranges[i].Length = length - ranges[i].Start;
				}
				else {
					var parts = rangeString.Split('-');
					var first = Convert.ToInt64(parts[0]);
					var last = Convert.ToInt64(parts[1]);
					ranges[i].Start = Math.Min(length, first);
					ranges[i].Length = Math.Min(length - first, last + 1 - first);
				}
				total += ranges[i].Length;
			}
			return ranges;
		}
	}
}

