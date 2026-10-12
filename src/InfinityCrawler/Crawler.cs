using InfinityCrawler.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;
using Microsoft.Extensions.Logging;
using InfinityCrawler.Processing.Content;

namespace InfinityCrawler
{
	public class Crawler
	{
		private HttpClient HttpClient { get; }
		private ILogger Logger { get; }

		public Crawler()
		{
			HttpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = false,
				UseCookies = false
			});
		}

		public Crawler(HttpClient httpClient, ILogger logger = null)
		{
			HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Logger = logger;
		}

		public async Task<CrawlResult> Crawl(Uri siteUri, CrawlSettings settings)
		{
			var result = new CrawlResult
			{
				CrawlStart = DateTime.UtcNow
			};
			var overallCrawlStopwatch = new Stopwatch();
			overallCrawlStopwatch.Start();

			var baseUri = new Uri(siteUri.GetLeftPart(UriPartial.Authority));
			var startUriWithoutFragment = new UriBuilder(siteUri)
			{
				Fragment = null
			}.Uri;
			var robotsFile = await new RobotsFileParser(HttpClient).FromUriAsync(baseUri);

			UpdateCrawlDelay(robotsFile, settings.UserAgent, settings.RequestProcessorOptions);

			//Find the effective document base for the starting URI. Some sites serve
			//the same page with and without a trailing slash (a "page alias"), with
			//relative links authored against the no-slash canonical form only.
			var startResolutionBase = await GetStartResolutionBaseAsync(siteUri, robotsFile, settings);

			var crawlRunner = new CrawlRunner(baseUri, robotsFile, HttpClient, settings, Logger, siteUri);

			//Crawl the starting URI directly so the start of the configured scope is always included
			if (settings.RestrictCrawlToStartingPath)
			{
				crawlRunner.AddRequest(siteUri);
			}

			//Use any links referred to by the sitemap as a starting point
			var urisFromSitemap = (await new SitemapQuery(HttpClient)
				.GetAllSitemapsForDomainAsync(siteUri.Host))
				.SelectMany(s => s.Urls.Select(u => u.Location).Distinct());
			foreach (var uri in urisFromSitemap)
			{
				crawlRunner.AddRequest(uri);
			}

			result.CrawledUris = await crawlRunner.ProcessAsync(async (requestResult, crawlState) =>
			{
				using (requestResult.Content)
				{
					var headers = new CrawlHeaders(requestResult.ResponseHeaders, requestResult.ContentHeaders);
					var content = settings.ContentProcessor.Parse(
						crawlState.Location.Equals(startUriWithoutFragment) ? startResolutionBase : crawlState.Location,
						headers,
						requestResult.Content);
					requestResult.Content.Seek(0, SeekOrigin.Begin);
					content.RawContent = await new StreamReader(requestResult.Content).ReadToEndAsync();
					crawlRunner.AddResult(crawlState.Location, content);
				}
			});

			overallCrawlStopwatch.Stop();
			result.ElapsedTime = overallCrawlStopwatch.Elapsed;
			return result;
		}

		private async Task<Uri> GetStartResolutionBaseAsync(Uri siteUri, RobotsFile robotsFile, CrawlSettings settings)
		{
			//Link resolution is relative to a document base. For a path-scoped crawl
			//that starts at a trailing-slash URL such as /post_64996/, some sites serve
			//the same page (same anchor link structure) at the bare /post_64996 form,
			//with relative links authored against that no-slash canonical form. When
			//that is the case, resolving links against the bare form produces the URLs
			//the site actually exposes instead of malformed nested forms like
			//post_64996/post_65113. The still-crawled and emitted starting URI is
			//unchanged; only the start page's link resolution base is affected.
			if (!settings.RestrictCrawlToStartingPath)
			{
				return siteUri;
			}

			var path = siteUri.AbsolutePath;
			if (path.Length <= 1 ||
				!path.EndsWith("/") ||
				(siteUri.Scheme != Uri.UriSchemeHttp && siteUri.Scheme != Uri.UriSchemeHttps))
			{
				return siteUri;
			}

			var bareUri = new UriBuilder(siteUri)
			{
				Path = path.Substring(0, path.Length - 1)
			}.Uri;

			if (!robotsFile.IsAllowedAccess(bareUri, settings.UserAgent))
			{
				return siteUri;
			}

			var slashHtml = await TryFetchHtmlAsync(siteUri);
			var bareHtml = await TryFetchHtmlAsync(bareUri);
			if (slashHtml == null || bareHtml == null)
			{
				return siteUri;
			}

			if (UriExtensions.HaveEquivalentAnchorSets(slashHtml, bareHtml))
			{
				return bareUri;
			}

			return siteUri;
		}

		private async Task<byte[]> TryFetchHtmlAsync(Uri uri)
		{
			try
			{
				using (var response = await HttpClient.GetAsync(uri))
				{
					//A redirect (for example bare -> trailing slash) means the two URL
					//forms are not interchangeable and no base override may be applied.
					if (response.RequestMessage?.RequestUri != null &&
						!response.RequestMessage.RequestUri.Equals(uri))
					{
						return null;
					}

					if (!response.IsSuccessStatusCode ||
						response.Content.Headers.ContentType?.MediaType != "text/html")
					{
						return null;
					}

					//Read raw bytes rather than a decoded string: sites using non-UTF8
					//code pages (e.g. charset=windows-1251) may not be decodable by the
					//default encoding provider, and the link-set comparison works on
					//the raw HTML regardless of encoding.
					return await response.Content.ReadAsByteArrayAsync();
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void UpdateCrawlDelay(RobotsFile robotsFile, string userAgent, RequestProcessorOptions requestProcessorOptions)
		{
			var minimumCrawlDelayInMilliseconds = 0;

			//Apply Robots.txt crawl-delay (if defined)
			if (robotsFile.TryGetEntryForUserAgent(userAgent, out var accessEntry))
			{
				minimumCrawlDelayInMilliseconds = accessEntry.CrawlDelay ?? 0 * 1000;
			}

			var taskDelay = Math.Max(minimumCrawlDelayInMilliseconds, requestProcessorOptions.DelayBetweenRequestStart.TotalMilliseconds);
			requestProcessorOptions.DelayBetweenRequestStart = new TimeSpan(0, 0, 0, 0, (int)taskDelay);
		}
	}
}
