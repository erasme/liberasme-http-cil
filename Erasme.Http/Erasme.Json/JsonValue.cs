// JsonValue.cs
// 
//  Define a JSON value. Base class of all JSON classes
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
using System.Text;
using System.Collections.Generic;

namespace Erasme.Json
{
	public abstract class JsonValue
	{
		public JsonValue()
		{
		}

		public abstract JsonType JsonType { get; }

		public virtual bool ContainsKey(string key)
		{
			return false;
		}

		public virtual JsonValue this[string key] {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public virtual JsonValue this[int index] {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public virtual int Count {
			get {
				return 0;
			}
		}

		public virtual ICollection<string> Keys {
			get {
				return (ICollection<string>)new List<string>();
			}
		}

		public virtual ICollection<JsonValue> Values {
			get {
				return (ICollection<JsonValue>)new List<JsonValue>();
			}
		}

		public static implicit operator double(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToDouble(((JsonPrimitive)value).Value);
			else
				return 0.0d;
		}

		public static implicit operator JsonValue(double value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator float(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToSingle(((JsonPrimitive)value).Value);
			else
				return 0.0f;
		}

		public static implicit operator JsonValue(float value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator bool(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToBoolean(((JsonPrimitive)value).Value);
			else
				return false;
		}

		public static implicit operator JsonValue(bool value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator string(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToString(((JsonPrimitive)value).Value);
			else
				return null;
		}

		public static implicit operator JsonValue(string value)
		{
			if(value == null)
				return null;
			else
				return new JsonPrimitive(value);
		}

		public static implicit operator byte(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToByte(((JsonPrimitive)value).Value);
			else
				return 0;
		}

		public static implicit operator JsonValue(byte value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator short(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToInt16(((JsonPrimitive)value).Value);
			else
				return 0;
		}

		public static implicit operator JsonValue(short value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator int(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToInt32(((JsonPrimitive)value).Value);
			else
				return 0;
		}

		public static implicit operator JsonValue(int value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator long(JsonValue value)
		{
			if(value is JsonPrimitive)
				return Convert.ToInt64(((JsonPrimitive)value).Value);
			else
				return 0L;
		}

		public static implicit operator JsonValue(long value)
		{
			return new JsonPrimitive(value);
		}


		public virtual object Value {
			get {
				throw new NotSupportedException();
			}
		}

		public override string ToString()
		{
			JsonSerializer serializer = new JsonSerializer();
			return serializer.Serialize(this);
		}

		public void Save(TextWriter textWriter)
		{
			textWriter.Write(ToString());
		}

		public void Save(Stream stream)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(ToString());
			stream.Write(buffer, 0, buffer.Length);
		}

		public static JsonValue Parse(string jsonString)
		{
			JsonDeserializer deserializer = new JsonDeserializer();
			return deserializer.Deserialize(jsonString);
		}

		public static JsonValue Load(TextReader textReader)
		{
			return Parse(textReader.ReadToEnd());
		}

		public static JsonValue Load(Stream stream)
		{
			return Load(new StreamReader(stream, Encoding.UTF8));
		}
	}
}

