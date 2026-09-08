using InfinityCrawler.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests.Cli
{
	[TestClass]
	public class CliArgumentParserTests
	{
		[TestMethod]
		public void ValidAbsoluteHttpUrlAccepted()
		{
			var args = CliArgumentParser.Parse(new[] { "http://example.com/ducks/" });

			Assert.IsFalse(args.ShowHelp);
			Assert.IsFalse(args.HasError);
			Assert.IsNotNull(args.StartUri);
			Assert.AreEqual("http://example.com/ducks/", args.StartUri.ToString());
			Assert.IsNull(args.MaxPages);
		}

		[TestMethod]
		public void ValidAbsoluteHttpsUrlAccepted()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/" });

			Assert.IsFalse(args.HasError);
			Assert.IsNotNull(args.StartUri);
			Assert.AreEqual("https://example.com/ducks/", args.StartUri.ToString());
		}

		[TestMethod]
		public void MalformedUrlRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "not-a-url" });

			Assert.IsTrue(args.HasError);
			Assert.IsNull(args.StartUri);
		}

		[TestMethod]
		public void RelativeUrlRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "/ducks/" });

			Assert.IsTrue(args.HasError);
			Assert.IsNull(args.StartUri);
		}

		[TestMethod]
		public void NonHttpSchemeRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "ftp://example.com/ducks/" });

			Assert.IsTrue(args.HasError);
			Assert.IsNull(args.StartUri);
		}

		[TestMethod]
		public void NoArgumentsReturnsUsageError()
		{
			var args = CliArgumentParser.Parse(new string[0]);

			Assert.IsTrue(args.HasError);
			Assert.IsFalse(args.ShowHelp);
			Assert.IsNull(args.StartUri);
		}

		[TestMethod]
		public void HelpSwitchIsAccepted()
		{
			var args = CliArgumentParser.Parse(new[] { "--help" });

			Assert.IsTrue(args.ShowHelp);
			Assert.IsFalse(args.HasError);
		}

		[TestMethod]
		public void ShortHelpSwitchIsAccepted()
		{
			var args = CliArgumentParser.Parse(new[] { "-h" });

			Assert.IsTrue(args.ShowHelp);
			Assert.IsFalse(args.HasError);
		}

		[TestMethod]
		public void ValidMaxPagesValueParsed()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages", "10" });

			Assert.IsFalse(args.HasError);
			Assert.AreEqual(10, args.MaxPages);
		}

		[TestMethod]
		public void MaxPagesBeforeUrlIsParsed()
		{
			var args = CliArgumentParser.Parse(new[] { "--max-pages", "7", "https://example.com/ducks/" });

			Assert.IsFalse(args.HasError);
			Assert.AreEqual(7, args.MaxPages);
			Assert.AreEqual("https://example.com/ducks/", args.StartUri.ToString());
		}

		[TestMethod]
		public void MaxPagesEqualsFormIsParsed()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages=5" });

			Assert.IsFalse(args.HasError);
			Assert.AreEqual(5, args.MaxPages);
		}

		[TestMethod]
		public void ZeroMaxPagesRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages", "0" });

			Assert.IsTrue(args.HasError);
		}

		[TestMethod]
		public void NegativeMaxPagesRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages", "-5" });

			Assert.IsTrue(args.HasError);
		}

		[TestMethod]
		public void NonNumericMaxPagesRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages", "many" });

			Assert.IsTrue(args.HasError);
		}

		[TestMethod]
		public void MissingMaxPagesValueRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "--max-pages" });

			Assert.IsTrue(args.HasError);
		}

		[TestMethod]
		public void UnknownOptionRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "--verbose", "https://example.com/ducks/" });

			Assert.IsTrue(args.HasError);
		}

		[TestMethod]
		public void ExtraPositionalArgumentRejected()
		{
			var args = CliArgumentParser.Parse(new[] { "https://example.com/ducks/", "extra" });

			Assert.IsTrue(args.HasError);
		}
	}
}