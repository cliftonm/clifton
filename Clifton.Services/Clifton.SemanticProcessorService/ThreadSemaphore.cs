using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Clifton.SemanticProcessorInterfaces;

namespace Clifton.SemanticProcessorService
{
	public class ThreadSemaphore<T>
	{
		public int Count { get { return requests.Count; } }
		protected Semaphore sem;

		// Requests on this thread.
		protected ConcurrentQueue<T> requests;

		public ThreadSemaphore()
		{
			sem = new Semaphore(0, Int32.MaxValue);
			requests = new ConcurrentQueue<T>();
		}

		public void Enqueue(T context)
		{
			requests.Enqueue(context);
			sem.Release();
		}

		public void WaitOne()
		{
			sem.WaitOne();
		}

		public bool TryDequeue(out T context)
		{
			return requests.TryDequeue(out context);
		}
	}
}
