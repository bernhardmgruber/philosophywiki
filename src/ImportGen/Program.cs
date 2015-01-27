using Common;
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
		const string csvSeparator = "\t";

		static string q(string str)
		{
			return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");
		}

		static void Main(string[] args)
		{
			Console.WriteLine("CSV generator");

			if (args.Length < 1)
			{
				Console.WriteLine("Please specify the input files stem");
				return;
			}

			string stem = args[0];

			string titlesFile = args[0] + ".titles.txt";
			string linksFile = args[0] + ".links.txt";
			string metaFile = args[0] + ".meta.txt";

			string[] metaLines = File.ReadAllLines(metaFile);

			var meta = new Dictionary<string, string>();
			foreach (var metaLine in metaLines)
			{
				var parts = metaLine.Split(new string[] { ": " }, StringSplitOptions.None);
				meta.Add(parts[0], parts[1]);
			}

			using (var csvWriter16 = new StreamWriter(stem + ".titles.utf16.csv", false, Encoding.Unicode))
			using (var csvWriter8 = new StreamWriter(stem + ".titles.utf8.csv", false, new UTF8Encoding(false)))
			{
				Console.WriteLine("Writing titles");
				long count = 0;
				using (var stream = new FileStream(titlesFile, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream, Encoding.Unicode))
				{
					while (!reader.EndOfStream)
					{
						string line = reader.ReadLine();
						int id = Convert.ToInt32(line);
						line = reader.ReadLine();
						string readableTitle = line;
						line = reader.ReadLine();
						string canonicalTitle = line;
						line = reader.ReadLine();
						int pageLength = Convert.ToInt32(line);
						line = reader.ReadLine();
						string pageText = line;

						if (pageText.Length > 100)
							Console.WriteLine("Invalid page text");

						string output = id + csvSeparator + readableTitle + csvSeparator + canonicalTitle + csvSeparator + pageLength + csvSeparator + pageText;
						csvWriter16.WriteLine(output);
						output = "\"" + id + "\"" + csvSeparator + "\"" + q(readableTitle) + "\"" + csvSeparator + "\"" + q(canonicalTitle) + "\"" + csvSeparator + "\"" + pageLength + "\"" + csvSeparator + "\"" + q(pageText) + "\""; // escape quotes for Neo4j and surround by quotes
						csvWriter8.WriteLine(output);
						Utils.UpdateProgress(stream);
						count++;
					}
				}

				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " titles (" + meta["TotalTitles"] + " in meta)");
			}

			using (var csvWriter16 = new StreamWriter(stem + ".links.utf16.csv", false, Encoding.Unicode))
			using (var csvWriter8 = new StreamWriter(stem + ".links.utf8.csv", false, new UTF8Encoding(false)))
			{
				Console.WriteLine("Writing links");
				long count = 0;
				long separatorCount = 0;
				Utils.lastPercentage = -1;
				using (var stream = new FileStream(linksFile, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream, Encoding.Unicode))
				{
					while (!reader.EndOfStream)
					{
						string line = reader.ReadLine();
						int id = Convert.ToInt32(line);
						string ctitle = reader.ReadLine();
						string links = reader.ReadLine();
						if (links.Length > 0)
						{
							foreach (var link in links.SplitLazy('|'))
							{
								string output = id + csvSeparator + link;
								csvWriter16.WriteLine(output);
								output = "\"" + id + "\"" + csvSeparator + "\"" + q(link) + "\""; // escape quotes for Neo4j and surround by quotes
								csvWriter8.WriteLine(output);
								count++;
							}
						}
						separatorCount += links.Count(c => c == '|');
						Utils.UpdateProgress(stream);
					}
				}
				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " links (" + meta["TotalLinks"] + " in meta, " + separatorCount + " separators)");
			}

			Console.WriteLine("Finished");
		}
	}
}
