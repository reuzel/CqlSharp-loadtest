using System;
using Microsoft.Owin.Hosting;

namespace SimpleOwinHost
{
	class Program
	{
		static void Main(string[] args)
		{
		 string uri = "http://localhost";
 
			using (WebApp.Start<OwinHost.Startup>(uri))
			{
					Console.WriteLine("Started");
					Console.ReadKey();
					Console.WriteLine("Stopping");
			}
		}
	}
}
