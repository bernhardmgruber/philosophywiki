using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Neo4jClient.Cypher;
using System.Diagnostics;
using System.Net.Http;

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

			var sw = new Stopwatch();

			try {
				client = new GraphClient(new Uri("http://localhost:7474/db/data"), new HttpClientWrapper(new HttpClient() { Timeout = TimeSpan.FromDays(4) }));
				client.Connect();

				sw.Start();
				((IRawGraphClient)client).ExecuteCypher(
					new CypherQuery(
						@"
USING PERIODIC COMMIT
LOAD CSV FROM 'file:///" + args[0] + @".titles.utf8.csv' AS line
FIELDTERMINATOR '\t'
CREATE (Page { id : line[0], title : line[1], ctitle : line[2], length : line[3], text : line[4] })
",
						new Dictionary<string, object>(),
						CypherResultMode.Set)
					);
				sw.Stop();
				Console.WriteLine("Running title script took: " + sw.Elapsed);

				sw.Start();
				((IRawGraphClient)client).ExecuteCypher(new CypherQuery(@"CREATE INDEX ON Page(ctitle)",new Dictionary<string, object>(),CypherResultMode.Set));
				sw.Stop();
				Console.WriteLine("Creating index took: " + sw.Elapsed);

				sw.Start();
				((IRawGraphClient)client).ExecuteCypher(
					new CypherQuery(
						@"
USING PERIODIC COMMIT
LOAD CSV FROM 'file:///" + args[0] + @".links.utf8.csv' AS line
FIELDTERMINATOR '\t'
MATCH (p1:Page {ctitle : line[0]}), (p2:Page {ctitle : line[1]})
CREATE (p1)-[links_to]->(p2)
",
						new Dictionary<string, object>(),
						CypherResultMode.Set)
					);
				sw.Stop();
				Console.WriteLine("Running title script took: " + sw.Elapsed);
			}
			catch (NeoException exc)
			{
				Console.WriteLine("Error: {0}", exc.Message);
			}
			Console.ReadKey();
		}
	}
}
