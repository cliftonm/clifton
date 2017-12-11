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

using System.Configuration;
using System.Linq;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Cores.Services.AppConfigService
{
	public class AppConfigModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IAppConfigService, ConfigService>();
		}
	}

	public class ConfigService : ServiceBase, IAppConfigService
	{
		public virtual string GetConnectionString(string key)
		{
			string text = ConfigurationManager.ConnectionStrings[key].ConnectionString;

			return DecryptOption(text);
		}

        public virtual bool KeyExists(string key)
        {
            return ConfigurationManager.AppSettings.AllKeys.Contains(key);
        }

        public virtual bool TryGetValue(string key, out string val)
        {
            val = null;
            bool found = false;

            if (KeyExists(key))
            {
                val = GetValue(key);
                found = true;
            }

            return found;
        }

		public virtual string GetValue(string key)
		{
			string text = ConfigurationManager.AppSettings[key];

			return DecryptOption(text);
		}

		protected string DecryptOption(string text)
		{
			if (text.BeginsWith("[e]"))
			{
				// Application must provide an IAppConfigDecryption service.
				text = ServiceManager.Get<IAppConfigDecryptionService>().Decrypt(text.Substring(3));
			}

			return text;
		}
	}
}
