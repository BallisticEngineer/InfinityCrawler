using System;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class PathScopedCrawlingTests : CrawlerTestBase
	{
		private Task<CrawlResult> CrawlPathScopedSiteAsync(Uri startUri, bool restrictCrawlToStartingPath)
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "PathScopedSite"
			});
			var settings = new CrawlSettings
			{
				RestrictCrawlToStartingPath = restrictCrawlToStartingPath,
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = GetNoDelayRequestProcessorOptions()
			};
			return crawler.Crawl(startUri, settings);
		}

		[TestMethod]
		public async Task DefaultBehaviourRemainsUnrestricted()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/"), false);

			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/geese/index.html")));
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/index.html")));
		}

		[TestMethod]
		public async Task InScopeChildLinksAreCrawled()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/index.html")));
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/mallard.html")));
		}

		[TestMethod]
		public async Task NestedDescendantsAreCrawled()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/species/wood-duck.html")));
		}

		[TestMethod]
		public async Task SameHostOutOfScopeLinksAreNotCrawled()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/geese/index.html")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/blog/ducks/index.html")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/")));
		}

		[TestMethod]
		public async Task PrefixConfusionPathsAreRejected()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			//ducklings/ must not be treated as being within the /ducks/ scope
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducklings/index.html")));
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/index.html")));
		}

		[TestMethod]
		public async Task SitemapUrlsOutsideScopeAreRejected()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			//The sitemap seeds http://localhost/index.html which is outside the /ducks/ scope
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/index.html")));
		}

		[TestMethod]
		public async Task QueryStringsOnInScopePathsAreAllowed()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/query-page.html?sort=name")));
		}

		[TestMethod]
		public async Task RedirectsToInScopePathsAreFollowed()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			var redirectedUri = result.CrawledUris.FirstOrDefault(c => c.Location == new Uri("http://localhost/ducks/redirect-destination.html"));
			Assert.IsNotNull(redirectedUri);
			Assert.AreEqual(CrawlStatus.Crawled, redirectedUri.Status);
			Assert.AreEqual(1, redirectedUri.RedirectChain.Count);
		}

		[TestMethod]
		public async Task RedirectsToOutOfScopePathsAreNotFollowed()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/geese/index.html")));
		}

		[TestMethod]
		public async Task HostRestrictionsRemainIntact()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks/"), true);

			var duckIndex = result.CrawledUris.FirstOrDefault(c => c.Location == new Uri("http://localhost/ducks/index.html"));
			Assert.IsNotNull(duckIndex);
			Assert.IsTrue(duckIndex.Content.Links.Any(l => l.Location == new Uri("http://not-allowed-domain.com/")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location.Host == "not-allowed-domain.com"));
		}

		[TestMethod]
		public async Task BareAndTrailingSlashScopesAreHandledConsistently()
		{
			var result = await CrawlPathScopedSiteAsync(new Uri("http://localhost/ducks"), true);
Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/index.html")));
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/ducks/mallard.html")));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == new Uri("http://localhost/geese/index.html")));
		}
	}
}