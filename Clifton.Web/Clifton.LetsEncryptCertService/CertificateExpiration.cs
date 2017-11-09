using System;

namespace Clifton.LetsEncryptCertService
{
    public class CertificateExpiration
    {
        public string Subject { get; set; }
        public DateTime ExpirationDate { get; set; }

        public override string ToString()
        {
            return Subject + " : " + ExpirationDate.ToString();
        }
    }
}
