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


			//using (var s = new StreamReader(args[0] + ".links.utf16.csv"))
			//{
			//	string l;

			//	for (int count = 1; (l = s.ReadLine()) != null; count++)
			//	{
			//		if (count == 733915)
			//			Console.WriteLine(l);
			//	}
			//}

			var sw = new Stopwatch();
			using (var writer = new StreamWriter(args[0] + ".sql-stats.txt") { AutoFlush = true })
			{
				Console.WriteLine("Opening database");
				sw.Start();
				using (var database = new SqlDatabase())
				{
					sw.Stop();
					writer.WriteLine("Database opening took: " + sw.Elapsed);

					{
						Console.WriteLine("\nSchema script");
						sw.Restart();
						database.Run(@"
DROP TABLE Page;
DROP TABLE SLink;
DROP TABLE Link;
CREATE TABLE Page (id INTEGER NOT NULL, title NVARCHAR(300) NOT NULL, ctitle NVARCHAR(300) NOT NULL, length INTEGER NOT NULL, text NVARCHAR(110) NOT NULL);
CREATE TABLE SLink(src INTEGER NOT NULL, dst NVARCHAR(300) NOT NULL);
CREATE TABLE Link (src INTEGER NOT NULL, dst INTEGER NOT NULL)
");
						sw.Stop();
						writer.WriteLine("Schema script run took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nPage script");
						sw.Restart();
						database.Run(@"
BULK INSERT Page
FROM '" + args[0] + @".titles.utf16.csv'
WITH ( FIELDTERMINATOR = '\t', ROWTERMINATOR = '\n', DATAFILETYPE = 'widechar', BATCHSIZE = 10000 )
");
						sw.Stop();
						writer.WriteLine("Page script took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Index on ctitle");
						sw.Restart();
						database.Run(@"
CREATE INDEX Index_ctitle ON Page(ctitle)
");
						sw.Stop();
						writer.WriteLine("Index on ctitle took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nLink script");
						sw.Restart();
						database.Run(@"
BULK INSERT SLink
FROM '" + args[0] + @".links.utf16.csv'
WITH ( FIELDTERMINATOR = '\t', ROWTERMINATOR = '\n', DATAFILETYPE = 'widechar', BATCHSIZE = 10000 )
");
						sw.Stop();
						writer.WriteLine("Link script run took: " + sw.Elapsed);
					}
				}
			}

			Console.WriteLine("Finished");
		}
	}
}
