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
		}

		public void Enqueue(T item)
		{
			queue.Enqueue(item);
			sem.Release();
		}

		public void ProcessQueueItem(object arg)
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
