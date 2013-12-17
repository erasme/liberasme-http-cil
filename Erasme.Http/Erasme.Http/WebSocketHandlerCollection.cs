// WebSocketHandlerCollection.cs
// 
//  Helper class to handle a collection of WebSocketHandler
//  and broadcast messages.
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

namespace Erasme.Http
{
	public class WebSocketHandlerCollection<T>: IEnumerable<T> where T: WebSocketHandler
	{
		object instanceLock = new object();
		List<T> list = new List<T>();

		public WebSocketHandlerCollection()
		{
		}

		public int Count {
			get {
				lock(instanceLock) {
					return list.Count;
				}
			}
		}

		public void Add(T item)
		{
			lock(instanceLock) {
				list.Add(item);
			}
		}

		public void Remove(T item)
		{
			lock(instanceLock) {
				list.Remove(item);
			}
		}

		public void Broadcast(string message)
		{
			// copy the handlers list
			T[] localList;
			lock(instanceLock) {
				localList = list.ToArray();
			}
			// TODO: auto-remove a handler if its connection is lost
			foreach(WebSocketHandler handler in localList)
				handler.Send(message);
		}

		public void Broadcast(byte[] message)
		{
			// copy the handlers list
			T[] localList;
			lock(instanceLock) {
				localList = list.ToArray();
			}
			// TODO: auto-remove a handler if its connection is lost
			foreach(WebSocketHandler handler in localList)
				handler.Send(message);
		}

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			IEnumerator<T> enumerator;
			lock(instanceLock) {
				List<T> listCopy = new List<T>(list);
				enumerator = ((IEnumerable<T>)listCopy).GetEnumerator();
			}
			return enumerator;
		}
	}
}
