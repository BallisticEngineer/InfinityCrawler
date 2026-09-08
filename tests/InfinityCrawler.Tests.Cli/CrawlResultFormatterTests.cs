using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using InfinityCrawler.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests.Cli
{
	[TestClass]
	public class CrawlResultFormatterTests
	{
		[TestMethod]
		public void HttpStatusCodeRenderedFromFinalRequest()
		{
			var result = CreateResult("http://example.com/ducks/mallard.html",
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.MovedPermanently, HttpStatusCode.OK });

			var line = CrawlResultFormatter.FormatResultLine(result);

			Assert.AreEqual("[200] http://example.com/ducks/mallard.html", line);
		}

		[TestMethod]
		public void NonSuccessHttpStatusRenderedWithoutInferringCrawlState()
		{
			var result = CreateResult("http://example.com/ducks/private.html", CrawlStatus.Crawled,
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.Forbidden });

			var line = CrawlResultFormatter.FormatResultLine(result);

			Assert.AreEqual("[403] http://example.com/ducks/private.html", line);
		}

		[TestMethod]
		public void RobotsBlockedStatusRenderedWhenNoHttpStatusExists()
		{
			var result = CreateResult("http://example.com/ducks/", CrawlStatus.RobotsBlocked);

			var line = CrawlResultFormatter.FormatResultLine(result);

			Assert.AreEqual("[ROBOTS] http://example.com/ducks/", line);
		}

		[TestMethod]
		public void MaxRetriesStatusRenderedWhenNoHttpStatusExists()
		{
			var result = CreateResult("http://example.com/ducks/", CrawlStatus.MaxRetries,
				requestStatusCodes: new HttpStatusCode?[] { null });

			var line = CrawlResultFormatter.FormatResultLine(result);

			Assert.AreEqual("[MAX-RETRIES] http://example.com/ducks/", line);
		}

		[TestMethod]
		public void MaxRedirectsStatusRenderedWhenNoHttpStatusExists()
		{
			var result = CreateResult("http://example.com/ducks/", CrawlStatus.MaxRedirects);

			var line = CrawlResultFormatter.FormatResultLine(result);

			Assert.AreEqual("[MAX-REDIRECTS] http://example.com/ducks/", line);
		}

		[TestMethod]
		public void LinesAreSortedWithOrdinalComparison()
		{
			var results = new[]
			{
				CreateResult("http://example.com/z.html"),
				CreateResult("http://example.com/B.html"),
				CreateResult("http://example.com/m.html"),
				CreateResult("http://example.com/a.html")
			};

			var lines = CrawlResultFormatter.FormatResultLines(results).ToArray();

			Assert.AreEqual(4, lines.Length);
			Assert.AreEqual("[200] http://example.com/B.html", lines[0]);
			Assert.AreEqual("[200] http://example.com/a.html", lines[1]);
			Assert.AreEqual("[200] http://example.com/m.html", lines[2]);
			Assert.AreEqual("[200] http://example.com/z.html", lines[3]);
		}

		[TestMethod]
		public void DuplicateFinalUrisProduceSingleLine()
		{
			var results = new[]
			{
				CreateResult("http://example.com/ducks/index.html"),
				CreateResult("http://example.com/ducks/index.html", CrawlStatus.RobotsBlocked)
			};

			var lines = CrawlResultFormatter.FormatResultLines(results).ToArray();

			Assert.AreEqual(1, lines.Length);
			Assert.AreEqual("[200] http://example.com/ducks/index.html", lines[0]);
		}

		[TestMethod]
		public void RedirectSourceUrisAreEnumeratedWithTheirOwnHttpStatus()
		{
			var result = CreateResult("http://example.com/ducks/new.html",
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.OK },
				redirectChain: new[]
				{
					CreateRedirect("http://example.com/ducks/old.html", HttpStatusCode.MovedPermanently)
				});

			var lines = CrawlResultFormatter.FormatResultLines(new[] { result }).ToArray();

			Assert.AreEqual(2, lines.Length);
			Assert.AreEqual("[200] http://example.com/ducks/new.html", lines[0]);
			Assert.AreEqual("[301] http://example.com/ducks/old.html", lines[1]);
		}

		[TestMethod]
		public void MultipleRedirectHopsAreAllEnumerated()
		{
			var result = CreateResult("http://example.com/final.html",
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.OK },
				redirectChain: new[]
				{
					CreateRedirect("http://example.com/a.html", HttpStatusCode.MovedPermanently),
					CreateRedirect("http://example.com/b.html", HttpStatusCode.Found)
				});

			var lines = CrawlResultFormatter.FormatResultLines(new[] { result }).ToArray();

			Assert.AreEqual(3, lines.Length);
			Assert.AreEqual("[301] http://example.com/a.html", lines[0]);
			Assert.AreEqual("[302] http://example.com/b.html", lines[1]);
			Assert.AreEqual("[200] http://example.com/final.html", lines[2]);
		}

		[TestMethod]
		public void RedirectHopPreferHttpStatusOverFallbackLabelOnDuplicateUri()
		{
			var duplicated = CreateResult("http://example.com/ducks/",
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.OK },
				redirectChain: new[]
				{
					CreateRedirect("http://example.com/ducks/old.html", HttpStatusCode.MovedPermanently)
				});
			var fallback = CreateResult("http://example.com/ducks/old.html", CrawlStatus.MaxRetries);

			var lines = CrawlResultFormatter.FormatResultLines(new[] { duplicated, fallback }).ToArray();

			Assert.AreEqual(2, lines.Length);
			Assert.AreEqual("[200] http://example.com/ducks/", lines[0]);
			Assert.AreEqual("[301] http://example.com/ducks/old.html", lines[1]);
		}

		[TestMethod]
		public void UrlLinesContainOnlyBareUrls()
		{
			var results = new[]
			{
				CreateResult("http://example.com/ducks/mallard.html"),
				CreateResult("http://example.com/ducks/", CrawlStatus.RobotsBlocked),
				CreateResult("http://example.com/ducks/private.html", CrawlStatus.Crawled,
					requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.Forbidden })
			};

			var lines = CrawlResultFormatter.FormatUrlLines(results).ToArray();

			CollectionAssert.AreEqual(new[]
			{
				"http://example.com/ducks/",
				"http://example.com/ducks/mallard.html",
				"http://example.com/ducks/private.html"
			}, lines);
			Assert.IsFalse(lines.Any(line => line.Contains("[")));
			Assert.IsFalse(lines.Any(line => line.Contains("]")));
		}

		[TestMethod]
		public void UrlLinesMatchStatusLineInventoryAfterPrefixRemoval()
		{
			var results = new[]
			{
				CreateResult("http://example.com/z.html"),
				CreateResult("http://example.com/B.html"),
				CreateResult("http://example.com/m.html"),
				CreateResult("http://example.com/a.html"),
				CreateResult("http://example.com/ducks/index.html", CrawlStatus.RobotsBlocked)
			};

			var statusLines = CrawlResultFormatter.FormatResultLines(results).ToArray();
			var urlLines = CrawlResultFormatter.FormatUrlLines(results).ToArray();

			var strippedStatusLines = statusLines
				.Select(line => line.Substring(line.IndexOf("] ") + 2))
				.ToArray();
			CollectionAssert.AreEqual(strippedStatusLines, urlLines);
		}

		[TestMethod]
		public void RedirectSourceUrisAreEnumeratedInUrlLines()
		{
			var result = CreateResult("http://example.com/ducks/new.html",
				requestStatusCodes: new HttpStatusCode?[] { HttpStatusCode.OK },
				redirectChain: new[]
				{
					CreateRedirect("http://example.com/ducks/old.html", HttpStatusCode.MovedPermanently)
				});

			var lines = CrawlResultFormatter.FormatUrlLines(new[] { result }).ToArray();

			CollectionAssert.AreEqual(new[]
			{
				"http://example.com/ducks/new.html",
				"http://example.com/ducks/old.html"
			}, lines);
		}

		[TestMethod]
		public void FallbackLabelOmittedButUrlKeptInUrlLines()
		{
			var result = CreateResult("http://example.com/ducks/", CrawlStatus.MaxRetries,
				requestStatusCodes: new HttpStatusCode?[] { null });

			var lines = CrawlResultFormatter.FormatUrlLines(new[] { result }).ToArray();

			CollectionAssert.AreEqual(new[] { "http://example.com/ducks/" }, lines);
			Assert.IsFalse(lines.Any(line => line.Contains("MAX-RETRIES")));
			Assert.IsFalse(lines.Any(line => line.Contains("ROBOTS")));
		}

		private static CrawledUri CreateResult(string location, CrawlStatus status = CrawlStatus.Crawled, HttpStatusCode?[] requestStatusCodes = null, IEnumerable<CrawledUriRedirect> redirectChain = null)
		{
			var requests = requestStatusCodes ?? (status == CrawlStatus.Crawled ?
				new HttpStatusCode?[] { HttpStatusCode.OK } :
				Array.Empty<HttpStatusCode?>());

			return new CrawledUri
			{
				Location = new Uri(location),
				Status = status,
				Requests = requests
					.Select(code => new CrawlRequest
					{
						StatusCode = code
					})
					.ToList(),
				RedirectChain = redirectChain?.ToList() ?? new List<CrawledUriRedirect>()
			};
		}

		private static CrawledUriRedirect CreateRedirect(string location, HttpStatusCode statusCode)
		{
			return new CrawledUriRedirect
			{
				Location = new Uri(location),
				Requests = new List<CrawlRequest>
				{
					new CrawlRequest
					{
						StatusCode = statusCode
					}
				}
			};
		}
	}
}