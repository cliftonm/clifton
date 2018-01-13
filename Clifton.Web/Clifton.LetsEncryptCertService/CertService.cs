// #define TESTING
// #define STAGING
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

// IMPORTANT: This requires that the folder acme.net exists in the application's bin folder.
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.LetsEncryptCertService
{
    public class CertServiceModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<ICertService, CertificateService>();
        }
    }

    public class CertificateService : ServiceBase, ICertService
    {
        public event EventHandler<EventArgs> RegistrationResult;
        public const int ONE_MINUTE = 60 * 1000;
        public const int DAYS_BEFORE_EXPIRATION = 30;

        public bool Success => success;
        public string Log => log.ToString();

        protected bool success;
        protected string websiteName;
        protected string certPassword;
        protected StringBuilder log;
        protected AcmeChallengeServer server;
        protected ILoggerService logger;
        protected CertRegistrationMethod registrationMethod;

        public override void FinishedInitialization()
        {
            logger = ServiceManager.Get<ILoggerService>();
            base.FinishedInitialization();
        }

        public void StartCertificateMonitor(CertRegistrationMethod registrationMethod)
        {
            this.registrationMethod = registrationMethod;
            websiteName = ServiceManager.Get<IAppConfigService>().GetValue("website");
            certPassword = ServiceManager.Get<IAppConfigService>().GetValue("certPassword");
            log = new StringBuilder();

            Task.Run(() =>
            {
                try
                {
                    CheckCertificate();
                    Thread.Sleep(ONE_MINUTE);
                }
                catch (Exception ex)
                {
                    logger.Log(LogMessage.Create("CERTIFICATE PROCESSING ERROR:" + ex.Message + "\r\n" + ex.StackTrace));
                }
            });
        }

        protected bool CheckCertificate()
        {
            bool ok = false;
            List<CertificateExpiration> certs = GetCertificateExpirationDates();

            CertificateExpiration cert = certs.SingleOrDefault(c => c.Subject == websiteName);

#if TESTING
            if (cert != null)
            {
                // Testing an existing cert for 30 days remaining.
                cert.ExpirationDate = DateTime.Now.AddDays(-(DAYS_BEFORE_EXPIRATION + 1));
            }
#endif

            if (cert == null || Expiring(cert))
            {
                Renew();
            }

            return ok;
        }

        protected bool Expiring(CertificateExpiration cert)
        {
            return DateTime.Now.AddDays(DAYS_BEFORE_EXPIRATION) > cert.ExpirationDate;
        }

        protected void Renew()
        {
            log.Clear();

            try
            {
                RemoveOldCertificate();
                bool ok = StartCertificateServer();

                if (ok)
                {
                    server.Start(GetLocalIP());
                    Process p = LaunchAcmeDotNet(websiteName, certPassword);

                    p.Exited += (snd, args) =>
                    {
                        server.Stop();

                        // Unfortunately, we don't get an exit code on error.
                        // int ret = p.ExitCode;
                        if (!log.ToString().Contains("acme:error:"))
                        {
                            if (registrationMethod == CertRegistrationMethod.NETSH)
                            {
                                CertRegistration.Register(websiteName, certPassword, s => log.AppendLine(s));

                                if (log.ToString().Contains("SSL Certificate successfully added"))
                                {
                                    success = true;
                                }
                                else
                                {
                                    success = false;
                                }
                            }
                        }
                        else
                        {
                            success = false;
                        }

                        RegistrationResult?.Invoke(this, EventArgs.Empty);
                    };
                }
            }
            catch (Exception ex)
            {
                success = false;
                log.AppendLine(ex.Message);
                RegistrationResult?.Invoke(this, EventArgs.Empty);
            }
        }

        protected string GetLocalIP()
        {
            return GetLocalHostIPs().First().ToString();
        }

        protected List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();

            return ret;
        }

        protected void RemoveOldCertificate()
        {
            CertRegistration.RemoveCert(StoreName.My, websiteName);
            CertRegistration.RemoveCert(StoreName.AuthRoot, websiteName);
        }

        protected bool StartCertificateServer()
        {
            bool ok = true;
            server = new AcmeChallengeServer();
            server.ServerException += (_, args) => ok = false;

            return ok;
        }

        protected List<CertificateExpiration> GetCertificateExpirationDates()
        {
            List<CertificateExpiration> certExpirations = new List<CertificateExpiration>();
            // My netsh binding expects certs here:
            GetCertificatesIn(StoreName.Root, certExpirations);

            // acme.net puts IIS certs here:
            GetCertificatesIn(StoreName.My, certExpirations);

            return certExpirations;
        }

        protected void GetCertificatesIn(StoreName storeName, List<CertificateExpiration> certs)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly);

            foreach (var cert in store.Certificates)
            {
                if (cert.Issuer.Contains("Let's Encrypt Authority"))
                {
                    certs.Add(new CertificateExpiration()
                    {
                        Subject = cert.Subject.RightOf("CN="),
                        ExpirationDate = cert.NotAfter,
                    });
                }
            }

            store.Close();
        }

        protected Process LaunchAcmeDotNet(string domainName, string certPassword)
        {
            // -a: accept terms of service
            // -j: accept instructions (since we're running a server that will accept the challenge
            // -d: the domain
            // -p: the password for the cert.
            // -c: challenge provider (manual, not iis)
            // -i: server configuration provider (manual, not iis)
            // -s: letsencrypt server.  For staging server, use: https://acme-staging.api.letsencrypt.org

#if STAGING
            string staging = "-s https://acme-staging.api.letsencrypt.org ";
#else
            string staging = String.Empty;
#endif
            string manualOptions = "-c manual-http-01 -i manual";       // challenge is handled by our micro server, not IIS, and manaul configuration of certificate
            string iisOptions = "-c manual-http-01 -i iis";             // challenge is handled by our micro server but cert is installed and configured in IIS
            string options = registrationMethod == CertRegistrationMethod.IIS ? iisOptions : manualOptions;
            Process p = LaunchProcess(@"acme.net\acme.exe", String.Format(staging + "-a -j -d {0} -p {1} {2}", domainName, certPassword, options),
                stdout => log.AppendLine(stdout),
                stderr => log.AppendLine(stderr));

            return p;
        }

        protected Process LaunchProcess(string processName, string arguments, Action<string> onOutput, Action<string> onError = null)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.FileName = processName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += (sndr, args) => { if (args.Data != null) onOutput(args.Data); };

            if (onError != null)
            {
                p.ErrorDataReceived += (sndr, args) => { if (args.Data != null) onError(args.Data); };
            }

            p.Start();

            // Interestingly, this has to be called after Start().
            p.EnableRaisingEvents = true;
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }
    }
}
