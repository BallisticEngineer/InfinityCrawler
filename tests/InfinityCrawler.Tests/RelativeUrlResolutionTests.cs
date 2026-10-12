using System;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Content;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class RelativeUrlResolutionTests : ContentProcessorTestBase
	{
		private async Task<CrawledContent> PerformRequestAsync(string path)
		{
			var requestUri = new UriBuilder("http://localhost/")
			{
				Path = path
			}.Uri;

			return await RequestAndProcessContentAsync(new SiteContext
			{
				SiteFolder = "RelativeResolutionSite"
			}, requestUri, new DefaultContentProcessor());
		}

		private void AssertLink(CrawledContent crawledContent, string expectedUri)
		{
			Assert.IsTrue(
				crawledContent.Links.Any(l => l.Location == new Uri(expectedUri)),
				$"Expected a link to resolve to {expectedUri}."
			);
		}

		[TestMethod]
		public async Task NonSlashDocumentResolvesAllLinkFormsCorrectly()
		{
			var crawledContent = await PerformRequestAsync("NoSlashDocument.html");
			var expectedLinks = new[]
			{
				"http://localhost/relative-file.html",
				"http://localhost/sub/child.html",
				"http://localhost/sibling.html",
				"http://localhost/NoSlashDocument.html?view=full",
				"http://localhost/NoSlashDocument.html#top",
				"http://localhost/root/path",
				"http://test-domain.com/scheme",
				"https://test-domain.com/absolute",
				"http://localhost/no-href-element"
			};

			Assert.AreEqual(expectedLinks.Length, crawledContent.Links.Count());
			for (var i = 0; i < expectedLinks.Length; i++)
			{
				Assert.AreEqual(new Uri(expectedLinks[i]), crawledContent.Links.ElementAt(i).Location);
			}
		}

		[TestMethod]
		public async Task SlashTerminatedDocumentKeepsStandardNestedResolution()
		{
			var crawledContent = await PerformRequestAsync("trailing/");

			AssertLink(crawledContent, "http://localhost/trailing/page.html");
			AssertLink(crawledContent, "http://localhost/trailing/sub/child.html");
			AssertLink(crawledContent, "http://localhost/other.html");
		}

		[TestMethod]
		public async Task SingleQuotedErotomShapeResolvesFromTheNoSlashCanonicalForm()
		{
			var crawledContent = await PerformRequestAsync("SingleQuotedErotomShape.html");

			AssertLink(crawledContent, "http://localhost/post_65113");
			AssertLink(crawledContent, "http://localhost/post_65172");
			AssertLink(crawledContent, "http://localhost/author.php?author=RedBone");
			AssertLink(crawledContent, "http://localhost/date.php?date=2025-02-24");
			AssertLink(crawledContent, "http://localhost/view_cat.php?cat=14");
			AssertLink(crawledContent, "http://localhost/view_cat.php?cat=9");
			AssertLink(crawledContent, "http://localhost/");

			Assert.IsFalse(crawledContent.Links.Any(l => l.Location.AbsolutePath.StartsWith("/SingleQuotedErotomShape/")));
		}
	}
}