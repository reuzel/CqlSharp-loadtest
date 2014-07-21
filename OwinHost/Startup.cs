using System;
using System.Diagnostics;
using Microsoft.Owin;
using Newtonsoft.Json;
using NLog;
using Owin;

[assembly: OwinStartup(typeof(OwinHost.Startup))]
	
namespace OwinHost
{
	public class Startup
	{

		private static Logger Log = LogManager.GetLogger("provider");
		public void Configuration(IAppBuilder app)
		{
			var provider = new Provider();
			app.UseHandlerAsync((req, res) =>
				{
					Log.Trace("new request arrived");					
					var path = req.Path.Remove(0, 9);
					var itemId = path.Substring(0, path.IndexOf('/'));
					var relation = path.Substring(path.IndexOf('/')+1);
					//var data = CallTimed(() => provider.Get("ozon", itemId, relation).Result);
					var data = CallTimed(() => {
						var task = provider.Get("ozon", itemId, relation);
						task.ConfigureAwait(false);
						task.Wait();
						return task.Result;
					});
					res.ContentType = "application/json";
					return res.WriteAsync(JsonConvert.SerializeObject(data));
				});
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