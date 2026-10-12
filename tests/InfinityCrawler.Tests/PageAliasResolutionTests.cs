using System;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class PageAliasResolutionTests : CrawlerTestBase
	{
		private async Task<CrawlResult> CrawlPageAliasAsync()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "PageAliasSite"
			});
			var settings = new CrawlSettings
			{
				RestrictCrawlToStartingPath = true,
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = GetNoDelayRequestProcessorOptions()
			};
			return await crawler.Crawl(new Uri("http://localhost/post_64996/"), settings);
		}

		[TestMethod]
		public async Task StartPageLinksResolveAgainstTheNoSlashAliasForm()
		{
			var result = await CrawlPageAliasAsync();

			var startPage = result.CrawledUris.FirstOrDefault(c => c.Location == new Uri("http://localhost/post_64996/"));
			Assert.IsNotNull(startPage);

			var linkUris = startPage.Content.Links.Select(l => l.Location).ToArray();
			Assert.IsTrue(linkUris.Contains(new Uri("http://localhost/post_65113")));
			Assert.IsTrue(linkUris.Contains(new Uri("http://localhost/post_65445")));
			Assert.IsTrue(linkUris.Contains(new Uri("http://localhost/author.php?author=RedBone")));
			Assert.IsTrue(linkUris.Contains(new Uri("http://localhost/view_cat.php?cat=14")));
			Assert.IsFalse(linkUris.Contains(new Uri("http://localhost/post_64996/post_65113")));
		}

		[TestMethod]
		public async Task NoMalformedNestedUrlsAreEmitted()
		{
			var result = await CrawlPageAliasAsync();

			Assert.IsFalse(result.CrawledUris.Any(c => c.Location.AbsolutePath.StartsWith("/post_64996/post_")));
		}

		[TestMethod]
		public async Task RootLevelSiblingLinksRemainOutOfScope()
		{
			var result = await CrawlPageAliasAsync();

			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/post_65113")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/view_cat.php?cat=9")));
		}

		[TestMethod]
		public async Task CleanRunEmitsOnlyTheStartPage()
		{
			var result = await CrawlPageAliasAsync();

			Assert.AreEqual(1, result.CrawledUris.Count());
			Assert.AreEqual(new Uri("http://localhost/post_64996/"), result.CrawledUris.Single().Location);
			Assert.AreEqual(CrawlStatus.Crawled, result.CrawledUris.Single().Status);
		}
	}
}