using System;
using System.Collections.Concurrent;

namespace LoadTester
{
	public class TestPlan
	{
		public string Name { get; set; }

		public int IterationsCount { get; set; }

		public Action<int> Action { get; set; }

		public TimeSpan Elapsed { get; set; }

		public ConcurrentBag<TestResult> Results { get; set; }

		public bool IsWarmUp { get; set; }

		public TestPlan(string name, int iterationsCount, Action<int> action)
		{
			Name = name;
			IterationsCount = iterationsCount;
			Action = action;
			Results = new ConcurrentBag<TestResult>();
		}
	}
}
