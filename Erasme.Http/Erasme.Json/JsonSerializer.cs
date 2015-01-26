// JsonSerializer.cs
// 
//  Convert a JSON value to a String
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

namespace Erasme.Json
{
	internal class JsonSerializer
	{
		int indent;
		StringBuilder sb;
		
		public JsonSerializer()
		{
		}
								
		void WriteValue(JsonValue val)
		{
			// object
			if(val == null)
				sb.Append("null");
			else if(val.JsonType == JsonType.Object)
				WriteObject((JsonObject)val);
			// array
			else if(val.JsonType == JsonType.Array)
				WriteArray((JsonArray)val);
			else if(val.JsonType == JsonType.String)
				sb.Append(Enquote((string)val));
			else if(val.JsonType == JsonType.Boolean) {
				if((bool)val)
					sb.Append("true");
				else
					sb.Append("false");
			}
			else if(val.JsonType == JsonType.Number)
				sb.Append(((double)val).ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
		
		void WriteIndent()
		{
			for(int i = 0; i < this.indent; i++)
				sb.Append("  ");
		}

		void WriteArray(JsonArray obj)
		{
			sb.Append("[\n");
			
			indent++;
			
			for(int i = 0; i < obj.Count; i++) {
				bool isLast = (i == obj.Count-1);
				JsonValue val = obj[i];
				
				WriteIndent();
				WriteValue(val);
			
				if(isLast)
					sb.Append("\n");
				else
					sb.Append(",\n");
			}
			indent--;
			WriteIndent();
			sb.Append("]");
		}
		
		void WriteObject(JsonObject obj)
		{
			sb.Append("{\n");
			
			indent++;
			
			int i = 0;
			foreach(string key in obj.Keys) {
				bool isLast = (i++ == obj.Count-1);
				JsonValue val = obj[key];

				WriteIndent();
				sb.Append(Enquote(key));
				sb.Append(": ");
				
				WriteValue(val);
			
				if(isLast)
					sb.Append("\n");
				else
					sb.Append(",\n");
			}
			indent--;
			WriteIndent();
			sb.Append("}");
		}
		
		public string Serialize(JsonValue json)
		{
			Serialize(new StringBuilder(), json);
			return sb.ToString();	
		}
		
		public void Serialize(StringBuilder sb, JsonValue json)
		{
			indent = 0;
			this.sb = sb;
			WriteValue(json);
			this.sb.Append("\n");
		}

		public static string Enquote(string s) 
		{
			if(s == null || s.Length == 0)
				return "\"\"";
				
			StringBuilder sb = new StringBuilder(s.Length + 4);
			sb.Append('"');
			for(int i = 0; i < s.Length; i += 1) {
				char c = s[i];
				if((c == '\\') || (c == '"')) {
					sb.Append('\\');
					sb.Append(c);
				}
				else if (c == '\b')
					sb.Append("\\b");
				else if (c == '\t')
					sb.Append("\\t");
				else if (c == '\n')
					sb.Append("\\n");
				else if (c == '\f')
					sb.Append("\\f");
				else if (c == '\r')
					sb.Append("\\r");
				else {
					if(c < ' ')  {
						//t = "000" + Integer.toHexString(c);
						string tmp = new string(c, 1);
						string t = "000" + int.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
						sb.Append("\\u" + t.Substring(t.Length - 4));
					}
					else 
						sb.Append(c);
				}
			}
			sb.Append('"');
			return sb.ToString();
		}
	}
}

