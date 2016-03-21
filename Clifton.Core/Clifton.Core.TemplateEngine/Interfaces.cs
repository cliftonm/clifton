using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.TemplateEngine
{
	public interface ITestRuntimeAssembly
	{
		string HelloWorld();
		string Print(string something);
	}

	public interface IRuntimeAssembly
	{
		string GetTemplate(object[] paramList = null);
	}
}
