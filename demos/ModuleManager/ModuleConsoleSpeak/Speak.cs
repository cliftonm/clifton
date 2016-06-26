using System;

using Clifton.Core.ModuleManagement;

namespace ModuleConsoleSpeak
{
    public class Speak : IModule
    {
		public void Say(string text)
		{
			Console.WriteLine(text);
		}
    }
}
