using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace InfinityCrawler.Cli
{
	public static class CrawlResultFormatter
	{
		public static IEnumerable<string> FormatResultLines(IEnumerable<CrawledUri> results)
		{
			return PrepareEntries(results)
				.Select(FormatResultEntry);
		}

		public static IEnumerable<string> FormatUrlLines(IEnumerable<CrawledUri> results)
		{
			return PrepareEntries(results)
				.Select(entry => entry.Location.ToString());
		}

		public static string FormatResultLine(CrawledUri result)
		{
			return FormatResultEntry(new ResultEntry
			{
				Location = result.Location,
				FinalStatusCode = GetFinalStatusCode(result.Requests),
				Label = GetStatusLabel(result.Status)
			});
		}

		public static string GetStatusLabel(CrawlStatus status)
		{
			switch (status)
			{
				case CrawlStatus.RobotsBlocked:
					return "ROBOTS";
				case CrawlStatus.MaxRetries:
					return "MAX-RETRIES";
				case CrawlStatus.MaxRedirects:
					return "MAX-REDIRECTS";
				case CrawlStatus.Crawled:
				default:
					return "CRAWLED";
			}
		}

		private static IEnumerable<ResultEntry> PrepareEntries(IEnumerable<CrawledUri> results)
		{
			return results
				.SelectMany(GetResultEntries)
				.GroupBy(entry => entry.Location.ToString(), StringComparer.Ordinal)
				.Select(group => group
					.OrderBy(entry => entry.FinalStatusCode.HasValue ? 0 : 1)
					.First())
				.OrderBy(entry => entry.Location.ToString(), StringComparer.Ordinal);
		}

		private static IEnumerable<ResultEntry> GetResultEntries(CrawledUri result)
		{
			yield return new ResultEntry
			{
				Location = result.Location,
				FinalStatusCode = GetFinalStatusCode(result.Requests),
				Label = GetStatusLabel(result.Status)
			};

			if (result.RedirectChain != null)
			{
				foreach (var redirect in result.RedirectChain)
				{
					yield return new ResultEntry
					{
						Location = redirect.Location,
						FinalStatusCode = GetFinalStatusCode(redirect.Requests),
						Label = GetStatusLabel(result.Status)
					};
				}
			}
		}

		private static HttpStatusCode? GetFinalStatusCode(IList<CrawlRequest> requests)
		{
			return requests?.LastOrDefault()?.StatusCode;
		}

		private static string FormatResultEntry(ResultEntry entry)
		{
			if (entry.FinalStatusCode.HasValue)
			{
				return $"[{(int)entry.FinalStatusCode.Value}] {entry.Location}";
			}

			return $"[{entry.Label}] {entry.Location}";
		}

		private class ResultEntry
		{
			public Uri Location { get; set; }
			public HttpStatusCode? FinalStatusCode { get; set; }
			public string Label { get; set; }
		}
	}
}