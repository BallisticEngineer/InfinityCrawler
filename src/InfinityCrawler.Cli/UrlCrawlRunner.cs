using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InfinityCrawler.Cli
{
	public class UrlCrawlRunner
	{
		public const string UserAgent = "InfinityCrawler.Cli/1.0";

		private HttpClient HttpClient { get; }

		public UrlCrawlRunner(HttpClient httpClient = null)
		{
			HttpClient = httpClient;
		}

		public async Task<CrawlResult> RunAsync(Uri startUri, int? maxPages)
		{
			var crawler = HttpClient != null ? new Crawler(HttpClient) : new Crawler();

			return await crawler.Crawl(startUri, new CrawlSettings
			{
				UserAgent = UserAgent,
				RestrictCrawlToStartingPath = true,
				MaxNumberOfPagesToCrawl = maxPages ?? 0
			});
		}
	}
}