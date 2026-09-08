# Report: Stage 02 URL Enumeration CLI

Task: `instructions/02-url-enumeration-cli.md`
Date: 2026-09-08
Status: Complete

## Implementation Results

Added a deliberately small console application, `InfinityCrawler.Cli`, on top of the existing library. The CLI accepts an absolute HTTP/HTTPS start URL as the first positional argument and prints one deterministic line per crawled/discovered URI plus the most useful status representation.

- It uses the Stage 01 path-scoping feature (`CrawlSettings.RestrictCrawlToStartingPath = true`) rather than reimplementing discovery: only the start URL's path subtree is crawled.
- It builds on `Crawler.Crawl(Uri, CrawlSettings)`; no crawler/robots/sitemap/redirect/retry/throttle logic is duplicated.
- Scoping is always enabled (no CLI switch to disable). The single operational option is `--max-pages <number>` (positive integer only), mapping directly to `CrawlSettings.MaxNumberOfPagesToCrawl`; omitted = unlimited default.
- `--help` / `-h` print help to stdout and exit 0. No URL, malformed/non-absolute/non-HTTP(S) URL, invalid `--max-pages`, unknown options, and extra positional arguments print a concise error to stderr plus usage and exit 1. A normal crawl exits 0 even when individual URLs return 4xx/5xx. An unhandled crawl failure exits 2.
- CLI identity is `CrawlSettings.UserAgent = "InfinityCrawler.Cli/1.0"` instead of the library's legacy default browser-like User-Agent. Concurrency/politeness stays on library defaults.

## Files Changed

New:
- `src/InfinityCrawler.Cli/InfinityCrawler.Cli.csproj` — console project, `net6.0`, non-packable, `RootNamespace` overridden to `InfinityCrawler.Cli`, references the library.
- `src/InfinityCrawler.Cli/Program.cs` — entry point; help/error printing, line output, exit codes 0/1/2, top-level exception handling.
- `src/InfinityCrawler.Cli/CliArgumentParser.cs` + `CliArguments.cs` — argument parsing into a help/run/error result.
- `src/InfinityCrawler.Cli/UrlCrawlRunner.cs` — builds `CrawlSettings` (scoping on, CLI User-Agent, `MaxNumberOfPagesToCrawl = maxPages ?? 0`), invokes the crawler. Accepts an optional `HttpClient` so tests can inject the in-process TestSite handler; the CLI uses the parameterless `Crawler` constructor (library default handler with auto-redirects disabled).
- `src/InfinityCrawler.Cli/CrawlResultFormatter.cs` — dedupe, ordinal sort, status rendering.
- `tests/InfinityCrawler.Tests.Cli/InfinityCrawler.Tests.Cli.csproj` — MSTest project (`net6.0`) referencing the library, the CLI, and the TestSite.
- `tests/InfinityCrawler.Tests.Cli/CliArgumentParserTests.cs`
- `tests/InfinityCrawler.Tests.Cli/CrawlResultFormatterTests.cs`
- `tests/InfinityCrawler.Tests.Cli/UrlCrawlRunnerTests.cs`

Modified:
- `InfinityCrawler.sln` — added both projects (SDK-style GUIDs, Debug/Release|Any CPU only, nested under existing `src`/`tests` folders), matching the repo's existing solution format exactly.

No library files were modified in this stage (Stage 01 implementation untouched).

## CLI Usage and Examples

```text
InfinityCrawler.Cli <url> [--max-pages <number>]
InfinityCrawler.Cli --help
```

Development invocation:

```powershell
dotnet run --project src/InfinityCrawler.Cli -- https://example.com/ducks/
dotnet run --project src/InfinityCrawler.Cli -- https://example.com/ducks/ --max-pages 50
```

Representative output (deterministic, ordinal-sorted, `[NNN]` = final request's HTTP status):

```text
[200] http://localhost/ducks/
[200] http://localhost/ducks/index.html
[200] http://localhost/ducks/mallard.html
[200] http://localhost/ducks/query-page.html?sort=name
[200] http://localhost/ducks/redirect-destination.html
[200] http://localhost/ducks/species/wood-duck.html
```

Terminal states without a final HTTP status render short labels, e.g.:

```text
[403] https://example.com/ducks/private/
[ROBOTS] https://example.com/ducks/robots-blocked/
[MAX-RETRIES] https://example.com/ducks/timing-out/
[MAX-REDIRECTS] https://example.com/ducks/loop/
```

## Status-Rendering Design

Inspect of the existing model:
- `CrawledUri` carries `Location`, `CrawlStatus` (`Crawled`, `RobotsBlocked`, `MaxRetries`, `MaxRedirects`), `Requests` (one `CrawlRequest` per attempt, in order, with nullable `HttpStatusCode`), and `RedirectChain`.
- Redirect hops are consumed into `RedirectChain`; the result's `Requests` belong to the final hop.

Rendering rules implemented in `CrawlResultFormatter`:
1. If `result.Requests.LastOrDefault()?.StatusCode` has a value, print the numeric HTTP status (`[200]`, `[403]`, ...). This covers `Crawled` results (success or non-2xx), 5xx-into-`MaxRetries` results, and in-page-`RobotsBlocked` results where an actual response code exists — the code is always taken from request history, never guessed.
2. Otherwise map the terminal state to a stable label: `RobotsBlocked` → `ROBOTS`, `MaxRetries` → `MAX-RETRIES`, `MaxRedirects` → `MAX-REDIRECTS`, `Crawled` (no HTTP history, defensive only) → `CRAWLED`.
3. No inference of success from `CrawlStatus.Crawled`; a 403 result renders `[403]`.
4. No library result-model changes were required.

Output shaping: dedupe by final `Location` (ordinal) and sort by `Location` ordinal before formatting, producing exact-deduplicated, deterministic lines.

## Tests Added

`InfinityCrawler.Tests.Cli` (25 tests):

- `CliArgumentParserTests` (18): valid absolute HTTP/HTTPS URL accepted; malformed URL, relative URL, and non-HTTP scheme rejected; no-arguments → usage/error; `--help` and `-h`; valid `--max-pages` (separate-token, before-URL, and `--max-pages=N` forms); zero/negative/non-numeric/missing-value rejected; unknown option rejected; extra positional rejected. Also verifies `StartUri`/`MaxPages` are carried correctly for the success case.
- `CrawlResultFormatterTests` (6): HTTP status from the final request (redirect history → `[200]`); 403 rendered without inferring success from `Crawled`; terminal-state labels when no HTTP status exists (`ROBOTS`, `MAX-RETRIES`, `MAX-REDIRECTS`); ordinal sort (case-sensitive, `B.html` before `a.html`); duplicate final URIs collapse to one line.
- `UrlCrawlRunnerTests` (1 integration): uses the in-process TestSite (`PathScopedSite`) with no real network. Crawling `http://localhost/ducks/` asserts in-scope lines are produced and out-of-scope siblings (`/geese/`, `/ducklings/`, `/blog/ducks/`, `not-allowed-domain`) are absent, and that output is ordinal-sorted. This directly satisfies the CLI-logic integration requirement without a brittle subprocess test.

The tests target `UrlCrawlRunner`/parser/formatter directly, so no real web requests or subprocess launches are involved.

## Validation Performed

- `dotnet build InfinityCrawler.sln` — succeeded, 0 errors. Warnings remain the known classes: `NETSDK1138` (net6.0 out of support, including the new net6.0 projects) and the pre-existing `NU1504` duplicate `PackageReference` in `InfinityCrawler.Tests.TestSite.csproj`.
- `dotnet test tests/InfinityCrawler.Tests -f net6.0` — 37/37 passed (Stage 01 + pre-existing, no regressions).
- `dotnet test tests/InfinityCrawler.Tests.Cli` — 25/25 passed.
- `dotnet run --project src/InfinityCrawler.Cli -- --help` — help printed, exit code 0 (see Manual Validation).
- `dotnet run --project src/InfinityCrawler.Cli -- not-a-url` — `'not-a-url' is not a valid absolute HTTP or HTTPS URL.` + usage on stderr, exit code 1.
- net461 not used as a gate (pre-existing assembly-binding issue, per instructions).

## Manual Validation Performed

```powershell
dotnet run --project src/InfinityCrawler.Cli -- --help
```

Output (stdout, exit 0):

```text
InfinityCrawler.Cli - enumerate the URLs discovered within a start URL's path subtree.

Usage: InfinityCrawler.Cli <url> [--max-pages <number>]

Arguments:
  <url>                Absolute HTTP or HTTPS start URL to crawl.

Options:
  --max-pages <number> Maximum number of pages to crawl.
  -h, --help           Show this help and exit.
```

```powershell
dotnet run --project src/InfinityCrawler.Cli -- not-a-url
```

Output (stderr, exit 1):

```text
'not-a-url' is not a valid absolute HTTP or HTTPS URL.
Usage: InfinityCrawler.Cli <url> [--max-pages <number>]
```

The CLI's crawl-and-print path was demonstrated against the in-process TestSite via the `UrlCrawlRunnerTests` integration test (the TestServer only exists inside the test process, so a standalone `dotnet run` against it is not possible). A safe public documentation site was not used, per the instruction not to make external sites a dependency.

## Unresolved Issues

None.

## Deviations From Instructions

None in functionality. Key decisions worth noting:

- Redirect source URIs are consumed into the library's `RedirectChain`, so only the redirect's final URI produces an output line (with that hop's HTTP status). A `[301] example.com/ducks/old/` line only appears when the crawler surfaces a terminal state for that hop; the spec allows status-rendering variation where the crawler exposes non-HTTP terminal states.
- A `Crawled` fallback label (`CRAWLED`) exists to keep rendering total in the defensive case where a `Crawled` result has no request history; it is not reachable through normal crawl flows.

## Relevant Errors Or Blockers

- Initial CLI test run: 2 failures, both caused by test expectation mistakes (helper produced requests with no HTTP status, so `CRAWLED` rendered instead of `[200]`), plus a `.Select()` on a null array. Fixed in the helper (default a `Crawled` result to one `200` request; empty array otherwise). No library or production-CLI code was affected.
- `dotnet sln add` rewrote the entire `.sln` (legacy project-type GUIDs and x64/x86 platform entries for every project). The solution file was reverted and the two projects were added by hand using the repo's existing SDK-style format, keeping the diff to 14 added lines.

## Information Needed For the Next Decision Or Task

- Stage 02 is self-contained and builds on the approved Stage 01 scoping. No follow-up is required.
- Natural next stages when requested: richer export/UX for the URL enumeration output, or additional operational flags (the parser is centralized and easy to extend). The current CLI intentionally exposes only `--max-pages` per the stage scope.
- `G:\My Drive\Arden\InfinityCrawler\CONTEXT.md` should be updated to reflect Stage 02 completion if that is part of the project's review workflow.