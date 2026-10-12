using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;

namespace InfinityCrawler.Internal
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href, string baseHref = null)
		{
			var hrefPieces = href.Split(new[] { '#' }, 2);
			var hrefWithoutFragment = hrefPieces[0];
			var hrefFragment = hrefPieces.Length > 1 ? hrefPieces[1] : null;

			if (Uri.IsWellFormedUriString(hrefWithoutFragment, UriKind.RelativeOrAbsolute))
			{
				var baseUri = pageUri;

				//Allows <base href=""> to work
				if (Uri.IsWellFormedUriString(baseHref, UriKind.RelativeOrAbsolute))
				{
					baseUri = new Uri(pageUri, baseHref);
				}

				return new UriBuilder(new Uri(baseUri, hrefWithoutFragment))
				{
					Fragment = hrefFragment
				}.Uri;
			}

			return null;
		}

		internal static HashSet<string> GetAnchorHrefs(byte[] html)
		{
			var document = new HtmlDocument();
			using (var stream = new MemoryStream(html))
			{
				//HtmlAgilityPack detects the page encoding (including non-UTF8
				//code pages such as windows-1251) the same way the content
				//processor does, so the comparison is encoding-safe.
				document.Load(stream, detectEncodingFromByteOrderMarks: true);
			}

			var anchorNodes = document.DocumentNode.SelectNodes("//a[@href]");
			if (anchorNodes == null)
			{
				return new HashSet<string>(StringComparer.Ordinal);
			}

			return new HashSet<string>(
				anchorNodes
					.Select(n => n.GetAttributeValue("href", null))
					.Where(h => h != null),
				StringComparer.Ordinal);
		}

		//Two documents expose the same discoverable link structure when they contain
		//the exact same set of anchor href values. This is deliberately a link-set
		//comparison, not a byte comparison: a site that serves the same page under
		//both a trailing-slash URL and a bare form (a "page alias") often varies
		//non-link content, but its relative links are authored against the same base.
		internal static bool HaveEquivalentAnchorSets(byte[] htmlA, byte[] htmlB)
		{
			var setA = GetAnchorHrefs(htmlA);
			var setB = GetAnchorHrefs(htmlB);
			return setA.SetEquals(setB);
		}
	}
}
