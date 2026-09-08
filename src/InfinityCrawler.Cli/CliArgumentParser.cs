using System;

namespace InfinityCrawler.Cli
{
	public static class CliArgumentParser
	{
		private const string MaxPagesPrefix = "--max-pages=";
		private const string OutputPrefix = "--output=";

		public static CliArguments Parse(string[] args)
		{
			var maxPages = (int?)null;
			var outputPath = (string)null;
			Uri startUri = null;

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (arg == "-h" || arg == "--help")
				{
					return CliArguments.ForHelp();
				}

				if (arg == "--max-pages")
				{
					if (i + 1 >= args.Length)
					{
						return CliArguments.ForError("Missing a value for the '--max-pages' option.");
					}

					i++;
					var maxPagesResult = TryParseMaxPages(args[i]);
					if (maxPagesResult.Error != null)
					{
						return CliArguments.ForError(maxPagesResult.Error);
					}

					maxPages = maxPagesResult.MaxPages;
					continue;
				}

				if (arg.StartsWith(MaxPagesPrefix, StringComparison.Ordinal))
				{
					var maxPagesResult = TryParseMaxPages(arg.Substring(MaxPagesPrefix.Length));
					if (maxPagesResult.Error != null)
					{
						return CliArguments.ForError(maxPagesResult.Error);
					}

					maxPages = maxPagesResult.MaxPages;
					continue;
				}

				if (arg == "--output")
				{
					if (outputPath != null)
					{
						return CliArguments.ForError("The '--output' option was specified more than once.");
					}

					if (i + 1 >= args.Length)
					{
						return CliArguments.ForError("Missing a value for the '--output' option.");
					}

					i++;
					var outputResult = TryParseOutputValue(args[i]);
					if (outputResult.Error != null)
					{
						return CliArguments.ForError(outputResult.Error);
					}

					outputPath = outputResult.OutputPath;
					continue;
				}

				if (arg.StartsWith(OutputPrefix, StringComparison.Ordinal))
				{
					if (outputPath != null)
					{
						return CliArguments.ForError("The '--output' option was specified more than once.");
					}

					var outputResult = TryParseOutputValue(arg.Substring(OutputPrefix.Length));
					if (outputResult.Error != null)
					{
						return CliArguments.ForError(outputResult.Error);
					}

					outputPath = outputResult.OutputPath;
					continue;
				}

				if (arg.StartsWith("-", StringComparison.Ordinal))
				{
					return CliArguments.ForError($"Unknown option '{arg}'.");
				}

				if (startUri != null)
				{
					return CliArguments.ForError($"Unexpected argument '{arg}'.");
				}

				if (!TryParseStartUri(arg, out startUri))
				{
					return CliArguments.ForError($"'{arg}' is not a valid absolute HTTP or HTTPS URL.");
				}
			}

			if (startUri == null)
			{
				return CliArguments.ForError("Missing the required start URL.");
			}

			return CliArguments.ForRun(startUri, maxPages, outputPath);
		}

		private static bool TryParseStartUri(string value, out Uri startUri)
		{
			if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
				(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			{
				startUri = uri;
				return true;
			}

			startUri = null;
			return false;
		}

		private static MaxPagesParseResult TryParseMaxPages(string value)
		{
			if (!int.TryParse(value, out var parsed) || parsed <= 0)
			{
				return new MaxPagesParseResult
				{
					Error = $"Invalid value for the '--max-pages' option: '{value}'. Expected a positive integer."
				};
			}

			return new MaxPagesParseResult
			{
				MaxPages = parsed
			};
		}

		private static OutputParseResult TryParseOutputValue(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new OutputParseResult
				{
					Error = "Missing a file path for the '--output' option."
				};
			}

			return new OutputParseResult
			{
				OutputPath = value
			};
		}

		private class MaxPagesParseResult
		{
			public int? MaxPages { get; set; }
			public string Error { get; set; }
		}

		private class OutputParseResult
		{
			public string OutputPath { get; set; }
			public string Error { get; set; }
		}
	}
}