using System;

namespace InfinityCrawler.Cli
{
	public class CliArguments
	{
		public bool ShowHelp { get; private set; }

		public Uri StartUri { get; private set; }

		public int? MaxPages { get; private set; }

		public string OutputPath { get; private set; }

		public string Error { get; private set; }

		public bool HasError
		{
			get
			{
				return Error != null;
			}
		}

		public static CliArguments ForHelp()
		{
			return new CliArguments
			{
				ShowHelp = true
			};
		}

		public static CliArguments ForRun(Uri startUri, int? maxPages, string outputPath)
		{
			return new CliArguments
			{
				StartUri = startUri,
				MaxPages = maxPages,
				OutputPath = outputPath
			};
		}

		public static CliArguments ForError(string error)
		{
			return new CliArguments
			{
				Error = error
			};
		}
	}
}