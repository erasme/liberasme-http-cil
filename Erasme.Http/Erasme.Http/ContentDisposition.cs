// ContentDisposition.cs
// 
//  Parser for the Content-Disposition header
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
using System.Text;
using System.Collections.Generic;

namespace Erasme.Http
{
	public class ContentDisposition
	{
		class ContentDispositionDecoder
		{
			string content;
			int pos = 0;

			public ContentDispositionDecoder(string contentDisposition)
			{
				content = contentDisposition;
			}

			public bool Read()
			{
				RemoveSpace();
				return (pos < content.Length);
			}

			public bool IsSeparator {
				get {
					return content[pos] == ';';
				}
			}

			public bool IsEqual {
				get {
					return content[pos] == '=';
				}
			}

			public void RemoveSpace()
			{
				while((pos < content.Length) && ((content[pos] == ' ') || (content[pos] == '\t')))
					pos++;
			}

			public string ReadString()
			{
				bool quoted = content[pos] == '"';
				if(quoted) {
					if(++pos >= content.Length)
						throw new Exception("Invalid quoted string");
				}
				StringBuilder str = new StringBuilder();
				do {
					if(quoted) {
						if(content[pos] == '\\') {
							pos++;
							if(pos >= content.Length)
								throw new Exception("Invalid quoted string");
							str.Append(content[pos++]);
						}
						else if(content[pos] == '"') {
							pos++;
							break;
						}
						else
							str.Append(content[pos++]);
						if(pos >= content.Length)
							throw new Exception("Invalid quoted string");
					}
					else {
						if((content[pos] == ';') || (content[pos] == '='))
							break;
						else {
							str.Append(content[pos++]);
							if(pos >= content.Length)
								break;
						}
					}
				}
				while(true);
				return str.ToString();
			}

			public void ReadSeparator()
			{
				pos++;
			}

			public void ReadEqual()
			{
				pos++;
			}
		}

		public static Dictionary<string,string> Decode(string contentDisposition)
		{
			Dictionary<string,string> result = new Dictionary<string, string>();
			ContentDispositionDecoder decoder = new ContentDispositionDecoder(contentDisposition);
			while(decoder.Read()) {
				string key = decoder.ReadString();
				string value = null;
				if(decoder.Read()) {
					if(decoder.IsEqual) {
						decoder.ReadEqual();
						if(!decoder.Read())
							throw new Exception("No value");
						value = decoder.ReadString();
						if(decoder.Read()) {
							if(decoder.IsSeparator)
								decoder.ReadSeparator();
							else
								throw new Exception("Invalid format");
						}
					}
					else if(decoder.IsSeparator)
						decoder.ReadSeparator();
					else
						throw new Exception("Invalid format");
				}
				result[key] = value;
			}
			return result;
		}

		public static string Encode(Dictionary<string,string> contentDisposition)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach(string key in contentDisposition.Keys) {
				string value = contentDisposition[key];
				if(first)
					first = false;
				else
					sb.Append("; ");
				sb.Append(key);
				sb.Append("=\"");
				sb.Append(value);
				sb.Append("\"");
			}
			return sb.ToString();
		}
	}
}
