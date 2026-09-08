using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InfinityCrawler.Cli
{
	public static class UrlFileWriter
	{
		public static void Write(IEnumerable<string> urls, string filePath)
		{
			if (urls == null)
			{
				throw new ArgumentNullException(nameof(urls));
			}

			if (string.IsNullOrEmpty(filePath))
			{
				throw new ArgumentException("The output file path must not be null or empty.", nameof(filePath));
			}

			var parentDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath));
			if (!string.IsNullOrEmpty(parentDirectory))
			{
				Directory.CreateDirectory(parentDirectory);
			}

			var urlList = urls.ToList();
			var content = urlList.Count == 0 ?
				string.Empty :
				string.Join(Environment.NewLine, urlList) + Environment.NewLine;

			File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
