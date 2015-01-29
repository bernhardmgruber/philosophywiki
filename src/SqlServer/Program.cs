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
		const int count = 10;

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
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 1 hop");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
SELECT COUNT(DISTINCT(l1.src))
FROM Page p
	JOIN Link l1 ON p.id = l1.dst
WHERE p.title='Jesus'
"));
							sw.Stop();
							writer.WriteLine("Jesus 1 hop took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 2 hop");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
SELECT COUNT(DISTINCT(l.src))
FROM (
		SELECT l1.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l2.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
		WHERE p.title='Jesus'
) l;
"));
							sw.Stop();
							writer.WriteLine("Jesus 2 hop took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 3 hop");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
SELECT COUNT(DISTINCT(l.src))
FROM (
		SELECT l1.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l2.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l3.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
		WHERE p.title='Jesus'
) l;
"));
							sw.Stop();
							writer.WriteLine("Jesus 3 hop took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 4 hop");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
SELECT COUNT(DISTINCT(l.src))
FROM (
		SELECT l1.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l2.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l3.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l4.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
			JOIN Link l4 ON l4.dst = l3.src
		WHERE p.title='Jesus'
) l;
"));
							sw.Stop();
							writer.WriteLine("Jesus 4 hop took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 5 hop");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
SELECT COUNT(DISTINCT(l.src))
FROM (
		SELECT l1.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l2.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l3.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l4.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
			JOIN Link l4 ON l4.dst = l3.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l5.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
			JOIN Link l4 ON l4.dst = l3.src
			JOIN Link l5 ON l5.dst = l4.src
		WHERE p.title='Jesus'
) l;
"));
							sw.Stop();
							writer.WriteLine("Jesus 5 hop took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 1 hop with pages");
							sw.Restart();
							database.RunReader(@"
SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l1.src)
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	) l ON p.id = l.src;
					");
							sw.Stop();
							writer.WriteLine("Jesus 1 hop with pages took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 2 hop with pages");
							sw.Restart();
							database.RunReader(@"
SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l.src)
		FROM (
				SELECT l1.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l2.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
				WHERE p.title='Jesus'
		) l
	) l ON p.id = l.src;
					");
							sw.Stop();
							writer.WriteLine("Jesus 2 hop with pages took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 3 hop with pages");
							sw.Restart();
							database.RunReader(@"
SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l.src)
		FROM (
				SELECT l1.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l2.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l3.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
				WHERE p.title='Jesus'
		) l
	) l ON p.id = l.src;
					");
							sw.Stop();
							writer.WriteLine("Jesus 3 hop with pages took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 4 hop with pages");
							sw.Restart();
							database.RunReader(@"
SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l.src)
		FROM (
				SELECT l1.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l2.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l3.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l4.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
					JOIN Link l4 ON l4.dst = l3.src
				WHERE p.title='Jesus'
		) l
	) l ON p.id = l.src;
					");
							sw.Stop();
							writer.WriteLine("Jesus 4 hop with pages took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Jesus 5 hop with pages");
							sw.Restart();
							database.RunReader(@"
SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l.src)
		FROM (
				SELECT l1.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l2.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l3.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l4.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
					JOIN Link l4 ON l4.dst = l3.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l5.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
					JOIN Link l4 ON l4.dst = l3.src
					JOIN Link l5 ON l5.dst = l4.src
				WHERE p.title='Jesus'
		) l
	) l ON p.id = l.src;
					");
							sw.Stop();
							writer.WriteLine("Jesus 5 hop with pages took: " + sw.Elapsed);
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Philosophy first links");
							sw.Restart();
							Console.WriteLine(database.RunScalar(@"
WITH temp (id) AS (
		SELECT p.id
		FROM Page p
		WHERE p.title='Philosophie'
	UNION ALL
		SELECT fl.src
		FROM FirstLink fl
			JOIN temp t ON fl.dst=t.id
)
SELECT COUNT(DISTINCT(t.id)) - 1
FROM temp t;
"));
							sw.Stop();
							writer.WriteLine("Philosophy first links took: " + sw.Elapsed);
						}
					}
				}
			}

			Console.WriteLine("Finished");
		}
	}
}
