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
using System.Collections.Concurrent;
using System.Threading;

namespace Clifton.Core.ThreadQueue
{
	public class ThreadedQueue<T>
	{
		protected Action<T> processAction;
		protected ConcurrentQueue<T> queue;
		protected Thread thread;
		protected Semaphore sem;

		public ThreadedQueue(Action<T> processor)
		{
			processAction = processor;
			queue = new ConcurrentQueue<T>();
			sem = new Semaphore(0, Int32.MaxValue);
			thread = new Thread(new ParameterizedThreadStart(ProcessQueueItem));
			thread.IsBackground = true;
		}

		public void Start()
		{
			thread.Start();
		}

		public void Enqueue(T item)
		{
			queue.Enqueue(item);
			sem.Release();
		}

		protected void ProcessQueueItem(object arg)
		{
			while (true)
			{
				sem.WaitOne();
				T item;

				if (queue.TryDequeue(out item))
				{
					processAction(item);
				}
			}
		}
	}
}
