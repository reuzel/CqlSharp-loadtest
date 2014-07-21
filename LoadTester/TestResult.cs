using System;

namespace LoadTester
{
	public class TestResult
	{
		public string Name { get; set; }
		public TimeSpan Elapsed { get; set; }
		public bool Failed { get { return Error != null;} }
		public Exception Error { get; set; }
	}
}