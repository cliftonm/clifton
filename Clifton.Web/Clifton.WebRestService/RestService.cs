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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.WebRestService
{
    public class WebRestModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<IWebRestService, WebRestService>();
        }
    }

    public class WebRestService : ServiceBase, IWebRestService
    {
        public const int TIMEOUT = 150000;
        public string LastJson;
        public string LastRetJson;

        public R Get<R>(string url) where R : IRestResponse
        {
            string ret = String.Empty;
            WebResponse resp = null;
            R target = Activator.CreateInstance<R>();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = TIMEOUT;
                request.Method = "GET";

                resp = request.GetResponse();
                ret = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                LastJson = ret;
            }
            catch (Exception ex)
            {
                // TODO: Log Exception
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }

            JObject jobj = JObject.Parse(ret);
            JsonConvert.PopulateObject(jobj.ToString(), target);

            return target;
        }

        public R Post<R>(string url, object obj) where R : IRestResponse
        {
            R target = Activator.CreateInstance<R>();
            Stream st = null;
            string json = string.Empty;
            string retjson = string.Empty;

            try
            {
                // was: json = JsonConvert.SerializeObject(obj);
                json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                LastJson = json;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = TIMEOUT;
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = json.Length;
                st = request.GetRequestStream();
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                st.Write(bytes, 0, bytes.Length);

                WebResponse resp = request.GetResponse();
                retjson = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                LastRetJson = retjson;

                JObject jobj = JObject.Parse(retjson);
                JsonConvert.PopulateObject(jobj.ToString(), target);
            }
            catch (Exception ex)
            {
				if (ex.Source == "Newtonsoft.Json")
				{
                    target.RawJsonRet = retjson;
				}
                // TODO: Log Exception
            }
            finally
            {
                if (st != null)
                {
                    st.Close();
                }
            }

            return target;
        }
    }
}
