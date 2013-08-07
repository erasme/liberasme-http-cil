// MimeHeaders.cs
// 
//  Mime headers. Header used for example in multipart contents
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
	public class MimeHeaders: Dictionary<string,string>
	{
		public MimeHeaders()
		{
		}

		public MimeHeaders(IDictionary<string,string> values): base(values)
		{
		}

		public Dictionary<string,string> ContentDisposition {
			get {
				if(ContainsKey("content-disposition"))
					return Erasme.Http.ContentDisposition.Decode(this["content-disposition"]);
				else
					return new Dictionary<string, string>();
			}
		}

		public string ContentType {
			get {
				if(this.ContainsKey("content-type"))
					return this["content-type"];
				else
					return null;
			}
			set {
				if(value == null)
					this.Remove("content-type");
				else
					this["content-type"] = value;
			}
		}

		public long? ContentLength {
			get {
				if(this.ContainsKey("content-length"))
					return Convert.ToInt64(this["content-length"]);
				else
					return null;
			}
			set {
				if(value == null)
					this.Remove("content-length");
				else
					this["content-length"] = ((long)value).ToString();
			}
		}
	}
}
