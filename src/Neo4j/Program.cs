using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Neo4jClient.Cypher;
using System.Diagnostics;

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
			const string csvFile = "enwiki-20141208-pages-articles.titles.csv";

			var sw = new Stopwatch();

			try {
				client = new GraphClient(new Uri("http://localhost:7474/db/data"));
				client.Connect();

				sw.Start();
				((IRawGraphClient)client).ExecuteCypher(
					new CypherQuery(
						@"
USING PERIODIC COMMIT
LOAD CSV FROM 'file:///D:/enwiki-20141208-pages-articles.titles.csv' AS line
FIELDTERMINATOR '\t'
CREATE (Page { id : line[0], title : line[1], ctitle : line[2], length : line[3], text : line[4] })
",
						new Dictionary<string, object>(),
						CypherResultMode.Set)
					);
				sw.Stop();
				Console.WriteLine("Running title script took: " + sw.Elapsed);

			//	client.Cypher
			//		.Match("(article:Article)")
			//		.Delete("article")
			//		.ExecuteWithoutResults();

			//	var a = CreateArticle("Article A");
			//	var b = CreateArticle("Article B");
			//	var c = CreateArticle("Article C");
			//	var d = CreateArticle("Article D");

			//	LinkTo(d, c);
			//	LinkTo(c, a);
			//	LinkTo(b, a);

			//	Console.WriteLine("Success");
			}
			catch (NeoException exc)
			{
				Console.WriteLine("Error: {0}", exc.Message);
			}
            Console.ReadKey();
        }

        private static Article CreateArticle(string name)
        {
            var newArticle = new Article { Name = name };
            
            return client.Cypher
                .Create("(article:Article {newArticle})")
                .WithParam("newArticle", newArticle)
                .Return<Article>("article")
                .Results
                .Single();
        }

        private static void LinkTo(Article source, Article target)
        {
            client.Cypher
                .Match("(s:Article)", "(t:Article)")
                .Where((Article s) => s.Name == source.Name)
                .AndWhere((Article t) => t.Name == target.Name)
                .CreateUnique("s-[:links_to]->t")
                .ExecuteWithoutResults();
        }
    }
}
