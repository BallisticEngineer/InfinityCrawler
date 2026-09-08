using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfinityCrawler.Cli
{
	public static class Program
	{
		public const int ExitCodeSuccess = 0;
		public const int ExitCodeArgumentError = 1;
		public const int ExitCodeCrawlError = 2;

		private const string ShortUsage = "Usage: InfinityCrawler.Cli <url> [--max-pages <number>] [--output <file>]";

		private const string HelpText =
			"InfinityCrawler.Cli - enumerate the URLs discovered within a start URL's path subtree.\n\n" +
			ShortUsage + "\n\n" +
			"Arguments:\n" +
			"  <url>                Absolute HTTP or HTTPS start URL to crawl.\n\n" +
			"Options:\n" +
			"  --max-pages <number> Maximum number of pages to crawl.\n" +
			"  --output <file>      Write discovered URLs to a plain text file, one URL per line.\n" +
			"  -h, --help           Show this help and exit.";

		public static async Task<int> Main(string[] args)
		{
			var arguments = CliArgumentParser.Parse(args);

			if (arguments.ShowHelp)
			{
				Console.WriteLine(HelpText);
				return ExitCodeSuccess;
			}

			if (arguments.HasError)
			{
				Console.Error.WriteLine(arguments.Error);
				Console.Error.WriteLine(ShortUsage);
				return ExitCodeArgumentError;
			}

			List<string> urlLines;
			try
			{
				var crawlResult = await new UrlCrawlRunner().RunAsync(arguments.StartUri, arguments.MaxPages);
				urlLines = CrawlResultFormatter.FormatUrlLines(crawlResult.CrawledUris).ToList();

				foreach (var line in CrawlResultFormatter.FormatResultLines(crawlResult.CrawledUris))
				{
					Console.WriteLine(line);
				}
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine($"The crawl failed: {exception.Message}");
				return ExitCodeCrawlError;
			}

			if (arguments.OutputPath != null)
			{
				try
				{
					UrlFileWriter.Write(urlLines, arguments.OutputPath);
				}
				catch (Exception exception)
				{
					Console.Error.WriteLine($"Failed to write the output file: {exception.Message}");
					return ExitCodeCrawlError;
				}
			}

			return ExitCodeSuccess;
		}
	}
}