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
using System.Collections.Generic;

namespace Clifton.Core.Workflow
{
	/// <summary>
	/// Workflow Continuation State
	/// </summary>
	public enum WorkflowState
	{
		/// <summary>
		/// Terminate execution of the workflow.
		/// </summary>
		Abort,

		/// <summary>
		/// Continue with the execution of the workflow.
		/// </summary>
		Continue,

		/// <summary>
		/// Execution is deferred until Continue is called, usually by another thread.
		/// </summary>
		Defer,

		/// <summary>
		/// The workflow should terminate without error.  The workflow step
		/// is indicating that it has handled the request and there is no further
		/// need for downstream processing.
		/// </summary>
		Done,

		/// <summary>
		/// An exception occurred during processing.
		/// </summary>
		Exception,

		/// <summary>
		/// Undefined state, should not occur, but could occur if a continue is attempted on an aborted or completed workflow.
		/// </summary>
		Undefined,
	}

	/// <summary>
	/// The Workflow class handles a list of workflow items that we can use to 
	/// determine the processing of a request.
	/// </summary>
	public class Workflow<T>
	{
		public Action<T> AbortHandler { get; protected set; }
		public Action<T, Exception> ExceptionHandler { get; protected set; }

		protected List<WorkflowItem<T>> items;

		public Workflow()
		{
			items = new List<WorkflowItem<T>>();
			AbortHandler = (_) => { };
			ExceptionHandler = (_a, _b) => { };
		}

		public Workflow(Action<T> abortHandler, Action<T, Exception> exceptionHandler)
		{
			items = new List<WorkflowItem<T>>();
            AbortHandler = abortHandler;
			ExceptionHandler = exceptionHandler;
		}

		/// <summary>
		/// Add a workflow item.
		/// </summary>
		public void AddItem(WorkflowItem<T> item)
		{
			items.Add(item);
		}

		/// <summary>
		/// Execute the workflow from the beginning.
		/// </summary>
		public WorkflowState Execute(T data)
		{
			WorkflowContinuation<T> continuation = new WorkflowContinuation<T>(this);
			WorkflowState state = InternalContinue(continuation, data);

			return state;
		}

		/// <summary>
		/// Continue a deferred workflow, unless it is aborted.
		/// </summary>
		public WorkflowState Continue(WorkflowContinuation<T> wc, T data)
		{
			WorkflowState state = WorkflowState.Undefined;

			// TODO: Throw exception instead?
			if ( (!wc.Abort) && (!wc.Done) )
			{
				wc.Defer = false;
				wc.Deferred = false;
				state = InternalContinue(wc, data);
			}

			return state;
		}

		/// <summary>
		/// Internally, we execute workflow steps until:
		/// 1. we reach the end of the workflow chain
		/// 2. we are instructed to abort the workflow
		/// 3. we are instructed to defer execution until later.
		/// </summary>
		protected WorkflowState InternalContinue(WorkflowContinuation<T> wc, T data)
		{
			WorkflowState state = WorkflowState.Done;

			while ((wc.WorkflowStep < items.Count) && !wc.Abort && !wc.Defer && !wc.Done)
			{
				try
				{
					state = items[wc.WorkflowStep++].Execute(wc, data);

					switch (state)
					{
						case WorkflowState.Abort:
							wc.Abort = true;
							wc.Workflow.AbortHandler(data);
							break;

						case WorkflowState.Defer:
							wc.Defer = true;
							break;

						case WorkflowState.Done:
							wc.Done = true;
							break;
					}
				}
				catch (Exception ex)
				{
					// Yes, the user's exception handler could itself through an exception
					// from which we need to protect ourselves.
					try
					{
						wc.Workflow.ExceptionHandler(data, ex);
					}
					catch { /* Now what? */ }
					// TODO: Should we use a different flag, like "Exception"?  Can't be Abort, as this invokes an app-specific handler.
					state = WorkflowState.Exception;
					wc.Done = true;
				}
			}

			if (wc.Defer)
			{
				// Synchronization, we're done with this loop and the workflow can now continue on another thread.
				wc.Deferred = true;
			}

			// If the loop exits and the last workflow state was continue, then we're actually now done.
			if (state == WorkflowState.Continue)
			{
				state = WorkflowState.Done;
			}

			return state;
		}
	}
}
