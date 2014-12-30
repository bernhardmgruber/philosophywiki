using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportGen
{
	static class LazySplit
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
	}
}
