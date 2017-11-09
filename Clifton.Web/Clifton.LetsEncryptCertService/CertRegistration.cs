using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

// Other notes:
// certlm.msc - directly launch certificate management plugin.

namespace Clifton.LetsEncryptCertService
{
    public static class CertRegistration
    {
        public static void Register(string subjectName, string password, Action<string> log)
        {
            // string tempCertFilename = "newcert.pfx";		// temp file for moving cert around.
            Guid appId = Guid.NewGuid();

            RemoveCert(StoreName.My, subjectName);
            RemoveCert(StoreName.Root, subjectName);

            // http://stackoverflow.com/questions/899991/x509certificate-createfromcertfile-the-specified-network-password-is-not-corre
            /*
                Lesson learned...
                .cer files are an X.509 certificate in binary form. They are DER encoded.
                .pfx files are container files. Also DER encoded. They contain not only certificates, but also private keys in encrypted form.            
            */

            string certHash;
            // Suddenly this started working with pfx certs.  Before I was getting an exception that the private key could not be found in the local store!
            log("Importing certificate into Personal Certificates...");
            //string sn = ImportCert(StoreName.My, subjectName + ".pfx", password, out certHash);


            string sn = ImportCert(StoreName.AuthRoot, subjectName + ".pfx", password, out certHash);
            log("Removing old binding...");
            RemoveBinding(log);
            log("Adding new binding...");
            AddNewBinding(certHash, appId, log);

            /*
			// https://www.codeproject.com/Articles/1068443/Setting-up-an-Amazon-EC-Instance-with-an-SSH-Serve
			// This step is not necessary.
			// log("Repairing certificate...");
			// RepairCert("My", sn, log);

			byte[] certBytes = ExportCert(StoreName.My, sn, password, log);

			if (certBytes != null)
			{
				// http://paulstovell.com/blog/x509certificate2  Tip #5
				File.WriteAllBytes(tempCertFilename, certBytes);
				log("Importing certificate into Trusted Root Certification Authorities...");
				ImportCert(StoreName.AuthRoot, tempCertFilename, password, out certHash);
				File.Delete(tempCertFilename);

				log("Removing old binding...");
				RemoveBinding(log);
				log("Adding new binding...");
				AddNewBinding(certHash, appId, log);
			}
			*/
        }

        public static void RemoveCert(StoreName storeName, string subjectName)
        {
            // http://stackoverflow.com/questions/7632757/how-to-remove-certificate-from-store-cleanly
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite | OpenFlags.IncludeArchived);
            X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);

            foreach (var cert in certCollection)
            {
                store.Remove(certCollection[0]);
            }

            store.Close();
        }

        static string ImportCert(StoreName storeName, string certFile, string password, out string certHash)
        {
            // https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509keystorageflags(v=vs.110).aspx
            // Exportable: http://paulstovell.com/blog/x509certificate2
            // Must specify MachineKeySet otherwise you'll get a "SSL Certificate add failed, Error 1312" error.
            X509Certificate2 certToImport = new X509Certificate2(certFile, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.MaxAllowed);
            store.Add(certToImport);
            store.Close();

            certHash = certToImport.Thumbprint; // .GetCertHashString();

            return certToImport.SerialNumber;
        }

        static void RepairCert(string storeName, string sn, Action<string> log)
        {
            Process p = ProcessLauncher.LaunchProcess("certutil", "-repairstore " + storeName + " \"" + sn + "\"",
                (stdout) => log(stdout),
                (stderr) => log(stderr));

            p.WaitForExit();
        }

        static void RemoveBinding(Action<string> log)
        {
            // netsh http delete sslcert ipport=0.0.0.0:443
            Process p = ProcessLauncher.LaunchProcess("netsh", "http delete sslcert ipport=0.0.0.0:443",
                (stdout) => log(stdout),
                (stderr) => log(stderr));

            p.WaitForExit();
        }

        static void AddNewBinding(string certHash, Guid appId, Action<string> log)
        {
            // netsh http add sslcert ipport=0.0.0.0:443 certstorename=Root certhash=[] appid={}
            // Why root?
            // https://stackoverflow.com/questions/13076915/ssl-certificate-add-failed-when-binding-to-port/19766650#19766650 (see Fredy Wenger's response)
            Process p = ProcessLauncher.LaunchProcess("netsh", "http add sslcert ipport=0.0.0.0:443 certstorename=Root certhash=" + certHash + " appid={" + appId.ToString() + "}",
                (stdout) => log(stdout),
                (stderr) => log(stderr));

            p.WaitForExit();
        }

        static byte[] ExportCert(StoreName storeName, string sn, string password, Action<string> log)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindBySerialNumber, sn, false);
            store.Close();

            if (certCollection.Count != 1)
            {
                log("Did not find certificate by serial number");
                return null;
            }

            X509Certificate2 cert = certCollection[0];

            // To secure your exported certificate use the following overload of the Export function:
            byte[] certBytes = cert.Export(X509ContentType.Pkcs12, password);

            return certBytes;
        }
    }
}

