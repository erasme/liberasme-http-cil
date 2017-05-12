// JsonPrimitive.cs
// 
//  Define a JSON primitive (String, Number...)
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2013 Metropole de Lyon
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

namespace Erasme.Json
{
	public class JsonPrimitive: JsonValue
	{
		readonly object value;

		public JsonPrimitive(bool value)
		{
			jsonType = JsonType.Boolean;
			this.value = value;
		}

		public JsonPrimitive(byte value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(short value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(int value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(long value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(float value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(double value)
		{
			jsonType = JsonType.Number;
			this.value = value;
		}

		public JsonPrimitive(string value)
		{
			jsonType = JsonType.String;
			this.value = value;
		}

		public JsonPrimitive(object value)
		{
			if (value is string)
				jsonType = JsonType.String;
			else if (value is bool)
				jsonType = JsonType.Boolean;
			else if ((value is byte) || (value is short) || (value is int) || (value is long) || (value is float) || (value is double))
				jsonType = JsonType.Number;
			else
				throw new Exception("Unsupported type");
			this.value = value;
		}


		JsonType jsonType;
		public override JsonType JsonType {
			get {
				return jsonType;
			}
		}

		public override object Value {
			get {
				return value;
			}
		}
	}
}

