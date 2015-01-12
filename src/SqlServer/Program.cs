using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Please specify the input file stem");
				return;
			}

			var sw = new Stopwatch();
			using (var writer = new StreamWriter(args[0] + ".sql-stats.txt") { AutoFlush = true })
			{
				Console.WriteLine("Opening database");
				sw.Start();
				var database = new SqlDatabase();
				sw.Stop();
				writer.WriteLine("Database opening took: " + sw.Elapsed);

				Console.WriteLine("\nSchema script");
				sw.Restart();
				database.Load(args[0] + ".schema.sql");
				sw.Stop();
				writer.WriteLine("Schema script run took: " + sw.Elapsed);

				Console.WriteLine("\nTitles script");
				sw.Restart();
				database.Load(args[0] + ".titles.sql", true);
				sw.Stop();
				writer.WriteLine("Titles script run took: " + sw.Elapsed);

				Console.WriteLine("\nLinks script");
				sw.Restart();
				database.Load(args[0] + ".links.sql", true);
				sw.Stop();
				writer.WriteLine("Links script run took: " + sw.Elapsed);
			}

			Console.Read();
		}
	}
}
