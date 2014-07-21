using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using NLog;
using OwinHost;

namespace LoadTester
{
	class Program
	{
		private static Logger Log = LogManager.GetLogger("provider");

		public class Options
		{
			[Option('s', "single", DefaultValue = true, HelpText = "Тест с одиночными выборками")]
			public bool DoSingle { get; set; }


			[Option("report-totals", DefaultValue = true, HelpText = "Выводить ли суммарное время по планам")]
			public bool ReportTotals { get; set; }

			[Option("report-slowest", DefaultValue = false, HelpText = "Выводить ли список (5 штук) самых медленных тасков в плане")]
			public bool ReportSlowest { get; set; }

			[Option("trace", DefaultValue = false, HelpText = "Выводить ли trace")]
			public bool IstracingEnabled { get; set; }

			[Option("count", DefaultValue = 1000, HelpText = "Количество итераций теста")]
			public int IterationsCount { get; set; }

			[Option("threads", DefaultValue = 20, HelpText = "Количество потоков")]
			public int ThreadCount { get; set; }

			[Option("appid", DefaultValue = "ozon", HelpText = "ID приложения")]
			public string ApplicationId { get; set; }

			[Option("fill-file", HelpText = "Файл с данными для заполнения")]
			public string FillFile { get; set; }

			[Option("url", DefaultValue = null, HelpText = "Обращаться к rest api")]
			public string HttpUrl { get; set; }

			[Option("sla", DefaultValue = 0, HelpText = "Провайдер для подключения к кассандре")]
			public double Sla { get; set; }
		}

		static void Main(string[] args)
		{
			var options = new Options();
			var ids = new[] { "20702362", "8791176", "19426582", "25969427", "19875072", "17413827", "4238394", "19999983", "24926433", "3771061", "3260133", "6969727", "7402881", "3189164", "5659077", "22838624", "7514760", "7293646", "4792995", "6264180", "18046522", "2662465", "4742592", "6246942", "25893775", "24050407", "21433818", "144350", "4413963", "19578541", "6046979", "3213902" };

			if (!CommandLine.Parser.Default.ParseArguments(args, options))
				return;

			Provider data = null;
			SimpleClient client = null;

			if (string.IsNullOrEmpty(options.HttpUrl))
			{
				data = new Provider();
			}
			else
			{
				client = new SimpleClient(options.HttpUrl);
			}

			var fSingle = string.IsNullOrEmpty(options.HttpUrl) 
				? new Func<string, string, string, Task<List<string>>>( async (appid, item, rel) => await data.Get(appid, item, rel))
				: new Func<string, string, string, Task<List<string>>>( async (appid, item, rel) => await client.GetAsync(item, rel));

			var runner = new TestRunner(options.ThreadCount, Console.Out);
			runner.SlaTarget = options.Sla;

			if (options.IstracingEnabled)
				runner.Log = LogManager.GetLogger("TestRunner");

			runner.AddPlan(new TestPlan("warmup",
				options.IterationsCount,
				counter =>
				{
					var res = fSingle(options.ApplicationId, ids[counter % 10], "vv").Result;
					if (res == null)
						throw new ArgumentNullException();
				}) { IsWarmUp = true });


			if (options.DoSingle)
			{
				runner.AddPlan("vv single",
					options.IterationsCount,
					counter =>
					{
						var res = fSingle(options.ApplicationId, ids[counter % 10], "vv").Result;
						if (res == null)
							throw new ArgumentNullException();
					});
			}

			var i = fSingle("ozon", ids[0], "vv").Result;
			if (i == null)
				return;

			runner.Start();
			var opts = ReportOptions.None;
			if (options.ReportTotals)
				opts = opts | ReportOptions.Totals;
			if (options.ReportSlowest)
				opts = opts | ReportOptions.Slowest;
			runner.Report(opts);
		}
	}
}