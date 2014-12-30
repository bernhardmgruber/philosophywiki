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
		private static int lastPercentage = -1;

		private static void UpdateProgress(FileStream stream)
		{
			int percentage = (int)(stream.Position * 100 / stream.Length);
			if (percentage != lastPercentage)
			{
				Console.Write("\b\b\b\b" + percentage + "%");
				lastPercentage = percentage;
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("Database script generator (run on output of DumpPreprocessor)");

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

			using (var writer = new StreamWriter(stem + ".schema.sql"))
			{
				// write schema
				writer.Write(@"
create table Page
(
	id integer auto_increment not null primary key,
	title varchar(240) not null,
	ctitle varchar(240) not null,
	constraint pkPage primary key (id)
)

create table Link
(
	src integer not null,
	dst integer not null,
	constraint pkLink primary key (src, dst)
)
");
			}

			using (var sqlWriter = new StreamWriter(stem + ".titles.sql"))
			using (var cypherWriter = new StreamWriter(stem + ".titles.cypher"))
			{
				Console.WriteLine("Writing titles");
				long count = 0;
				using (var stream = new FileStream(titlesFile, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream))
				{
					while (!reader.EndOfStream)
					{
						string readableTitle = reader.ReadLine();
						string canonicalTitle = reader.ReadLine();

						// escape quotes
						readableTitle = readableTitle.Replace("'", "''");
						canonicalTitle = canonicalTitle.Replace("'", "''");

						sqlWriter.WriteLine("INSERT INTO Page VALUES (null, '" + readableTitle + "', '" + canonicalTitle + "')");
						cypherWriter.WriteLine("CREATE (Page { title : '" + readableTitle + "', ctitle : '" + canonicalTitle + "' })");
						UpdateProgress(stream);
						count++;
					}
				}

				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " titles (" + meta["TotalTitles"] + " in meta)");
			}

			using (var sqlWriter = new StreamWriter(stem + ".links.sql"))
			using (var cypherWriter = new StreamWriter(stem + ".links.cypher"))
			{
				Console.WriteLine("Writing links");
				long count = 0;
				long separatorCount = 0;
				lastPercentage = -1;
				using (var stream = new FileStream(linksFile, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream))
				{
					while (!reader.EndOfStream)
					{
						string readableTitle = reader.ReadLine();
						readableTitle = readableTitle.Replace("'", "''"); // escape quotes
						string links = reader.ReadLine();
						if (links.Length > 0)
						{
							foreach (var link in links.SplitLazy('|'))
							{
								// escape quotes
								var l = link.Replace("'", "''");

								sqlWriter.Write("INSERT INTO Link VALUES (");
								sqlWriter.Write("(SELECT id FROM Page WHERE title = '");
								sqlWriter.Write(readableTitle);
								sqlWriter.Write("'), ");
								sqlWriter.Write("(SELECT id FROM Page WHERE ctitle = '");
								sqlWriter.Write(l);
								sqlWriter.Write("'))");
								sqlWriter.WriteLine();

								cypherWriter.Write("MATCH (s:Page),(d:Page) WHERE a.title = '");
								cypherWriter.Write(readableTitle);
								cypherWriter.Write("' AND b.ctitle = '");
								cypherWriter.Write(l);
								cypherWriter.Write("' CREATE (a)-[links_to]->(b)");
								cypherWriter.WriteLine();

								count++;
							}
						}
						separatorCount += links.Count(c => c == '|');
						UpdateProgress(stream);
					}
				}
				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " links (" + meta["TotalLinks"] + " in meta, " + separatorCount + " separators)");
			}

			Console.WriteLine("Finished");
		}
	}
}
