# Report: Stage 02A Redirect Source Enumeration Fix

Task: `instructions/02a-redirect-source-enumeration-fix.md`
Date: 2026-09-08
Status: Complete (awaiting review)

## Implementation Result

The CLI no longer loses redirect source URLs. `CrawlResultFormatter` now flattens each final `CrawledUri.RedirectChain` into output entries: one line per final `CrawledUri.Location` plus one line per `CrawledUriRedirect.Location`. Each entry renders its own final request HTTP status when one exists; otherwise the parent result's terminal crawl state is rendered as a fallback label (`ROBOTS`, `MAX-RETRIES`, `MAX-REDIRECTS`, `CRAWLED`).

Example behavior (as specified, from the formatter unit tests):

```text
[301] http://example.com/ducks/old.html
[200] http://example.com/ducks/new.html
```

Real redirect sources (`/ducks/redirect-to-destination`) previously vanished from output; they now appear as `[302] http://localhost/ducks/redirect-to-destination`, with the destination `/ducks/redirect-destination.html` rendered as `[200]`.

Scope was strictly honored: only `src/InfinityCrawler.Cli/CrawlResultFormatter.cs` and its tests changed. No crawler internals, Stage 01 path scoping, argument parsing, exit codes, User-Agent, `--max-pages`, or library result models were modified.

## Files Changed

Modified:
- `src/InfinityCrawler.Cli/CrawlResultFormatter.cs` — `FormatResultLines` now expands each `CrawledUri` into an entry set (final URI + one per `RedirectChain` hop) via a new `GetResultEntries` helper; dedupe/sort logic generalized to operate on the merged multi-source entry set.
- `tests/InfinityCrawler.Tests.Cli/CrawlResultFormatterTests.cs` — added 3 tests; `CreateResult` helper gained an optional `redirectChain` parameter and a `CreateRedirect` helper.
- `tests/InfinityCrawler.Tests.Cli/UrlCrawlRunnerTests.cs` — integration test extended: asserts the `[302] http://localhost/ducks/redirect-to-destination` redirect-source line is present, asserts the out-of-scope redirect source `/ducks/redirect-to-geese` is absent, and the ordering assertion now checks URI-ordinal sorting (full-line sorting no longer equals URI ordering once 3xx/2xx statuses mix).

No other files changed.

## Redirect-Flattening/Deduplication Design

Each `CrawledUri` produces a `ResultEntry` for itself plus one per `CrawledUriRedirect` in `RedirectChain` (`GetResultEntries`). Every entry carries its own `FinalStatusCode` taken from that entry's `Requests.LastOrDefault()?.StatusCode` — for a redirect hop this is that hop's own preserved request history (req. 7: never invented). A hop without an HTTP code falls back to the parent `CrawlStatus` label.

The combined multi-source entry list is then:
1. `GroupBy(Location.ToString(), StringComparer.Ordinal)` — dedup by URI across final results and redirect chains (req. 5).
2. Within each group, prefer the entry backed by an actual HTTP status code over a fallback-label entry (req. 6); ties resolved to any single one (the first ordered), keeping output deterministic — `OrderBy(entry => entry.FinalStatusCode.HasValue ? 0 : 1).First()`.
3. `OrderBy(Location.ToString(), StringComparer.Ordinal)` — one deterministic ordinal sort across both entry kinds (req. 4).

`FormatResultLine` (single-result public API, used by the existing tests) was kept and re-expressed on the same `ResultEntry`/`FormatResultEntry` path so old behavior is unchanged.

## Tests Added or Changed

`CrawlResultFormatterTests` (6 → 9):
- `RedirectSourceUrisAreEnumeratedWithTheirOwnHttpStatus` — `[200]` destination + `[301]` source from a single `RedirectChain` hop, one ordinal sort.
- `MultipleRedirectHopsAreAllEnumerated` — multi-hop chain renders `[301]`, `[302]`, `[200]` on three lines.
- `RedirectHopPreferHttpStatusOverFallbackLabelOnDuplicateUri` — a URI present both as a chain hop (301) and as a separate `MaxRetries` final result collapses to one `[301]` line (HTTP-backed preferred).

`UrlCrawlRunnerTests` (integration, extended):
- Asserts `[302] http://localhost/ducks/redirect-to-destination` appears.
- Asserts `/ducks/redirect-to-geese` (source of a redirect whose destination is out of scope) is absent — documented, intentional behavior.
- Ordering assertion reworked to validate URI-ordinal output on the merged set.

All 28 `InfinityCrawler.Tests.Cli` tests pass; the 6 pre-existing formatter tests are unchanged and pass.

## Validation Performed

- `dotnet build InfinityCrawler.sln` — 0 errors; only the known `NETSDK1138` (net6.0 EOL) and pre-existing `NU1504` warnings.
- `dotnet test tests/InfinityCrawler.Tests -f net6.0` — 37/37 passed.
- `dotnet test tests/InfinityCrawler.Tests.Cli` — 28/28 passed.
- `dotnet run --project src/InfinityCrawler.Cli -- --help` — help printed, exit 0.
- `dotnet run --project src/InfinityCrawler.Cli -- not-a-url` — error + usage on stderr, exit 1.
- net461 not used as a gate (documented pre-existing assembly-binding issue).

## Unresolved Issues

None.

## Deviations

None from the instruction. Notes on intentional, documented behavior:

- A redirect source whose redirect *destination* is out of path scope produces no line (the destination result — and its `RedirectChain`, which is where the source hop lives — is rejected by Stage 01 scoping, e.g. `/ducks/redirect-to-geese` → `/geese/index.html`). This is enforced by the crawler's scope admission, not the formatter, and holds for final results too.
- Ordering is by URI ordinal only; the `[NNN]` status prefix is not part of the sort key, so a 3xx source can sort between 2xx finals (e.g. `.../redirect-to-destination` after `.../redirect-destination.html`).

## Relevant Errors or Blockers

- Test-authoring mistake on first pass: the dedupe test initially asserted the wrong line order (`[301]` before `[200]`), which contradicts URI-ordinal sorting (`/ducks/` < `/ducks/old.html`). Fixed before running; no production code affected.
- The old full-line-ordinal sort assertion in the integration test would have broken once 3xx and 2xx lines mix (prefix order differs from URI order); updated to assert URI-ordinal ordering.

## Timestamps (SAST, this run)

- 07:31 — Stage 02A start, docs read, clock check.
- 07:33 — implementation + test edits complete; build passed (0 errors).
- 07:34 — 37/37 library + 28/28 CLI tests passed; manual `--help`/`not-a-url` validations passed.
- ~07:35 — report written; `CONTEXT.md` updated to Stage 02 + 02A implemented, awaiting review.