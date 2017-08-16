// JsonValue.cs
// 
//  Define a JSON value. Base class of all JSON classes
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// Copyright (c) 2017 Daniel Lacroix
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
using System.Dynamic;
using System.Collections.Generic;
using System.Collections;

namespace Erasme.Json
{
	public abstract class JsonValue: DynamicObject
	{
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
				return new List<string>();
			}
		}

		public virtual ICollection<JsonValue> Values {
			get {
				return new List<JsonValue>();
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
			if(value.JsonType == JsonType.String)
				return (string)value.Value;
			else if(value is JsonPrimitive)
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

		public static implicit operator JsonValue(DateTime? value)
		{
			if (value == null)
				return null;
			else
				return new JsonPrimitive((new DateTimeOffset((DateTime)value)).ToString("O"));
		}

		public static implicit operator DateTime?(JsonValue value)
		{
			if (value == null)
				return null;
			else
				return DateTime.Parse((string)value.Value);
		}

		public virtual object Value {
			get {
				throw new NotSupportedException();
			}
		}

		public override string ToString()
		{
			var serializer = new JsonSerializer();
			return serializer.Serialize(this);
		}

		public void Save(TextWriter textWriter)
		{
			textWriter.Write(ToString());
		}

		public void Save(Stream stream)
		{
			var buffer = Encoding.UTF8.GetBytes(ToString());
			stream.Write(buffer, 0, buffer.Length);
		}

		public static JsonValue Parse(string jsonString)
		{
			var deserializer = new JsonDeserializer();
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

		public void Merge(JsonValue source)
		{
			Merge(this, source);
		}

		public static void Merge(JsonValue dest, JsonValue source)
		{
			foreach(string key in source.Keys) {
				if(dest.Keys.Contains(key) && (dest[key] is JsonObject) && (source[key] is JsonObject))
					Merge(dest[key], source[key]);
				else
					dest[key] = source[key];
			}
		}

		object ToObjectInternal(Type type)
		{
			object result = null;
			if (this is JsonPrimitive)
			{
				if (type.IsEnum && JsonType == JsonType.String)
					result = Enum.Parse(type, (string)Value);
				else if (type == typeof(TimeSpan) && JsonType == JsonType.String)
					result = TimeSpan.Parse((string)Value);
				else
					result = Convert.ChangeType(Value, type);
			}
			else if (this is JsonObject)
			{
				result = Activator.CreateInstance(type);
				foreach (var field in type.GetFields())
				{
					if (!ContainsKey(field.Name))
						continue;
					var value = this[field.Name];
					field.SetValue(result, value.ToObjectInternal(field.FieldType));
				}
				foreach (var prop in type.GetProperties())
				{
					if (!ContainsKey(prop.Name))
						continue;
					var value = this[prop.Name];
					prop.SetValue(result, value.ToObjectInternal(prop.PropertyType));
				}
			}
			else if (this is JsonArray)
			{
				result = Activator.CreateInstance(type, Count);
				if (type.IsArray)
				{
					var arrayItems = result as Array;
					for (var i = 0; i < ((JsonArray)this).Count; i++)
					{
						var el = ((JsonArray)this)[i];
						var elType = type.GetElementType();
						var value = el.ToObjectInternal(elType);
						arrayItems.SetValue(value, i);
					}
				}
			}
			return result;
		}

		public T ToObject<T>() where T : new()
		{
			return (T)ToObjectInternal(typeof(T));
		}

		public static T ParseToObject<T>(string jsonString) where T : new()
		{
			var deserializer = new JsonDeserializer();
			var json = deserializer.Deserialize(jsonString);
			return json.ToObject<T>();
		}

		static JsonValue NativeToJsonValue(object value)
		{
			JsonValue result = null;
			if (value is string)
				result = (string)value;
			else if (value is int)
				result = (int)value;
			else if (value is int?)
				result = (int?)value;
			else if (value is uint)
				result = (uint)value;
			else if (value is uint?)
				result = (uint?)value;
			else if (value is long)
				result = (long)value;
			else if (value is long?)
				result = (long?)value;
			else if (value is ulong)
				result = (ulong)value;
			else if (value is ulong?)
				result = (ulong?)value;
			else if (value is float)
				result = (float)value;
			else if (value is float?)
				result = (float?)value;
			else if (value is double)
				result = (double)value;
			else if (value is double?)
				result = (double?)value;
			else if (value is bool)
				result = (bool)value;
			else if (value is bool?)
				result = (bool?)value;
			else if (value is DateTime)
				result = (DateTime)value;
			else if (value is DateTime?)
				result = (DateTime?)value;
			else if (value is TimeSpan)
				result = ((TimeSpan)value).ToString("c");
			else if (value is Enum)
				result = ((Enum)value).ToString();
			else if (value as object == null)
				result = null;
			else if (value.GetType().IsClass)
				result = ObjectToJson(value);
			return result;
		}

		public static JsonValue ObjectToJson(object obj)
		{
			JsonValue result = null;
			var enumerable = obj as IEnumerable;
			if (enumerable != null)
			{
				var jsonArray = new JsonArray();
				result = jsonArray;
				foreach (var enumObj in enumerable)
					jsonArray.Add(ObjectToJson(enumObj));
			}
			else
			{
				var jsonObject = new JsonObject();
				result = jsonObject;
				foreach (var field in obj.GetType().GetFields())
					result[field.Name] = NativeToJsonValue(field.GetValue(obj));
				foreach (var prop in obj.GetType().GetProperties())
				{
					if (prop.GetIndexParameters().Length > 0)
						continue;
					result[prop.Name] = NativeToJsonValue(prop.GetValue(obj));
				}
			}
			return result;
		}
	}
}

