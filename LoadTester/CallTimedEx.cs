using System;
using System.Diagnostics;

namespace LoadTester
{
	public static class PerfHelper
	{
		public static TestResult CallTimed<T>(Action<T> method, T argument, string name = null)
		{
			var timer = new Stopwatch();
			var result = new TestResult() { Name = name ?? method.Method.Name };
			timer.Start();

			try
			{
				method.Invoke(argument);
				timer.Stop();
			}
			catch (AggregateException e)
			{
				timer.Stop();
				result.Error = e;
			}
			catch (Exception e)
			{
				timer.Stop();
				result.Error = e;
			}
			finally
			{
				result.Elapsed = timer.Elapsed;
			}

			timer.Reset();
			timer = null;
			return result;
		}

		public static TestResult CallTimed(Action method, string name = null)
		{
			var timer = new Stopwatch();
			var result = new TestResult() { Name = name ?? method.Method.Name };
			timer.Start();

			try
			{
				method.Invoke();
				timer.Stop();
			}
			catch (AggregateException e)
			{
				timer.Stop();
				result.Error = e;
			}
			catch (Exception e)
			{
				timer.Stop();
				result.Error = e;
			}
			finally
			{
				result.Elapsed = timer.Elapsed;
			}

			timer.Reset();
			timer = null;
			return result;
		}
	}
}
