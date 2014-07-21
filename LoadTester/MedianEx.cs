using System;
using System.Collections.Generic;
using System.Linq;

namespace LoadTester
{
	public static class MedianEx
	{
		public static double Median(this IEnumerable<double> source)
		{
			int numberCount = source.Count();
			int halfIndex = source.Count() / 2;
			var sortedNumbers = source.OrderBy(n => n);
			double median;
			if ((numberCount % 2) == 0)
			{
				median = ((sortedNumbers.ElementAt(halfIndex) +
						sortedNumbers.ElementAt((halfIndex - 1))) / 2);
			}
			else
			{
				median = sortedNumbers.ElementAt(halfIndex);
			}
			return median;
		}

		public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		{
			return source.Select(selector).Median();
		}
	}
}