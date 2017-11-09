using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Clifton.Core.ExtensionMethods;

namespace Clifton.LetsEncryptCertService
{
    public class ServerExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }

    public class AcmeChallengeServer
    {
        public event EventHandler<ServerExceptionEventArgs> ServerException;

        protected bool running = true;
        protected HttpListener listener;

        public void Start(string localIP)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://" + localIP + "/");
            listener.Start();
            Task.Run(() => WaitForConnection(listener));
        }

        public void Stop()
        {
            running = false;
            listener.Stop();
        }

        private void WaitForConnection(object objListener)
        {
            while (running)
            {
                HttpListenerContext context;

                try
                {
                    context = listener.GetContext();
                }
                catch (HttpListenerException)
                {
                    // Occurs when we stop the listener.
                    break;
                }
                catch (Exception ex)
                {
                    ServerException?.Invoke(this, new ServerExceptionEventArgs() { Exception = ex });
                    // Other exceptions should be handled elsewhere.
                    break;
                }

                if (context.Request.RawUrl.StartsWith("/.well-known/acme-challenge"))
                {
                    string challengeFile = context.Request.RawUrl.RightOfRightmostOf('/');

                    if (File.Exists(challengeFile))
                    {
                        string data = File.ReadAllText(challengeFile);

                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/text";
                        context.Response.ContentEncoding = Encoding.UTF8;

                        byte[] byteData = Encoding.ASCII.GetBytes(data);
                        context.Response.ContentLength64 = byteData.Length;
                        context.Response.OutputStream.Write(byteData, 0, byteData.Length);
                    }
                }

                context.Response.Close();
            }
        }
    }
}
