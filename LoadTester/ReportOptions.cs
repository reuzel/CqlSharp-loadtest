using System;

namespace LoadTester
{
	[Flags]
	public enum ReportOptions
	{
		None = 0,
		Totals = 1,
		Slowest = 2
	}
}