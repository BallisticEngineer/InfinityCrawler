using System;
using System.IO;
using System.Linq;
using System.Text;
using InfinityCrawler.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests.Cli
{
	[TestClass]
	public class UrlFileWriterTests
	{
		private string TempDirectory { get; set; }

		[TestInitialize]
		public void Initialize()
		{
			TempDirectory = Path.Combine(Path.GetTempPath(), "InfinityCrawler.UrlFileWriterTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(TempDirectory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(TempDirectory))
			{
				Directory.Delete(TempDirectory, recursive: true);
			}
		}

		[TestMethod]
		public void WritesOneUrlPerLineWithoutBomOrDecorations()
		{
			var filePath = Path.Combine(TempDirectory, "urls.txt");
			var urls = new[]
			{
				"http://example.com/ducks/",
				"http://example.com/ducks/index.html",
				"http://example.com/ducks/mallard.html"
			};

			UrlFileWriter.Write(urls, filePath);

			var bytes = File.ReadAllBytes(filePath);
			Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "File must not start with a UTF-8 BOM.");

			var content = Encoding.UTF8.GetString(bytes);
			var expected = string.Join(Environment.NewLine, urls) + Environment.NewLine;
			Assert.AreEqual(expected, content);
			Assert.IsFalse(content.Contains("["));
			Assert.IsFalse(content.Contains("200"));
			Assert.AreEqual(3, content.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Count(line => line.Length > 0));
		}

		[TestMethod]
		public void MissingParentDirectoryIsCreated()
		{
			var filePath = Path.Combine(TempDirectory, "nested", "deeper", "urls.txt");

			UrlFileWriter.Write(new[] { "http://example.com/ducks/" }, filePath);

			Assert.IsTrue(File.Exists(filePath));
			Assert.AreEqual("http://example.com/ducks/" + Environment.NewLine, File.ReadAllText(filePath));
		}

		[TestMethod]
		public void ExistingFileIsOverwrittenWithoutAppending()
		{
			var filePath = Path.Combine(TempDirectory, "urls.txt");
			File.WriteAllText(filePath, "http://example.com/stale.html" + Environment.NewLine);

			UrlFileWriter.Write(new[] { "http://example.com/ducks/" }, filePath);

			Assert.AreEqual("http://example.com/ducks/" + Environment.NewLine, File.ReadAllText(filePath));
		}

		[TestMethod]
		public void EmptyUrlSetProducesEmptyFile()
		{
			var filePath = Path.Combine(TempDirectory, "urls.txt");

			UrlFileWriter.Write(Enumerable.Empty<string>(), filePath);

			Assert.IsTrue(File.Exists(filePath));
			Assert.AreEqual(0, new FileInfo(filePath).Length);
		}

		[TestMethod]
		public void RelativePathResolvesFromCurrentDirectory()
		{
			var fileName = $"{Guid.NewGuid():N}.txt";
			try
			{
				UrlFileWriter.Write(new[] { "http://example.com/ducks/" }, fileName);

				var resolvedPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
				Assert.IsTrue(File.Exists(resolvedPath));
				Assert.AreEqual("http://example.com/ducks/" + Environment.NewLine, File.ReadAllText(resolvedPath));
			}
			finally
			{
				var resolvedPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
				if (File.Exists(resolvedPath))
				{
					File.Delete(resolvedPath);
				}
			}
		}

		[TestMethod]
		public void NullUrlSequenceRejected()
		{
			Assert.ThrowsException<ArgumentNullException>(() => UrlFileWriter.Write(null, Path.Combine(TempDirectory, "urls.txt")));
		}

		[TestMethod]
		public void NullOrEmptyFilePathRejected()
		{
			Assert.ThrowsException<ArgumentException>(() => UrlFileWriter.Write(new[] { "http://example.com/" }, null));
			Assert.ThrowsException<ArgumentException>(() => UrlFileWriter.Write(new[] { "http://example.com/" }, ""));
		}
	}
}
