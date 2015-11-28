using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Clifton.Core.Utils
{
	public static class RestCall
	{
		public static string Get(string url)
		{
			string ret = String.Empty;
			WebResponse resp = null;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";

			resp = request.GetResponse();
			ret = new StreamReader(resp.GetResponseStream()).ReadToEnd();
			resp.Close();

			return ret;
		}

		public static R Post<R>(string url, object obj)
		{
			R target = Activator.CreateInstance<R>();
			Stream st = null;
			string json = string.Empty;
			string retjson = string.Empty;

			json = JsonConvert.SerializeObject(obj);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/json";
			request.ContentLength = json.Length;
			st = request.GetRequestStream();
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			st.Write(bytes, 0, bytes.Length);
			WebResponse resp = request.GetResponse();
			retjson = new StreamReader(resp.GetResponseStream()).ReadToEnd();
			JObject jobj = JObject.Parse(retjson);
			JsonConvert.PopulateObject(jobj.ToString(), target);

			resp.Close();
			st.Close();

			return target;
		}
	}
}