using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.Cli;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests.Cli
{
	[TestClass]
	public class UrlCrawlRunnerTests
	{
		[TestMethod]
		public async Task ScopedCrawlDoesNotEnumerateOutOfScopeSiblings()
		{
			var testSiteManager = new TestSiteManager(new SiteContext
			{
				SiteFolder = "PathScopedSite"
			});

			try
			{
				var runner = new UrlCrawlRunner(testSiteManager.GetHttpClient());
				var result = await runner.RunAsync(new Uri("http://localhost/ducks/"), null);

				var lines = CrawlResultFormatter.FormatResultLines(result.CrawledUris).ToArray();

				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/"));
				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/index.html"));
				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/mallard.html"));
				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/species/wood-duck.html"));
				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/query-page.html?sort=name"));
				Assert.IsTrue(lines.Contains("[200] http://localhost/ducks/redirect-destination.html"));
				Assert.IsTrue(lines.Contains("[302] http://localhost/ducks/redirect-to-destination"));

				Assert.IsFalse(lines.Any(line => line.Contains("/geese/")));
				Assert.IsFalse(lines.Any(line => line.Contains("/ducklings/")));
				Assert.IsFalse(lines.Any(line => line.Contains("/blog/ducks/")));
				Assert.IsFalse(lines.Any(line => line.Contains("not-allowed-domain")));
				Assert.IsFalse(lines.Any(line => line.Contains("/ducks/redirect-to-geese")));

				var sortedLines = lines
					.Select(line => new
					{
						Line = line,
						Uri = line.Substring(line.IndexOf("] ") + 2)
					})
					.OrderBy(item => item.Uri, StringComparer.Ordinal)
					.Select(item => item.Line)
					.ToArray();
				CollectionAssert.AreEqual(sortedLines, lines);
			}
			finally
			{
				testSiteManager.Dispose();
			}
		}

		[TestMethod]
		public async Task OutputFileMatchesConsoleInventory()
		{
			var testSiteManager = new TestSiteManager(new SiteContext
			{
				SiteFolder = "PathScopedSite"
			});

			var outputFilePath = Path.Combine(Path.GetTempPath(), "InfinityCrawler.UrlCrawlRunnerTests", $"{Guid.NewGuid():N}.txt");

			try
			{
				var runner = new UrlCrawlRunner(testSiteManager.GetHttpClient());
				var result = await runner.RunAsync(new Uri("http://localhost/ducks/"), null);

				var statusLines = CrawlResultFormatter.FormatResultLines(result.CrawledUris).ToArray();
				var urlLines = CrawlResultFormatter.FormatUrlLines(result.CrawledUris).ToArray();

				UrlFileWriter.Write(urlLines, outputFilePath);

				var fileLines = File.ReadAllLines(outputFilePath);

				var strippedStatusLines = statusLines
					.Select(line => line.Substring(line.IndexOf("] ") + 2))
					.ToArray();
				CollectionAssert.AreEqual(strippedStatusLines, fileLines);
				CollectionAssert.AreEqual(urlLines, fileLines);
				Assert.IsTrue(fileLines.Contains("http://localhost/ducks/redirect-destination.html"));
				Assert.IsTrue(fileLines.Contains("http://localhost/ducks/redirect-to-destination"));
				Assert.IsFalse(fileLines.Any(line => line.Contains("/geese/")));
				Assert.IsFalse(fileLines.Any(line => line.Contains("[")));
				StringAssert.StartsWith(File.ReadAllText(outputFilePath), "http://");
			}
			finally
			{
				if (File.Exists(outputFilePath))
				{
					File.Delete(outputFilePath);
				}

				testSiteManager.Dispose();
			}
		}
	}
}