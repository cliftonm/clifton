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
using System.IO;
using System.Net;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Workflow;

using Clifton.WebInterfaces;

namespace WebWorkflowService
{
	public class WebWorkflowModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebWorkflowService, WebWorkflow>();
		}
	}

	public class WebWorkflow : ServiceBase, IWebWorkflowService
	{
		protected Workflow<PreRouteWorkflowData> preRouteWorkflow;
		protected Workflow<PostRouteWorkflowData> postRouteWorkflow;

		public WebWorkflow()
		{
			preRouteWorkflow = new Workflow<PreRouteWorkflowData>();
			postRouteWorkflow = new Workflow<PostRouteWorkflowData>();
		}

		public void RegisterPreRouterWorkflow(WorkflowItem<PreRouteWorkflowData> item)
		{
			preRouteWorkflow.AddItem(item);
		}

		public void RegisterPostRouterWorkflow(WorkflowItem<PostRouteWorkflowData> item)
		{
			postRouteWorkflow.AddItem(item);
		}

		public bool PreRouter(HttpListenerContext context)
		{
			WorkflowState state = preRouteWorkflow.Execute(new PreRouteWorkflowData(context));

			return state == WorkflowState.Done;
		}

		public bool PostRouter(HttpListenerContext context, HtmlResponse response)
		{
			WorkflowState state = postRouteWorkflow.Execute(new PostRouteWorkflowData(ServiceManager, context, response));

			return state == WorkflowState.Done;
		}
	}
}

