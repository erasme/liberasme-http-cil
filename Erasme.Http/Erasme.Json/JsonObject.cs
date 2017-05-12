// JsonObject.cs
// 
//  Define a JSON object
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013-2014 Departement du Rhone
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
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;

namespace Erasme.Json
{
	public class JsonObject: JsonValue, IDictionary<string, JsonValue>, IDictionary
	{
		readonly Dictionary<string, JsonValue> hash = new Dictionary<string, JsonValue>();

		public JsonObject()
		{
		}

		public JsonObject(IEnumerable<KeyValuePair<string, JsonValue>> values)
		{
			foreach(KeyValuePair<string, JsonValue> pair in values) {
				hash.Add(pair.Key, pair.Value);
			}
		}

		public JsonObject(IEnumerable<KeyValuePair<string, object>> values)
		{
			foreach (KeyValuePair<string, object> pair in values)
			{
				if (pair.Value is JsonValue)
					hash.Add(pair.Key, (JsonValue)pair.Value);
				else
					hash.Add(pair.Key, new JsonPrimitive(pair.Value));
			}
		}

		public JsonObject(KeyValuePair<string, JsonValue>[] values)
		{
			foreach(KeyValuePair<string, JsonValue> pair in values) {
				hash.Add(pair.Key, pair.Value);
			}
		}

		public override JsonType JsonType {
			get {
				return JsonType.Object;
			}
		}

		public override int Count {
			get {
				return hash.Count;
			}
		}

		public override JsonValue this[string key] {
			get {
				return hash[key];
			}
			set {
				hash[key] = value;
			}
		}

		object IDictionary.this[object key]
		{
			get {
				return hash[(string)key];
			}
			set {
				hash[(string)key] = (JsonValue)value;
			}
		}

		public override ICollection<string> Keys {
			get {
				return hash.Keys;
			}
		}

		ICollection IDictionary.Keys
		{
			get {
				return ((IDictionary)hash).Keys;
			}
		}

		public override ICollection<JsonValue> Values {
			get {
				return hash.Values;
			}
		}

		ICollection IDictionary.Values {
			get {
				return hash.Values;
			}
		}

		public void Clear()
		{
			hash.Clear();
		}

		public override bool ContainsKey(string key)
		{
			return hash.ContainsKey(key);
		}

		bool IDictionary.Contains(object key)
		{
			return hash.ContainsKey((string)key);
		}

		public bool Contains(KeyValuePair<string, JsonValue> pair)
		{
			return hash.ContainsKey(pair.Key);
		}

		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
		{
			return hash.GetEnumerator();
		}

		public void Add(string key, JsonValue value)
		{
			hash.Add(key, value);
		}

		void IDictionary.Add(object key, object value)
		{
			hash.Add((string)key, (JsonValue)value);
		}

		public void Add(KeyValuePair<string, JsonValue> pair)
		{
			hash.Add(pair.Key, pair.Value);
		}

		public bool Remove(string key)
		{
			return hash.Remove(key);
		}

		public bool Remove(KeyValuePair<string, JsonValue> pair)
		{
			return hash.Remove(pair.Key);
		}

		public void Remove(object key)
		{
			hash.Remove((string)key);
		}

		public bool TryGetValue(string key, out JsonValue value)
		{
			return hash.TryGetValue(key, out value);
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		bool ICollection.IsSynchronized
		{
			get {
				return ((ICollection)hash).IsSynchronized;
			}
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return ((IDictionary)hash).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)hash).GetEnumerator();
		}

		public void CopyTo(KeyValuePair<string,JsonValue>[] array, int arrayIndex)
		{
			foreach(KeyValuePair<string,JsonValue> pair in this)
				array[arrayIndex++] = pair;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if(hash.ContainsKey(binder.Name)) {
				result = hash[binder.Name];
				return true;
			}
			else {
				result = null;
				return false;
			}
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			hash[binder.Name] = (JsonValue)value;
			return true;
		}

		bool IDictionary.IsFixedSize
		{
			get {
				return false;
			}
		}

		bool IDictionary.IsReadOnly
		{
			get {
				return false;
			}
		}

		object ICollection.SyncRoot
		{
			get {
				return ((ICollection)hash).SyncRoot;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)hash).CopyTo(array, index);
		}
	}
}

