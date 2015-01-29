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
	class Program
	{
		private static GraphClient client;

		const int count = 10;

		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Please specify the input file stem");
				return;
			}

			using (var writer = new StreamWriter(args[0] + ".neo-stats.txt") { AutoFlush = true })
			{
				var sw = new Stopwatch();

				try
				{
					client = new GraphClient(new Uri("http://localhost:7474/db/data"), new HttpClientWrapper(new HttpClient() { Timeout = TimeSpan.FromDays(4) }));
					client.Connect();
					IRawGraphClient raw = ((IRawGraphClient)client);

					{
						Console.WriteLine("Insert pages");
						sw.Restart();
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
						sw.Restart();
						raw.ExecuteCypher(new CypherQuery(@"CREATE INDEX ON :Page(ctitle)", new Dictionary<string, object>(), CypherResultMode.Set));
						sw.Stop();
						writer.WriteLine("Index on ctitle took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Index on id");
						sw.Restart();
						raw.ExecuteCypher(new CypherQuery(@"CREATE INDEX ON :Page(id)", new Dictionary<string, object>(), CypherResultMode.Set));
						sw.Stop();
						writer.WriteLine("Index on id took: " + sw.Elapsed);
					}
					{
						Console.WriteLine("Insert links");
						sw.Restart();
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
					{
						Console.WriteLine("Insert first links");
						sw.Restart();
						raw.ExecuteCypher(
							new CypherQuery(
								@"
USING PERIODIC COMMIT 100
LOAD CSV FROM 'file:///" + args[0] + @".firstlinks.utf8.csv' AS line
FIELDTERMINATOR '\t'
MATCH (p1:Page {id : toInt(line[0])}), (p2:Page {ctitle : line[1]})
CREATE (p1)-[:first_links_to]->(p2)
",
								new Dictionary<string, object>(),
								CypherResultMode.Set)
							);
						sw.Stop();
						writer.WriteLine("Insert links took: " + sw.Elapsed);
					}
					{
						for (int hop = 5; hop <= 5; hop++)
						{
							for (int i = 0; i < count; i++)
							{
								Console.WriteLine("Jesus " + hop + " hop");
								sw.Restart();
								raw.ExecuteCypher(
									new CypherQuery(
										@"
MATCH (p:Page {title:'Jesus'})
MATCH (p)<-[:links_to*1.." + hop + @"]-(a:Page)RETURN COUNT(DISTINCT(a));
",
										new Dictionary<string, object>(),
										CypherResultMode.Set)
									);
								sw.Stop();
								writer.WriteLine("Jesus " + hop + " hop took: " + sw.Elapsed);
							}
						}
					}
					{
						for (int hop = 5; hop <= 5; hop++)
						{
							for (int i = 0; i < count; i++)
							{
								Console.WriteLine("Jesus " + hop + " hop with data");
								sw.Restart();
								foreach (var r in raw.ExecuteGetCypherResults<string>(
									new CypherQuery(
										@"
MATCH (p:Page {title:'Jesus'})
MATCH (p)<-[:links_to*1.." + hop + @"]-(a:Page)RETURN DISTINCT(a);
",
										new Dictionary<string, object>(),
										CypherResultMode.Set)
									))
									//Console.WriteLine(r);
									;
								sw.Stop();
								writer.WriteLine("Jesus " + hop + " hop with data took: " + sw.Elapsed);
							}
						}
					}
					{
						for (int i = 0; i < count; i++)
						{
							Console.WriteLine("Philosophy first links");
							sw.Restart();
							raw.ExecuteCypher(
								new CypherQuery(
									@"
MATCH (p:Page {title:'Philosophie'})
MATCH (p)<-[:first_links_to*]-(a:Page)RETURN COUNT(DISTINCT(a));
",
									new Dictionary<string, object>(),
									CypherResultMode.Set)
								);
							sw.Stop();
							writer.WriteLine("Philosophy first links took: " + sw.Elapsed);
						}
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
