// HttpUtility.cs
// 
//  Provide tools to handle HTTP requests. Tools used by other classes
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2017 Daniel LACROIX
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
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erasme.Http
{
	public static class HttpUtility
	{
		static Regex quotedPrintableRegex = new Regex("=\\?([^\\?]+)\\?Q\\?(=[0-9A-F][0-9A-F])+\\?=", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public static bool ReadLine(ArraySegment<byte> segment, MemoryStream stringBuffer, ref bool cr, out int used)
		{
			used = 0;
			int count = segment.Count;
			int offset = segment.Offset;
			while(count > 0) {
				used++;
				if(cr) {
					// LF
					if(segment.Array[offset] == 10) {
						cr = false;
						return true;
					}
					else {
						throw new Exception("Invalid line format (expected LF)");
					}
				}
				else {
					// CR
					if(segment.Array[offset] == 13) {
						cr = true;
					}
					else if(segment.Array[offset] == 10) {
						throw new Exception("Invalid line format (LF without CR)");
					}
					else {
						stringBuffer.WriteByte(segment.Array[offset]);
					}
				}
				count--;
				offset++;
			}
			return false;
		}

		public static bool ReadLine(BufferContext bufferContext, MemoryStream stringBuffer, ref bool cr)
		{
			while(bufferContext.Count > 0) {
				bufferContext.Count--;
				int offset = bufferContext.Offset++;
				if(cr) {
					// LF
					if(bufferContext.Buffer[offset] == 10) {
						cr = false;
						return true;
					}
					else {
						throw new Exception("Invalid line format");
					}
				}
				else {
					// CR
					if(bufferContext.Buffer[offset] == 13) {
						cr = true;
					}
					else if(bufferContext.Buffer[offset] == 10) {
						throw new Exception("Invalid line format");
					}
					else {
						stringBuffer.WriteByte(bufferContext.Buffer[offset]);
					}
				}
			}
			return false;
		}

		public static async Task<string> ReadLineAsync(BufferContext bufferContext, MemoryStream stringBuffer)
		{
			bool cr = false;
			while(!ReadLine(bufferContext, stringBuffer, ref cr)) {
				await bufferContext.Fill();
				if(bufferContext.Count == 0)
					return null;
			}
			return Encoding.UTF8.GetString(stringBuffer.GetBuffer(), 0, (int)stringBuffer.Length);
		}

		public static Task<string> ReadLineAsync(ISharedBufferStream stream)
		{
			return ReadLineAsync(stream, new MemoryStream(128));
		}

		public static async Task<string> ReadLineAsync(ISharedBufferStream stream, MemoryStream stringBuffer)
		{
			bool cr = false;
			int used = 0;
			ArraySegment<byte> segment = await stream.SharedBufferReadAsync(Int32.MaxValue);
			if(segment.Count == 0)
					return null;
			while(!ReadLine(segment, stringBuffer, ref cr, out used)) {
				segment = await stream.SharedBufferReadAsync(Int32.MaxValue);
				if(segment.Count == 0)
					return null;
			}
			if(used != segment.Count)
				stream.SharedBufferRewind(segment.Count - used);
			return Encoding.UTF8.GetString(stringBuffer.GetBuffer(), 0, (int)stringBuffer.Length);
		}

		public static Task<string> ReadLineAsync(BufferContext bufferContext)
		{
			return ReadLineAsync(bufferContext, new MemoryStream(128));
		}

		public static async Task<bool> ReadHeadersAsync(BufferContext bufferContext, MemoryStream stringBuffer, Dictionary<string,string> headers)
		{
			bool cr = false;
			string line = null;

			while(true) {
				// if needed, fill the buffer
				if(bufferContext.Count <= 0) {
					await bufferContext.Fill();
				}
				if(bufferContext.Count <= 0)
					return false;
				// read a line
				if(ReadLine(bufferContext, stringBuffer, ref cr)) {
					string line2 = Encoding.UTF8.GetString(stringBuffer.GetBuffer(), 0, (int)stringBuffer.Length);
					stringBuffer.SetLength(0);
					// multi lines header
					if((line != null) && (line2.StartsWith(" ") || line2.StartsWith("\t"))) {
						line += line2;
					}
					// new header
					else {
						// flush previous line
						if(line != null) {
							int pos = line.IndexOf(':');
							if((pos == -1) || (pos == 0))
								throw new Exception("Invalid mime header line. Missing ':'");
							string key = line.Substring(0, pos).ToLower();
							string content = line.Substring(pos+2, line.Length - (pos+2));
							headers[key] = HttpUtility.QuotedPrintableDecode(content);
						}
						line = line2;
						if(line == String.Empty)
							return true;
					}
				}
			}
		}

		public static async Task<bool> ReadHeadersAsync(ISharedBufferStream stream, Dictionary<string,string> headers)
		{
			bool flushNeeded = false;
			string line = null;
			while(true) {
				string line2 = await ReadLineAsync(stream);
				if(line2 == null)
					throw new Exception("Invalid headers");
				else if(line2.StartsWith(" ") || line2.StartsWith("\t")) {
					if(line == null)
						line = line2;
					else
						line += line2;
					line2 = null;
				}
				else
					flushNeeded = true;

				// flush previous line
				if(flushNeeded) {
					if(line != null) {
						int pos = line.IndexOf(':');
						if((pos == -1) || (pos == 0))
							throw new Exception("Invalid mime header line. Missing ':'");
						string key = line.Substring(0, pos).ToLower();
						string content = line.Substring(pos+2, line.Length - (pos+2));
						headers[key] = HttpUtility.QuotedPrintableDecode(content);
					}
					line = line2;
				}
				if(line == String.Empty)
					return true;
			}
		}


		public static string ReadLine(Stream stream)
		{
			return ReadLine(new byte[1024], stream);
		}

		public static string ReadLine(byte[] buffer, Stream stream)
		{
			int pos = 0;
			bool lastCR = false;
			while(pos < buffer.Length) {
				int data = stream.ReadByte();
				if(data == -1)
					return null;
				if((data == 0x0a) && lastCR)
					// use UTF8 because it is compatible with ASCII-7
					// and because some browser encode part of mime headers
					// with UTF8 (which is a bug but a reality)
					return Encoding.UTF8.GetString(buffer, 0, pos-1);
				if(data == 0x0d)
					lastCR = true;
				else
					lastCR = false;
				buffer[pos] = (byte)data;
				pos++;
			}
			throw new Exception("line too long (max: "+buffer.Length+")");
		}

		public static string QuotedPrintableDecode(string line)
		{
			Match match = quotedPrintableRegex.Match(line);
			if(!match.Success)
				return line;
			
			StringBuilder sb = new StringBuilder();
			int pos = 0;
			
			while((match != null) && (match.Success)) {
				sb.Append(line.Substring(pos, match.Index-pos));
			
				string encodingstr = match.Groups[1].Captures[0].Value;
				Encoding encoding = Encoding.GetEncoding(encodingstr);
				if(encoding == null)
					throw new Exception("Quoted-Printable, unknown encoding");

				byte[] bytes = new byte[match.Groups[2].Captures.Count];
				for(int i = 0; i < match.Groups[2].Captures.Count; i++)
					bytes[i] = byte.Parse(match.Groups[2].Captures[i].Value.Substring(1), System.Globalization.NumberStyles.HexNumber);
				
				string decoded = encoding.GetString(bytes);
				sb.Append(decoded);				
				pos = match.Index + match.Length;
				match = match.NextMatch();
			}
			sb.Append(line.Substring(pos, line.Length - pos));
			return sb.ToString();
		}

		public static Dictionary<string,string> ReadHeaders(Stream stream)
		{
			return ReadHeaders(new byte[1024], stream);
		}

		public static Dictionary<string,string> ReadHeaders(byte[] buffer, Stream stream)
		{
			Dictionary<string,string> headers = new Dictionary<string,string>();
			string key = null;
			string content = null;
			while(true) {
				string line = HttpUtility.ReadLine(buffer, stream);
				if(line == null) {
					throw new Exception("Invalid Mime header 1");
				}
				if(line == "")
					break;
				// continue the previous line
				if((line.Length > 0) && ((line[0] == ' ') || (line[0] == '\t'))) {
					if(content == null) {
						throw new Exception("Invalid Mime header 2");
					}
					content += line;
				}
				else {
					if(key != null) {
						headers[key] = QuotedPrintableDecode(content);
						key = content = null;
					}
					int pos = line.IndexOf(':');
					if(pos == -1) {
						throw new Exception("Invalid Mime header2");
					}
					key = line.Substring(0, pos).ToLower();
					content = line.Substring(pos+2, line.Length - (pos+2));
				}
			}
			if(key != null) {
				headers[key] = QuotedPrintableDecode(content);
				key = content = null;
			}			
			return headers;
		}

		public static string UrlDecode(string str)
		{
			if(str == null)
				return null;
			if((str.IndexOf('+') == -1) && (str.IndexOf('%') == -1))
				return str;
			byte[] bytes = new byte[str.Length];
			int pos = 0;
			int result;
			for(int i = 0; i < str.Length; i++) {
				char c = str[i];
				if(c == '+')
					bytes[pos] = (byte)' ';
				else if((c == '%') && (i < str.Length - 1) &&
					int.TryParse(str[i + 1] + "" + str[i + 2], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result)) {
					i += 2;
					bytes[pos] = (byte)result;
				}
				else
					bytes[pos] = (byte)c;
				pos++;
			}
			return Encoding.UTF8.GetString(bytes, 0, pos);
		}

		public static string UrlEncode(string str)
		{
			if(str == null)
				return null;
			StringBuilder sb = new StringBuilder();
			foreach(char c in str) {
				if(((c >= '0') && (c <= '9'))  || ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
					sb.Append(c);
				else {
					byte[] bytes = System.Text.Encoding.UTF8.GetBytes(new char[1]{ c });
					foreach(byte b in bytes) {
						sb.AppendFormat("%{0:X2}", b);
					}
				}
			}
			return sb.ToString();
		}

		public static string GetStatusDetail(int status)
		{
			switch(status) {
			case 100: return "Continue";
			case 101: return "Switching Protocols";
			case 200: return "OK";
			case 201: return "Created";
			case 202: return "Accepted";
			case 203: return "Non-Authoritative Information";
			case 204: return "No Content";
			case 205: return "Reset Content";
			case 206: return "Partial Content";
			case 300: return "Multiple Choices";
			case 301: return "Moved Permanently";
			case 302: return "Found";
			case 303: return "See Other";
			case 304: return "Not Modified";
			case 305: return "Use Proxy";
			case 306: return "(Unused)";
			case 307: return "Temporary Redirect";
			case 400: return "Bad Request";
			case 401: return "Unauthorized";
			case 402: return "Payment Required";
			case 403: return "Forbidden";
			case 404: return "Not Found";
			case 405: return "Method Not Allowed";
			case 406: return "Not Acceptable";
			case 407: return "Proxy Authentication Required";
			case 408: return "Request Timeout";
			case 409: return "Conflict";
			case 410: return "Gone";
			case 411: return "Length Required";
			case 412: return "Precondition Failed";
			case 413: return "Request Entity Too Large";
			case 414: return "Request-URI Too Long";
			case 415: return "Unsupported Media Type";
			case 416: return "Requested Range Not Satisfiable";
			case 417: return "Expectation Failed";
			case 500: return "Internal Server Error";
			case 501: return "Not Implemented";
			case 502: return "Bad Gateway";
			case 503: return "Service Unavailable";
			case 504: return "Gateway Timeout";
			case 505: return "HTTP Version Not Supported";
			default: return "Unknown";
			}
		}

		public static void HeadersToStream(Dictionary<string,string> headers, Stream stream)
		{
			byte[] buffer;
			foreach(string header in headers.Keys) {
				buffer = Encoding.UTF8.GetBytes(header);
				stream.Write(buffer, 0, buffer.Length);
				buffer = Encoding.UTF8.GetBytes(": ");
				stream.Write(buffer, 0, 2);
				buffer = Encoding.UTF8.GetBytes(headers[header]);
				stream.Write(buffer, 0, buffer.Length);
				buffer = Encoding.UTF8.GetBytes("\r\n");
				stream.Write(buffer, 0, 2);
			}
			stream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, 2);
		}

		public static void ParseCommand(
			string command, out string method, out string fullPath,
			out string path, out Dictionary<string,string> queryString,
			out Dictionary<string,List<string>> queryStringArray, out string protocol)
		{
			string[] tmp = command.Split(' ');
			if(tmp.Length != 3)
				throw new Exception("Invalid HTTP header, command not valid");
			// handle HTTP method
			method = tmp[0];
			// handle HTTP protocol
			protocol = tmp[2];
			if(!protocol.StartsWith("HTTP/", StringComparison.InvariantCulture))
				throw new Exception("Invalid HTTP header, protocol is not HTTP");
			// handle path
			fullPath = HttpUtility.UrlDecode(tmp[1]);
			tmp = tmp[1].Split('?');
			path = HttpUtility.UrlDecode(tmp[0]);
			// handle GET parameters
			queryString = new Dictionary<string,string>();
			queryStringArray = new Dictionary<string, List<string>>();
			if(tmp.Length > 1) {
				tmp = tmp[1].Split('&');
				foreach(string keyval in tmp) {
					var tmp2 = keyval.Split('=');
					var key = HttpUtility.UrlDecode(tmp2[0]);
					if (tmp2.Length == 1)
						queryString[key] = null;
					else if (tmp2.Length == 2)
					{
						if (key.EndsWith("[]", StringComparison.InvariantCulture))
						{
							key = key.Substring(0, key.Length - 2);
							if (!queryStringArray.ContainsKey(key))
								queryStringArray[key] = new List<string>();
							queryStringArray[key].Add(HttpUtility.UrlDecode(tmp2[1]));
						}
						else
							queryString[key] = HttpUtility.UrlDecode(tmp2[1]);
					}
				}
			}
		}

		public static void ParseStatus(string status, out string protocol, out int statusCode, out string statusDescription)
		{
			int pos = status.IndexOf(' ');
			if(pos == -1)
				throw new Exception("Invalid HTTP status");
			protocol = status.Substring(0, pos);
			if(!protocol.StartsWith("HTTP/", StringComparison.InvariantCulture))
				throw new Exception("Invalid HTTP status, protocol is not valid");
			status = status.Substring(pos + 1);
			pos = status.IndexOf(' ');
			if(pos == -1) {
				statusCode = Convert.ToInt32(status);
				statusDescription = String.Empty;
			}
			else {
				statusCode = Convert.ToInt32(status.Substring(0, pos));
				statusDescription = status.Substring(pos+1);
			}
		}

		public static Dictionary<string,string> ParseCookie(string cookie)
		{
			Dictionary<string,string> cookies = new Dictionary<string, string>();
			string[] tmp = cookie.Split(new char[] {';'}, System.StringSplitOptions.RemoveEmptyEntries);
			foreach(string keyval in tmp) {
				string[] tmp2 = keyval.Split(new char[]{'='}, System.StringSplitOptions.RemoveEmptyEntries);
				if(tmp2.Length == 2) {
					cookies[HttpUtility.UrlDecode(tmp2[0].Trim())] = HttpUtility.UrlDecode(tmp2[1]);
				}
			}
			return cookies;
		}

		public static string QueryStringToString(Dictionary<string,string> queryString)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach(string key in queryString.Keys) {
				if (!first)
					sb.Append("&");
				sb.Append(UrlEncode(key));
				if(queryString[key] != null) {
					sb.Append("=");
					sb.Append(UrlEncode(queryString[key]));
				}
				first = false;
			}
			return sb.ToString();
		}
	}
}

