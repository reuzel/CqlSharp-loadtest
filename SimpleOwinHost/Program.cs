using System;
using Microsoft.Owin.Hosting;

namespace SimpleOwinHost
{
	class Program
	{
		static void Main(string[] args)
		{
			string uri = "http://localhost";

			if (args.Length > 0)
				uri = args[0];

			using (WebApp.Start<OwinHost.Startup>(uri))
			{
				Console.WriteLine("Started");
				Console.ReadKey();
				Console.WriteLine("Stopping");
			}
		}
	}
}
