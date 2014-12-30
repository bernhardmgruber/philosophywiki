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
				Console.WriteLine("Please specify the input files stem");

			string titlesFile = args[0] + ".titles.txt";
			string linksFile = args[0] + ".links.txt";
			string metaFile = args[0] + ".meta.txt";
			string schemaSqlFile = args[0] + ".schema.sql";
			string titlesSqlFile = args[0] + ".titles.sql";
			string linksSqlFile = args[0] + ".links.sql";

			string[] metaLines = File.ReadAllLines(metaFile);

			var meta = new Dictionary<string, string>();
			foreach (var metaLine in metaLines)
			{
				var parts = metaLine.Split(new string[] { ": " }, StringSplitOptions.None);
				meta.Add(parts[0], parts[1]);
			}

			using (var writer = new StreamWriter(schemaSqlFile))
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

			using (var writer = new StreamWriter(titlesSqlFile))
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
						WriteTitle(readableTitle, canonicalTitle, writer);
						UpdateProgress(stream);
						count++;
					}
				}

				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " titles (" + meta["TotalTitles"] + " in meta)");
			}

			using (var writer = new StreamWriter(linksSqlFile))
			{
				Console.WriteLine("Writing links");
				long count = 0;
				lastPercentage = -1;
				using (var stream = new FileStream(linksFile, FileMode.Open, FileAccess.Read))
				using (var reader = new StreamReader(stream))
				{
					while (!reader.EndOfStream)
					{
						string readableTitle = reader.ReadLine();
						string links = reader.ReadLine();
						WriteLinks(readableTitle, links, writer, ref count);
						UpdateProgress(stream);
					}
				}
				Console.WriteLine();
				Console.WriteLine("Wrote " + count + " links (" + meta["TotalLinks"] + " in meta)");
			}

			Console.WriteLine("Finished");
		}

		private static void WriteTitle(string readableTitle, string canonicalTitle, TextWriter writer)
		{
			writer.WriteLine("insert into Page values (null, \"" + readableTitle + "\", \"" + canonicalTitle + "\")");
		}

		private static void WriteLinks(string readableTitle, string links, StreamWriter writer, ref long count)
		{
			Func<string, bool, string> selectId = (t, useCTitle) => "(select id from Page where " + (useCTitle ? "c" : "") + "title = \"" + t + "\")";

			foreach (var l in links.Split(',').Where(l => l.Length != 0))
			{
				writer.WriteLine("insert into Link values (" + selectId(readableTitle, false) + ", " + selectId(l, true) + ")");
				count++;
			}
		}
	}
}
