using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace DumpPreprocessor
{
	class Program
	{
		private static int threadCount;
		private static Semaphore linkWrites;

		private static long totalTitles = 0;
		private static long totalLinks = 0;

		static void Main(string[] args)
		{
			threadCount = Environment.ProcessorCount * 2;
			linkWrites = new Semaphore(threadCount, threadCount);

			Console.WriteLine("Wikipedia dump preprocessor (enwiki-<date>-pages-articles.xml)");

			if (args.Length < 1)
			{
				Console.WriteLine("Please specify the input file stem.");
				return;
			}

			string dumpFile = args[0] + ".xml";
			string linkFile = args[0] + ".links.txt";
			string titleFile = args[0] + ".titles.txt";
			string metaFile = args[0] + ".meta.txt";

			using (var stream = new FileStream(dumpFile, FileMode.Open, FileAccess.Read))
			using (var reader = XmlReader.Create(stream))
			using (var linkWriter = new StreamWriter(linkFile))
			using (var titleWriter = new StreamWriter(titleFile))
			using (var metaWriter = new StreamWriter(metaFile))
			{
				Stopwatch sw = Stopwatch.StartNew();
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
							WritePageTitle(page, titleWriter);
							WritePageLinksAsync(page, linkWriter);
							page = null;
						}
						else if (page != null && reader.Name == "revision")
							inRevision = false;
						else if (reader.Name == "siteinfo")
						{
							WriteMeta(meta, metaWriter);
							meta = null;
						}

					}
				}

				// all write slots should be in use by now (now task can starve now)
				// in the end, aquire all write slots back
				// if this is successfull, all writing taks should have completed
				for (int i = 0; i < threadCount; i++)
					linkWrites.WaitOne();

				sw.Stop();

				metaWriter.WriteLine("TotalTitles: " + totalTitles);
				metaWriter.WriteLine("TotalLinks: " + totalLinks);
				metaWriter.WriteLine("Finished after: " + sw.Elapsed);

				Console.WriteLine();
				Console.WriteLine("Finished after " + sw.Elapsed);
			}
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
			writer.WriteLine("File created: " + DateTime.Now);
			writer.WriteLine("Database: " + meta.DatabaseName);
			writer.WriteLine("Generator: " + meta.Generator);
		}

		private static void WritePageTitle(RawPage page, TextWriter writer)
		{
			Interlocked.Increment(ref totalTitles);
			//writer.WriteLine(page.Id);
			writer.WriteLine(page.Title);
			writer.WriteLine(CanonicalPageName(page.Title));
		}

		private static Regex linkRegex = new Regex("\\[\\[([^#|]+?)(#.*?)?(\\|.*?)?\\]\\]", RegexOptions.Compiled);

		private static void WritePageLinksAsync(RawPage page, TextWriter writer)
		{
			// skip all pages which are not in the main/article namespace
			// see: http://en.wikipedia.org/wiki/Wikipedia:Namespace
			if (page.NamespaceId != 0)
				return;

			linkWrites.WaitOne(); // wait for write slot

			Task.Run(() =>
			{
				try
				{
					page.Text = page.Text.Replace('\n', ' ');

					// find links using regex and make them unique (this is expensive and can take half an hour !!!)
					var matches = linkRegex.Matches(page.Text).Cast<Match>();
					var matchedLinks = matches.Select(m => m.Groups[1].Value);
					var links = matchedLinks
						.Where(l => !l.Contains(':')) // filter links to pages not in namespace main/articles and File: links
						.Select(l => CanonicalPageName(l))
						.Where(l => l.Length > 0) // yes, there are wikipedia users who put empty links in their articles ...
						.Distinct()
						.ToArray(); // evaluate eager to keep locked region as short as possible

					lock (writer)
					{
						//writer.WriteLine(page.Id);
						writer.WriteLine(page.Title);
						//writer.WriteLine(page.Text);
						foreach (var link in links)
						{
							Interlocked.Increment(ref totalLinks);
							writer.Write(link + ",");
						}
						writer.WriteLine();
						//writer.WriteLine();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Write task failed: " + e);
					throw e;
				}
				finally
				{
					linkWrites.Release(); // release a write slot
				}

			});
		}

		private static string CanonicalPageName(string link)
		{
			string l = link;
			l = l.Replace(' ', '_'); // space and underscore are equivalent
			l = l.Trim('_'); // trim spaces and underscores at the start and end
			l = HttpUtility.HtmlDecode(l); // decode html entities
			if (l.Length > 0 && char.IsLower(l[0])) // ensure first character is upper case
				l = char.ToUpper(l[0]) + l.Substring(1);

			return l;
		}
	}
}
