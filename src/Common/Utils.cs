using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
	public static class Utils
	{
		public static IEnumerable<string> SplitLazy(this string str, char c)
		{
			int start = 0;

			while (true)
			{
				int end = str.IndexOf(c, start);
				if (end != -1)
					yield return str.Substring(start, end - start);
				else
					break;
				start = end + 1;
			}
			yield return str.Substring(start);
		}

		public static int lastPercentage = -1;

		public static void UpdateProgress(Stream stream)
		{
			int percentage = (int)(stream.Position * 100 / stream.Length);
			if (percentage != lastPercentage)
			{
				Console.Write("\b\b\b\b" + percentage + "%");
				lastPercentage = percentage;
			}
		}
	}
}
