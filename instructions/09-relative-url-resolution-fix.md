# InfinityCrawler Stage 09: Fix Browser-Equivalent Relative URL Resolution

## Objective

Fix the real-world URL resolution defect observed by Sean on:

`https://erotom.com/post_64996/`

The crawler currently emits malformed nested URLs such as:

`https://erotom.com/post_64996/post_65113`

when the real target link should resolve to:

`https://erotom.com/post_65113`

This is a functional defect. The project is reopened until this real-world case is corrected and validated.

## Required Workflow

Read and follow:

- `instructions/00-agent-workflow.md`
- `AGENTS.md`
- `G:\My Drive\Arden\InfinityCrawler\CONTEXT.md`
- current crawler/link extraction and URI resolution code
- current path-scoping tests
- current CLI tests
- `docs/usage.md`

The mandatory per-step SAST timestamp requirement applies throughout the run.

## Reproduction

Use the published utility against:

`https://erotom.com/post_64996/`

Observed bad output includes examples like:

- `https://erotom.com/post_64996/post_65113`
- `https://erotom.com/post_64996/post_65172`
- `https://erotom.com/post_64996/post_65445`

These are not valid destination URLs for the site. The corresponding destination form is root-level, e.g.:

- `https://erotom.com/post_65113`
- `https://erotom.com/post_65172`
- `https://erotom.com/post_65445`

Do not assume this is merely a path-scoping issue. Diagnose the exact HTML/link form and the current URI resolution behavior first.

## Required Diagnosis

1. Inspect the actual HTML returned by the target page and identify the exact `href` values producing the malformed URLs.
2. Determine how a real browser resolves those links. Account for:
   - document URL semantics
   - trailing slash behavior
   - any `<base href>` element
   - redirects/canonical response URL if relevant
   - root-relative vs path-relative links
3. Locate the code path that converts extracted `href` values into absolute `Uri` instances.
4. Prove why the current implementation yields `/post_64996/post_65113` instead of `/post_65113`.
5. Preserve existing path-scope filtering semantics, but ensure filtering happens on correctly resolved absolute URLs.

Do not apply a site-specific string hack for `erotom.com` or `post_`. The fix must implement correct general URL resolution behavior.

## Implementation Requirements

- Implement browser-equivalent absolute URL resolution for discovered links.
- Respect `<base href>` when present.
- Use the effective/final document URL after redirects as the resolution base when appropriate.
- Preserve correct handling of:
  - absolute URLs
  - scheme-relative URLs
  - root-relative URLs
  - path-relative URLs
  - query-only links
  - fragment-only links
- Preserve fragment stripping/dedup behavior.
- Preserve host restrictions and path-scope admission after resolution.
- Do not weaken path scoping to make this target pass.
- Do not redesign unrelated crawler architecture.

## Regression Tests

Add focused tests that reproduce the failure shape. At minimum cover:

1. A start/document URL ending in `/post_64996/` with a link form matching the live site, proving the result is `https://example.test/post_65113` rather than `https://example.test/post_64996/post_65113`.
2. Normal path-relative links that genuinely should remain nested still resolve correctly.
3. Root-relative links remain root-relative.
4. `<base href>` behavior if the live page uses it.
5. Redirected document URL behavior if relevant to the root cause.
6. Existing path-scoping tests continue to pass.

Use the in-process test site where practical.

## Validation

Run:

```powershell
dotnet build InfinityCrawler.sln
dotnet test tests/InfinityCrawler.Tests -f net6.0
dotnet test tests/InfinityCrawler.Tests.Cli
```

Then republish the Windows executable:

```powershell
.\publish-win-x64.ps1
```

Run the published executable against the exact real target:

```powershell
& '.\artifacts\publish\win-x64\InfinityCrawler.Cli.exe' 'https://erotom.com/post_64996/' --output TestResults\real-world\erotom-urls.txt
```

Capture stdout/stderr and verify:

- no malformed nested `.../post_64996/post_*` URLs are emitted
- expected root-level `https://erotom.com/post_*` URLs are emitted when present in the page
- output URLs are actually requestable using the same URL form the site exposes
- no regression in deduplication, statuses, redirect handling, or output-file parity
- scope semantics are still correct after proper URL resolution

If live anti-bot/rate-limit behavior prevents full validation, still prove URL construction correctness from the fetched HTML and report the blocker clearly. Do not classify success solely from unit tests.

## Documentation

Update `docs/usage.md` only if the fix changes or clarifies user-visible behavior/limitations. Do not add implementation trivia to the user guide.

## Source Control

Do not commit or push in this stage unless Sean explicitly authorizes it after review. Leave the correction ready for review.

## Required Report

Write:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\reports\09-relative-url-resolution-fix.md`

Include:

- exact root cause
- relevant live HTML/link evidence
- browser-equivalent expected resolution
- files changed
- tests added/updated
- build/test results
- real-world validation command and results
- examples of bad-before vs correct-after URLs
- any anti-bot/rate-limit complications
- unresolved issues
- deviations
- blockers
- Git status
- per-step SAST timestamps

## Acceptance Criteria

Stage 09 passes only when the malformed nesting defect is eliminated by a general URL-resolution fix, regression tests cover it, existing tests remain green, and the exact real-world target demonstrates correct URL construction with no `.../post_64996/post_*` output.
