using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace AppConfigDecryptionService
{
    public class AppConfigDecryptionModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<IAppConfigDecryption, AppConfigDecryptionService>(d =>
            {
                d.Password = "somepassword";
                d.Salt = "somesalt";
            });
        }
    }

    public class AppConfigDecryptionService : ServiceBase, IAppConfigDecryption
    {
        public string Password { get; set; }
        public string Salt { get; set; }

        public string Decrypt(string text)
        {
            return text.Decrypt(Password, Salt);
        }
    }
}
