using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportGen
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Database script generator (run on output of DumpPreprocessor)");

			if (args.Length < 2)
				Console.WriteLine("Please specify two file name as command line argument. Input and output file.");

			string linkFile = args[0];
			string importFile = args[1];

			using (var reader = new StreamReader(linkFile))
			using (var writer = new StreamWriter(importFile))
			{
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();

					// skip comments and blank lines
					if (line.StartsWith("//") || string.IsNullOrEmpty(line))
						continue;

					if (reader.EndOfStream)
						break;

					// asume we are at the begin of a new page entry

					int id = int.Parse(line);
					line = reader.ReadLine();

					string title = line;
					line = reader.ReadLine();

					string[] links = line.Split(',');
					line = reader.ReadLine();

					WritePage(id, title, links, writer);
				}
			}
		}

		private static void WritePage(int id, string title, string[] links, TextWriter writer)
		{
			Func<string, string> formatPage = t => "(Page{title:\"" + title + "\"})";

			//writer.WriteLine("CREATE " + formatPage(title) + "-[links_to]->" + formatPage(;
		}
	}
}
