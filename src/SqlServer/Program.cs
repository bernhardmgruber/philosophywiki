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

			using (var writer = new StreamWriter(args[0] + ".sql-stats.txt") { AutoFlush = true })
			{
				var sw = new Stopwatch();

				Console.WriteLine("Opening database");
				sw.Start();
				using (var database = new SqlDatabase())
				{
					sw.Stop();
					writer.WriteLine("Database opening took: " + sw.Elapsed);

					{
						Console.WriteLine("\nCreating schema");
						sw.Restart();
						database.Run(@"
DROP TABLE Page;
DROP TABLE SLink;
DROP TABLE SFirstLink;
CREATE TABLE Page (id INTEGER NOT NULL, title NVARCHAR(300) NOT NULL, ctitle NVARCHAR(300) NOT NULL, length INTEGER NOT NULL, text NVARCHAR(110) NOT NULL);
CREATE TABLE SLink(src INTEGER NOT NULL, dst NVARCHAR(300) NOT NULL);
CREATE TABLE SFirstLink(src INTEGER NOT NULL, dst NVARCHAR(300) NOT NULL);
");
						sw.Stop();
						writer.WriteLine("Creating schema took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nInsert pages");
						sw.Restart();
						database.Run(@"
BULK INSERT Page
FROM '" + args[0] + @".titles.utf16.csv'
WITH ( FIELDTERMINATOR = '\t', ROWTERMINATOR = '\n', DATAFILETYPE = 'widechar', BATCHSIZE = 10000 )
");
						sw.Stop();
						writer.WriteLine("Insert pages took: " + sw.Elapsed);
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
						Console.WriteLine("Index on id");
						sw.Restart();
						database.Run(@"
CREATE INDEX Index_id ON Page(id)
");
						sw.Stop();
						writer.WriteLine("Index on id took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nInsert links");
						sw.Restart();
						database.Run(@"
BULK INSERT SLink
FROM '" + args[0] + @".links.utf16.csv'
WITH ( FIELDTERMINATOR = '\t', ROWTERMINATOR = '\n', DATAFILETYPE = 'widechar', BATCHSIZE = 10000 )
");
						sw.Stop();
						writer.WriteLine("Insert links took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nResolving links");
						sw.Restart();
						database.Run(@"
DROP TABLE Link;
SELECT *
INTO Link
FROM (
	SELECT
		src,
		(
			SELECT id
			FROM Page
			WHERE ctitle = dst COLLATE Latin1_General_BIN
		) AS dst
	FROM SLink
) links
WHERE dst IS NOT NULL;
");
						sw.Stop();
						writer.WriteLine("Resolving links took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nInsert first links");
						sw.Restart();
						database.Run(@"
BULK INSERT SFirstLink
FROM '" + args[0] + @".firstlinks.utf16.csv'
WITH ( FIELDTERMINATOR = '\t', ROWTERMINATOR = '\n', DATAFILETYPE = 'widechar', BATCHSIZE = 10000 )
");
						sw.Stop();
						writer.WriteLine("Insert first links took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("\nResolving first links");
						sw.Restart();
						database.Run(@"
DROP TABLE FirstLink;
SELECT *
INTO FirstLink
FROM (
	SELECT
		src,
		(
			SELECT id
			FROM Page
			WHERE ctitle = dst COLLATE Latin1_General_BIN
		) AS dst
	FROM SFirstLink
) links
WHERE dst IS NOT NULL;
");
						sw.Stop();
						writer.WriteLine("Resolving first links took: " + sw.Elapsed);
					}
				}
			}

			Console.WriteLine("Finished");
		}
	}
}
