using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Neo4jClient.Cypher;
using System.Diagnostics;
using System.Net.Http;
using System.IO;

namespace Neo4j
{
	class Article
	{
		public string Name { get; set; }
	}

	class Program
	{
		private static GraphClient client;

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

				try
				{
					client = new GraphClient(new Uri("http://localhost:7474/db/data"), new HttpClientWrapper(new HttpClient() { Timeout = TimeSpan.FromDays(4) }));
					client.Connect();
					IRawGraphClient raw = ((IRawGraphClient)client);

					{
						Console.WriteLine("Insert pages");
						sw.Start();
						raw.ExecuteCypher(
							new CypherQuery(
								@"
USING PERIODIC COMMIT
LOAD CSV FROM 'file:///" + args[0] + @".titles.utf8.csv' AS line
FIELDTERMINATOR '\t'
CREATE (:Page { id : toInt(line[0]), title : line[1], ctitle : line[2], length : toInt(line[3]), text : line[4] })
",
								new Dictionary<string, object>(),
								CypherResultMode.Set)
							);
						sw.Stop();
						writer.WriteLine("Insert pages took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Index on ctitle");
						sw.Start();
						raw.ExecuteCypher(new CypherQuery(@"CREATE INDEX ON :Page(ctitle)", new Dictionary<string, object>(), CypherResultMode.Set));
						sw.Stop();
						writer.WriteLine("Index on ctitle took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Index on id");
						sw.Start();
						raw.ExecuteCypher(new CypherQuery(@"CREATE INDEX ON :Page(id)", new Dictionary<string, object>(), CypherResultMode.Set));
						sw.Stop();
						writer.WriteLine("Index on id took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Insert links");
						sw.Start();
						raw.ExecuteCypher(
							new CypherQuery(
								@"
USING PERIODIC COMMIT 100
LOAD CSV FROM 'file:///" + args[0] + @".links.utf8.csv' AS line
FIELDTERMINATOR '\t'
MATCH (p1:Page {id : toInt(line[0])}), (p2:Page {ctitle : line[1]})
CREATE (p1)-[:links_to]->(p2)
",
								new Dictionary<string, object>(),
								CypherResultMode.Set)
							);
						sw.Stop();
						writer.WriteLine("Insert links took: " + sw.Elapsed);
					}
				}
				catch (NeoException exc)
				{
					Console.WriteLine("Error: {0}", exc.Message);
				}
			}

			Console.WriteLine("Finished");
		}
	}
}
