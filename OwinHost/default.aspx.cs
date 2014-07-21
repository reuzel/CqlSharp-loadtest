using System;
using System.Diagnostics;
using Newtonsoft.Json;
using NLog;

namespace OwinHost
{
	public partial class _default : System.Web.UI.Page
	{
		private Provider provider = new Provider();
		private static Logger Log = LogManager.GetLogger("provider");

		protected void Page_Load(object sender, EventArgs e)
		{
			var data = CallTimed(() => provider.Get("ozon", RouteData.Values["item"] as string, RouteData.Values["relation"] as string).Result);
			Response.ContentType = "application/json";
			Response.Write(JsonConvert.SerializeObject(data));
			Response.Flush();
			Response.End();
		}

		private T CallTimed<T>(Func<T> caller)
		{
			var timer = new Stopwatch();

			timer.Start();
			var result = caller();
			timer.Stop();
			Log.Trace("Time in provider: {0}ms", timer.ElapsedMilliseconds);
			return result;
		}
	}
}