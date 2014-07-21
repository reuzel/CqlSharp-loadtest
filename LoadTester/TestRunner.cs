using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LoadTester
{
	public class TestRunner
	{
		public int ThreadCount { get; set; }
		public TextWriter Output { get; set; }

		public Logger Log { get; set; }

		public double SlaTarget { get; set; }

		private List<TestPlan> _plans = new List<TestPlan>();

		public TestRunner(int threadCount, TextWriter output)
		{
			ThreadCount = threadCount;
			Output = output;
		}

		private void Trace(string message, params object[] args)
		{
			if (this.Log != null)
				this.Log.Trace(message, args);
		}

		public void AddPlan(string name, int iterationsCount, Action<int> method)
		{
			_plans.Add(new TestPlan(name, iterationsCount, method));
		}

		public void AddPlan(TestPlan plan)
		{
			_plans.Add(plan);
		}

		public void Start()
		{
			if (_plans == null || _plans.Count == 0)
				throw new ArgumentNullException("Нет ни одного тест-плана");

			foreach (var plan in _plans)
			{
				Trace("filling plan " + plan.Name);

				var count = 0;
				var _tasks = new List<Task>();

				for (var i = 0; i < ThreadCount; i++)
				{
					_tasks.Add(new Task(() =>
					{
						while (count < plan.IterationsCount)
						{
							if (count >= plan.IterationsCount)
								break;
							var item = Interlocked.Increment(ref count);
							Trace("starting task {0}_{1}", plan.Name, item);
							var result = PerfHelper.CallTimed(plan.Action, item, plan.Name + item);
							Trace("finished task {0}_{1} in {2:s\\.fff}", plan.Name, item, result.Elapsed);
							plan.Results.Add(result);
						}
					}));
				}

				Trace("starting plan " + plan.Name);
				plan.Elapsed = PerfHelper.CallTimed(() =>
					{
						foreach (var t in _tasks)
							t.Start();
						Task.WaitAll(_tasks.ToArray());
					}).Elapsed;

				Trace("finished plan {0} in {1:s\\.fff}", plan.Name, plan.Elapsed);
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		public void Reset()
		{
			_plans.Clear();
		}

		public void Report(ReportOptions opts)
		{
			if (Output == null)
				throw new ArgumentNullException("Output");

			Output.WriteLine();
			Output.WriteLine("\t\t\tCalls\tFailed\tAvg\tMedian\tMin\tMax" + ((SlaTarget > 0) ? "\tSLA %" : ""));
			_plans.RemoveAll(p => p.IsWarmUp);
			var tcntTotal = _plans.Sum(p => p.Results.Count);
			var tcntFailed = _plans.Sum(p => p.Results.Count(r => r.Failed));
			var tsucceed = _plans.SelectMany(p => p.Results.Where(r => !r.Failed)).ToList();
			var totalOk = (double) _plans.Sum(p => p.Results.Count(r => r.Elapsed.TotalMilliseconds < SlaTarget));

			Output.WriteLine("{0}\t{1}\t{2}\t{3:s\\.fff}\t{4:s\\.fff}\t{5:s\\.fff}\t{6:s\\.fff}" + ((SlaTarget > 0) ? "\t{7:00.000}" : ""),
				"Total".PadRight(22),
				tcntTotal,
				tcntFailed,
				tcntTotal > tcntFailed ? new TimeSpan(0, 0, 0, 0, (int)tsucceed.Average(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
				tcntTotal > tcntFailed ? new TimeSpan(0, 0, 0, 0, (int)tsucceed.Median(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
				tcntTotal > tcntFailed ? new TimeSpan(0, 0, 0, 0, (int)tsucceed.Min(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
				tcntTotal > tcntFailed ? new TimeSpan(0, 0, 0, 0, (int)tsucceed.Max(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
				(SlaTarget > 0 && tcntTotal > tcntFailed) ? ( totalOk * 100D) / Convert.ToDouble(tcntTotal) : 0
				);

			Output.WriteLine("-------------------------------------------------------------------------------");

			foreach (var plan in _plans)
			{
				var cntTotal = plan.Results.Count();
				var cntFailed = plan.Results.Count(t => t.Failed);
				var succeed = plan.Results.Where(t => !t.Failed).ToList();
				var planOk = (double) plan.Results.Count(r => r.Elapsed.TotalMilliseconds < SlaTarget);
				Output.WriteLine("{0}\t{1}\t{2}\t{3:s\\.fff}\t{4:s\\.fff}\t{5:s\\.fff}\t{6:s\\.fff}" + ((SlaTarget > 0) ? "\t{7:00.000}" : ""),
					plan.Name.PadRight(22),
					cntTotal,
					cntFailed,
					cntTotal > cntFailed ? new TimeSpan(0, 0, 0, 0, (int)succeed.Average(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
					cntTotal > cntFailed ? new TimeSpan(0, 0, 0, 0, (int)succeed.Median(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
					cntTotal > cntFailed ? new TimeSpan(0, 0, 0, 0, (int)succeed.Min(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
					cntTotal > cntFailed ? new TimeSpan(0, 0, 0, 0, (int)succeed.Max(t => t.Elapsed.TotalMilliseconds)) : new TimeSpan(),
					(SlaTarget > 0 && cntTotal > cntFailed) ? ( planOk * 100D) / Convert.ToDouble(cntTotal) : 0
					);
			}

			if (opts.HasFlag(ReportOptions.Totals))
			{
				Output.WriteLine();
				Output.WriteLine("Plan totals:");
				Output.WriteLine("-------------------------------------------------------------------------------");
				foreach (var plan in _plans)
				{
					Output.WriteLine("{3}:\ttotal of {0} tasks run in {1:hh\\:mm\\:ss\\.ffff} ({2:N} req/s)", plan.Results.Count, plan.Elapsed, plan.Results.Count / plan.Elapsed.TotalSeconds, plan.Name.PadRight(22));
				}
			}

			if (opts.HasFlag(ReportOptions.Slowest))
			{
				Output.WriteLine();
				Output.WriteLine("-------------------------------------------------------------------------------");
				Output.WriteLine("Slowest tasks:");
				foreach (var plan in _plans)
				{
					Output.WriteLine("\t" + plan.Name + ":");
					foreach (var result in plan.Results.OrderByDescending(r => r.Elapsed.TotalMilliseconds).Take(5))
					{
						Output.WriteLine("\ttask id: {0}, elapsed: {1:mm\\:ss\\.ffff}", result.Name, result.Elapsed);
					}
				}
			}
		}
	}
}