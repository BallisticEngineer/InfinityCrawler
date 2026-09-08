# Report: Optional Strict Path-Scoped Crawling

Task: `instructions/01-path-scoped-crawling.md`
Date: 2026-09-08
Status: Complete

## Implementation Results

Implemented an opt-in path-scoping feature. A new `CrawlSettings.RestrictCrawlToStartingPath` boolean (default `false`) limits the crawl to the subtree rooted at the path of the URI passed to `Crawler.Crawl()`. When enabled:

- The scope is derived from the start URI, never hard-coded. `/ducks` and `/ducks/` both resolve to scope `/ducks/`.
- Every discovered request (page links, sitemap seeds, redirect destinations, retries) passes through the same scope check at the central URI-admission point in `CrawlRunner.AddRequest(Uri, bool)`.
- Sitemap URLs outside the scope are dropped, so they cannot bypass filtering.
- Redirect destinations outside the scope are dropped before any request is queued.
- Host/`HostAliases` rules, fragment stripping, and URI deduplication are unchanged.
- Query strings on otherwise in-scope paths remain valid (comparison is on `AbsolutePath`, the request is made with the full query URI).

Scope membership rule (segment-boundary safe because the scope is normalized to a trailing `/`):

```
requestPath == scope || requestPath == scope.TrimEnd('/') || requestPath.StartsWith(scope)
```

This allows `/ducks`, `/ducks/`, `/ducks/mallard`, `/ducks?sort=name`, rejects `/`, `/geese/`, `/ducklings/`, `/blog/ducks/`.

## Files Changed

- `src/InfinityCrawler/CrawlSettings.cs` — added `RestrictCrawlToStartingPath` (1 line).
- `src/InfinityCrawler/Crawler.cs` — passes `siteUri` into `CrawlRunner`; when scoping is enabled also queues `siteUri` directly so the scope root is always crawled (the base-root seed `AddRequest(baseUri)` and sitemap seeds fall outside the scope by design).
- `src/InfinityCrawler/Internal/CrawlRunner.cs` — optional `startingUri` constructor parameter; `PathScope` derivation (`GetPathScope`); `IsInPathScope` boundary logic; scope check inserted in private `AddRequest` after the host/alias check.
- `tests/InfinityCrawler.Tests/PathScopedCrawlingTests.cs` — new test class (11 tests).
- `tests/InfinityCrawler.Tests.TestSite/Startup.cs` — added `UseDefaultFiles` (directory URLs like `/ducks/` now serve the directory's `index.html`), reusing the existing `PhysicalFileProvider`.
- `tests/InfinityCrawler.Tests.TestSite/Controllers/HelperController.cs` — two fixture redirect endpoints: `/ducks/redirect-to-destination` (in-scope) and `/ducks/redirect-to-geese` (out-of-scope).
- `tests/InfinityCrawler.Tests.TestSite/InfinityCrawler.Tests.TestSite.csproj` — Resource glob `Resources\*\*.*` → `Resources\**\*.*` so nested fixture folders are copied to output.
- `tests/InfinityCrawler.Tests.TestSite/Resources/PathScopedSite/` — new fixture tree: `robots.txt`, root `index.html`, `ducks/` (index, mallard, query-page, redirect-destination, species/wood-duck), `geese/`, `ducklings/`, `blog/ducks/`.

## Design Decisions

1. **Scope semantics** = the start URI's path normalized to a trailing slash (a subtree rooted at that path). This satisfies the requirement that `/ducks` and `/ducks/` behave identically, matches the task's `https://example.com/ducks/` example, and gives a clean segment-boundary prefix test (no `ducklings` false matches).
2. **Central enforcement** in the private `AddRequest(Uri, bool)` rather than in each caller (`AddLink`, `AddRedirect`, sitemap seeding, retry). Covers every URI source in one place (requirement 3) and keeps the diff small.
3. **Query relevance**: the membership test uses `requestUri.AbsolutePath` (query excluded); the request itself retains the query, so `?sort=name` works (requirement 8).
4. **Scope root is explicitly seeded**: when scoping is on, `Crawler.Crawl` queues `siteUri` in addition to the existing base-root and sitemap seeds. The base root (`/`) and sitemap entries are legitimately out of scope, so without this the crawl would have no starting point.
5. **Host rules untouched**: scope check runs after the host/`HostAliases` check and applies to whichever host was admitted (same-host semantics; `test-domain.com` alias paths follow the same path scope).

## Validation Performed

- `dotnet build InfinityCrawler.sln` — Build succeeded, 0 errors. Only pre-existing warnings (`NETSDK1138` net6.0-out-of-support; `NU1504` duplicate package refs in TestSite netstandard2.0); the `NU1504` duplicate is pre-existing in `InfinityCrawler.Tests.TestSite.csproj`.
- `dotnet test tests/InfinityCrawler.Tests -f net6.0` — **37/37 passed** (26 pre-existing + 11 new).
- Regression extras: `dotnet test -f net5.0` and `-f netcoreapp3.1` also **37/37 passed**. (net461 intentionally not gated per instructions — documented pre-existing assembly-binding failure.)

## Tests Added

`PathScopedCrawlingTests` (uses `CrawlerTestBase.GetTestSiteCrawler` / `GetNoDelayRequestProcessorOptions`):

- `DefaultBehaviourRemainsUnrestricted` — scoping off crawls `/geese/index.html`.
- `InScopeChildLinksAreCrawled` — `/ducks/mallard.html` crawled.
- `NestedDescendantsAreCrawled` — `/ducks/species/wood-duck.html` crawled.
- `SameHostOutOfScopeLinksAreNotCrawled` — `/geese/`, `/blog/ducks/`, and `/` rejected.
- `PrefixConfusionPathsAreRejected` — `/ducklings/` rejected for `/ducks/` scope.
- `SitemapUrlsOutsideScopeAreRejected` — sitemap seed `http://localhost/index.html` not crawled.
- `QueryStringsOnInScopePathsAreAllowed` — `/ducks/query-page.html?sort=name` crawled.
- `RedirectsToInScopePathsAreFollowed` — `/ducks/redirect-destination.html` crawled with redirect chain.
- `RedirectsToOutOfScopePathsAreNotFollowed` — `/geese/index.html` never requested.
- `HostRestrictionsRemainIntact` — `http://not-allowed-domain.com/` appears as a link but is not crawled.
- `BareAndTrailingSlashScopesAreHandledConsistently` — start `/ducks` behaves like `/ducks/`.

## Unresolved Issues

None.

## Deviations From Instructions

None in library behaviour. Two test-infrastructure changes were required to build realistic fixtures:

1. `UseDefaultFiles` added to the test server so directory URLs (`/ducks/`) serve their `index.html`. This is the canonical form of the task's example start URL; without it the start page 404s.
2. TestSite csproj fixture glob changed from `Resources\*\*.*` to `Resources\**\*.*`. Verified that nested fixture files were otherwise **not copied** to the test output (the old glob only matched files directly under a site folder), which would make `PathScopedSite` unservable.

## Relevant Errors Or Blockers

- First implementation attempt failed 8 new tests: `dotnet test -f net6.0` showed 8 failures (all positive-path assertions). Root cause: nested fixture files under `PathsScopedSite/ducks/...` were absent from the test output because of the shallow `Resources\*\*.*` glob — `http://localhost/ducks/` returned 404. Fixed by the glob change; re-verified with a temporary `DebugProbe` test (removed) that returned `200` with the expected HTML.

## Information Needed For the Next Decision Or Task

- No follow-up work is outstanding. If stricter semantics are ever wanted (e.g., a literal/leaf-file scope instead of a directory subtree), the derivation is isolated in `CrawlRunner.GetPathScope`.
- The pre-existing net461 runtime failure and the `NU1504` duplicate-package warning are unchanged and remain related to the upstream repo, not this feature.