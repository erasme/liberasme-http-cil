// JsonArray.cs
// 
//  Define a JSON array
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
using System.Collections;
using System.Collections.Generic;

namespace Erasme.Json
{
	public class JsonArray: JsonValue, IList<JsonValue>
	{
		List<JsonValue> list = new List<JsonValue>();

		public JsonArray()
		{
		}

		public override JsonType JsonType {
			get {
				return JsonType.Array;
			}
		}

		public override JsonValue this[int index] {
			get {
				return list[index];
			}
			set {
				list[index] = value;
			}
		}

		public override int Count {
			get {
				return list.Count;
			}
		}

		public override ICollection<JsonValue> Values {
			get {
				return (ICollection<JsonValue>)list;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public void Add(JsonValue item)
		{
			list.Add(item);
		}

		public void AddRange(IEnumerable<JsonValue> items)
		{
			list.AddRange(items);
		}

		public bool Contains(JsonValue item)
		{
			return list.Contains(item);
		}

		public void Clear()
		{
			list.Clear();
		}

		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public IEnumerator<JsonValue> GetEnumerator()
		{
			return (IEnumerator<JsonValue>)list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)list.GetEnumerator();
		}

		public int IndexOf(JsonValue item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, JsonValue item)
		{
			list.Insert(index, item);
		}

		public bool Remove(JsonValue item)
		{
			return list.Remove(item);
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}
	}
}

