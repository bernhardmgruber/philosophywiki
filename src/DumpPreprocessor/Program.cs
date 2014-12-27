using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace DumpPreprocessor
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Wikipedia dump processor (enwiki-<date>-pages-articles.xml)");

			if (args.Length < 2)
				Console.WriteLine("Please specify two file name as command line argument. Input and output file.");

			string dumpFile = args[0];
			string outFile = args[1];

			//	<siteinfo>
			//	<sitename>Wikipedia</sitename>
			//	<dbname>enwiki</dbname>
			//	<base>http://en.wikipedia.org/wiki/Main_Page</base>
			//	<generator>MediaWiki 1.25wmf10</generator>
			//	<case>first-letter</case>
			//	<namespaces>
			//	  <namespace key="-2" case="first-letter">Media</namespace>
			//	  <namespace key="-1" case="first-letter">Special</namespace>
			//	  <namespace key="0" case="first-letter" />
			//	  <namespace key="1" case="first-letter">Talk</namespace>
			//	  <namespace key="2" case="first-letter">User</namespace>
			//	  <namespace key="3" case="first-letter">User talk</namespace>
			//	  <namespace key="4" case="first-letter">Wikipedia</namespace>
			//	  <namespace key="5" case="first-letter">Wikipedia talk</namespace>
			//	  <namespace key="6" case="first-letter">File</namespace>
			//	  <namespace key="7" case="first-letter">File talk</namespace>
			//	  <namespace key="8" case="first-letter">MediaWiki</namespace>
			//	  <namespace key="9" case="first-letter">MediaWiki talk</namespace>
			//	  <namespace key="10" case="first-letter">Template</namespace>
			//	  <namespace key="11" case="first-letter">Template talk</namespace>
			//	  <namespace key="12" case="first-letter">Help</namespace>
			//	  <namespace key="13" case="first-letter">Help talk</namespace>
			//	  <namespace key="14" case="first-letter">Category</namespace>
			//	  <namespace key="15" case="first-letter">Category talk</namespace>
			//	  <namespace key="100" case="first-letter">Portal</namespace>
			//	  <namespace key="101" case="first-letter">Portal talk</namespace>
			//	  <namespace key="108" case="first-letter">Book</namespace>
			//	  <namespace key="109" case="first-letter">Book talk</namespace>
			//	  <namespace key="118" case="first-letter">Draft</namespace>
			//	  <namespace key="119" case="first-letter">Draft talk</namespace>
			//	  <namespace key="446" case="first-letter">Education Program</namespace>
			//	  <namespace key="447" case="first-letter">Education Program talk</namespace>
			//	  <namespace key="710" case="first-letter">TimedText</namespace>
			//	  <namespace key="711" case="first-letter">TimedText talk</namespace>
			//	  <namespace key="828" case="first-letter">Module</namespace>
			//	  <namespace key="829" case="first-letter">Module talk</namespace>
			//	  <namespace key="2600" case="first-letter">Topic</namespace>
			//	</namespaces>
			//  </siteinfo>
			//  <page>
			//	<title>AccessibleComputing</title>
			//	<ns>0</ns>
			//	<id>10</id>
			//	<redirect title="Computer accessibility" />
			//	<revision>
			//	  <id>631144794</id>
			//	  <parentid>381202555</parentid>
			//	  <timestamp>2014-10-26T04:50:23Z</timestamp>
			//	  <contributor>
			//		<username>Paine Ellsworth</username>
			//		<id>9092818</id>
			//	  </contributor>
			//	  <comment>add [[WP:RCAT|rcat]]s</comment>
			//	  <model>wikitext</model>
			//	  <format>text/x-wiki</format>
			//	  <text xml:space="preserve">#REDIRECT [[Computer accessibility]] {{Redr|move|from CamelCase|up}}</text>
			//	  <sha1>4ro7vvppa5kmm0o1egfjztzcwd0vabw</sha1>
			//	</revision>
			//	</page>

			using (var stream = new FileStream(dumpFile, FileMode.Open, FileAccess.Read))
			using (var reader = XmlReader.Create(stream))
			using (var writer = new StreamWriter(outFile))
			{
				reader.MoveToContent();

				RawMeta meta = null;
				RawPage page = null;
				bool inRevision = false;

				while (reader.Read())
				{
					UpdateProgress(stream);

					if (reader.NodeType == XmlNodeType.Element)
					{
						if (reader.Name == "page")
							page = new RawPage();
						else if (page != null && reader.Name == "title")
							page.Title = reader.ReadElementContentAsString();
						else if (page != null && reader.Name == "ns")
							page.NamespaceId = reader.ReadElementContentAsInt();
						else if (page != null && !inRevision && reader.Name == "id")
							page.Id = reader.ReadElementContentAsInt();
						else if (page != null && reader.Name == "text")
							page.Text = reader.ReadElementContentAsString();
						else if (page != null && reader.Name == "revision")
							inRevision = true;
						else if (reader.Name == "siteinfo")
							meta = new RawMeta();
						else if (meta != null && reader.Name == "dbname")
							meta.DatabaseName = reader.ReadElementContentAsString();
						else if (meta != null && reader.Name == "generator")
							meta.Generator = reader.ReadElementContentAsString();
						else if (meta != null && reader.Name == "namespaces")
							meta.Namespaces = new Dictionary<int, string>();
						else if (meta != null && meta.Namespaces != null && reader.Name == "namespace")
						{
							int ns = int.Parse(reader.GetAttribute("key"));
							string name = reader.ReadElementContentAsString();
							meta.Namespaces.Add(ns, name);
						}
					}
					else if (reader.NodeType == XmlNodeType.EndElement)
					{
						if (reader.Name == "page")
						{
							WritePage(page, writer);
							page = null;
						}
						else if (page != null && reader.Name == "revision")
							inRevision = false;
						else if (reader.Name == "siteinfo")
						{
							WriteMeta(meta, writer);
							meta = null;
						}

					}
				}
			}

			Console.WriteLine("Finished");
		}

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

		private static void WriteMeta(RawMeta meta, TextWriter writer)
		{
			writer.WriteLine("// File created: " + DateTime.Now);
			writer.WriteLine("// Database: " + meta.DatabaseName);
			writer.WriteLine("// Generator: " + meta.Generator);
			writer.WriteLine();
		}

		private static Regex linkRegex = new Regex("\\[\\[(.*?)(\\|.*?)?\\]\\]", RegexOptions.Compiled);

		private static void WritePage(RawPage page, TextWriter writer)
		{
			page.Text = page.Text.Replace('\n', ' ');
			
			// find links
			var matches = linkRegex.Matches(page.Text);
			var links = matches.Cast<Match>().Select(m => m.Groups[1].Value);

			writer.WriteLine(page.Id);
			writer.WriteLine(page.Title);
			writer.WriteLine(page.Text);
			foreach (var link in links)
				writer.Write(link + ",");
			writer.WriteLine();
			writer.WriteLine();
		}
	}
}
