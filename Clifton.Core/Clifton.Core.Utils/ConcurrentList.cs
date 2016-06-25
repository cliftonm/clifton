/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Clifton.Core.Utils
{
	// http://stackoverflow.com/questions/6601611/no-concurrentlistt-in-net-4-0
	public class ConcurrentList<T> : IList<T>, IDisposable
	{
		private readonly List<T> _list;
		private readonly ReaderWriterLockSlim _lock;

		public ConcurrentList()
		{
			this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			this._list = new List<T>();
		}

		public ConcurrentList(int capacity)
		{
			this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			this._list = new List<T>(capacity);
		}

		public ConcurrentList(IEnumerable<T> items)
		{
			this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			this._list = new List<T>(items);
		}

		public void Add(T item)
		{
			try
			{
				this._lock.EnterWriteLock();
				this._list.Add(item);
			}
			finally
			{
				this._lock.ExitWriteLock();
			}
		}

		public void Insert(int index, T item)
		{
			try
			{
				this._lock.EnterWriteLock();
				this._list.Insert(index, item);
			}
			finally
			{
				this._lock.ExitWriteLock();
			}
		}

		public bool Remove(T item)
		{
			try
			{
				this._lock.EnterWriteLock();
				return this._list.Remove(item);
			}
			finally
			{
				this._lock.ExitWriteLock();
			}
		}

		public void RemoveAt(int index)
		{
			try
			{
				this._lock.EnterWriteLock();
				this._list.RemoveAt(index);
			}
			finally
			{
				this._lock.ExitWriteLock();
			}
		}

		public int IndexOf(T item)
		{
			try
			{
				this._lock.EnterReadLock();
				return this._list.IndexOf(item);
			}
			finally
			{
				this._lock.ExitReadLock();
			}
		}

		public void Clear()
		{
			try
			{
				this._lock.EnterWriteLock();
				this._list.Clear();
			}
			finally
			{
				this._lock.ExitWriteLock();
			}
		}

		public bool Contains(T item)
		{
			try
			{
				this._lock.EnterReadLock();
				return this._list.Contains(item);
			}
			finally
			{
				this._lock.ExitReadLock();
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			try
			{
				this._lock.EnterReadLock();
				this._list.CopyTo(array, arrayIndex);
			}
			finally
			{
				this._lock.ExitReadLock();
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new ConcurrentEnumerator<T>(this._list, this._lock);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ConcurrentEnumerator<T>(this._list, this._lock);
		}

		~ConcurrentList()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			this._lock.Dispose();
		}

		public T this[int index]
		{
			get
			{
				try
				{
					this._lock.EnterReadLock();
					return this._list[index];
				}
				finally
				{
					this._lock.ExitReadLock();
				}
			}
			set
			{
				try
				{
					this._lock.EnterWriteLock();
					this._list[index] = value;
				}
				finally
				{
					this._lock.ExitWriteLock();
				}
			}
		}

		public int Count
		{
			get
			{
				try
				{
					this._lock.EnterReadLock();
					return this._list.Count;
				}
				finally
				{
					this._lock.ExitReadLock();
				}
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}
	}

	public class ConcurrentEnumerator<T> : IEnumerator<T>
	{
		private readonly IEnumerator<T> _inner;
		private readonly ReaderWriterLockSlim _lock;

		public ConcurrentEnumerator(IEnumerable<T> inner, ReaderWriterLockSlim @lock)
		{
			this._lock = @lock;
			this._lock.EnterReadLock();
			this._inner = inner.GetEnumerator();
		}

		T IEnumerator<T>.Current
		{
			get { return _inner.Current; }
		}

		void IDisposable.Dispose()
		{
			this._lock.ExitReadLock();
		}

		object IEnumerator.Current
		{
			get { return _inner.Current; }
		}

		bool IEnumerator.MoveNext()
		{
			return _inner.MoveNext();
		}

		void IEnumerator.Reset()
		{
			_inner.Reset();
		}
	}
}
