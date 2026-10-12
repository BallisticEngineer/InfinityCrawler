# Report: Stage 06 Real-World End-to-End Validation

Task: `instructions/06-real-world-end-to-end-validation.md`
Date: 2026-09-08
Status: Complete — **PASS**

## Classification Against the Original End Goal

**PASS.** The published utility was given a real starting URL (`https://docs.python.org/3/howto/`) and returned the discoverable URL inventory beneath that path: 28 real pages, all in-scope, all same-host, all HTTP 200, with zero scope leakage and zero cross-host URLs. The original user outcome — give a starting URL and receive the URL inventory beneath that path without wandering into unrelated same-host paths — is proven against a live public documentation subtree. No functional or usability defect was found; no fix is required.

## Exact Executable Path

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\artifacts\publish\win-x64\InfinityCrawler.Cli.exe`

- Verified present before the run: 63,315,377 bytes, self-contained single-file build (LastWrite 2026-09-08 18:31:31; republished after Stage 07 — `--help` shows the `--output` option).
- The published executable was used throughout; `dotnet run` was not used at any point.

## Exact Command Used

Primary validation run (stdout/stderr captured to the specified files):

```
Start-Process -FilePath "G:\...\artifacts\publish\win-x64\InfinityCrawler.Cli.exe" `
              -ArgumentList "https://docs.python.org/3/howto/" `
              -RedirectStandardOutput "TestResults\real-world\python-howto-output.txt" `
              -RedirectStandardError  "TestResults\real-world\python-howto-stderr.txt" `
              -Wait
```

No `--max-pages` was supplied — the complete discoverable subtree was requested, per the instruction.

Supporting runs:

```
& "<exe>" --help          → exit 0, usage text rendered
```

## Target URL

`https://docs.python.org/3/howto/`

Context check: `https://docs.python.org/robots.txt` disallows only `/dev`, `/release`, and EOL trees (`/2.x`, `/3.0`–`/3.9`); `/3/howto/` is fully crawlable with no crawl-delay, so robots handling did not artificially shrink the run.

## Exit Code

`0`

## Elapsed Time

`00:00:09.69` (9.69 seconds) for the complete crawl, including robots.txt fetch, sitemap.xml fetch (out-of-scope sitemap seeds rejected by the path filter), and all 28 page requests.

## Emitted URL Count

- 28 URL lines on stdout; unique = 28 (no duplicates, no fragment variants).
- stderr was empty (0 bytes).

## Representative In-Scope Output

Full stdout (28 lines):

```
[200] https://docs.python.org/3/howto/
[200] https://docs.python.org/3/howto/a-conceptual-overview-of-asyncio.html
[200] https://docs.python.org/3/howto/annotations.html
[200] https://docs.python.org/3/howto/argparse-optparse.html
[200] https://docs.python.org/3/howto/argparse.html
[200] https://docs.python.org/3/howto/cporting.html
[200] https://docs.python.org/3/howto/curses.html
[200] https://docs.python.org/3/howto/descriptor.html
[200] https://docs.python.org/3/howto/enum.html
[200] https://docs.python.org/3/howto/free-threading-extensions.html
[200] https://docs.python.org/3/howto/free-threading-python.html
[200] https://docs.python.org/3/howto/functional.html
[200] https://docs.python.org/3/howto/gdb_helpers.html
[200] https://docs.python.org/3/howto/index.html
[200] https://docs.python.org/3/howto/instrumentation.html
[200] https://docs.python.org/3/howto/ipaddress.html
[200] https://docs.python.org/3/howto/isolating-extensions.html
[200] https://docs.python.org/3/howto/logging-cookbook.html
[200] https://docs.python.org/3/howto/logging.html
[200] https://docs.python.org/3/howto/mro.html
[200] https://docs.python.org/3/howto/perf_profiling.html
[200] https://docs.python.org/3/howto/regex.html
[200] https://docs.python.org/3/howto/remote_debugging.html
[200] https://docs.python.org/3/howto/sockets.html
[200] https://docs.python.org/3/howto/sorting.html
[200] https://docs.python.org/3/howto/timerfd.html
[200] https://docs.python.org/3/howto/unicode.html
[200] https://docs.python.org/3/howto/urllib2.html
```

## Explicit Scope-Leak Checks

Independent baseline (evidence: `TestResults\real-world\python-howto-index.html`, HTTP 200, 19,167 bytes, retrieved fresh this run): every `<a href>` on the live starting page resolved against the page URI into:

- **26 distinct in-scope** `/3/howto/` targets (27 link instances including `#fragment` variants),
- **9 distinct out-of-scope same-host targets**: `/3/bugs.html`, `/3/copyright.html`, `/3/genindex.html`, `/3/improve-page-nojs.html`, `/3/index.html`, `/3/installing/index.html`, `/3/py-modindex.html`, `/bugs.html`, `/license.html`,
- **4 cross-host targets**.

The live page therefore genuinely tempts the crawler away from scope (9 same-host nav links plus cross-host links), so the zero out-of-scope emission in the tool's output is meaningful, not accidental.

Checks against the crawl output:

- Cross-host URLs emitted: **0** (every emitted URL is `https://docs.python.org/...`; host check `host_bad_count=0`).
- Same-host URLs outside the `/3/howto` subtree: **0** — none of `/3/tutorial/`, `/3/library/`, `/3/reference/`, `/3/genindex.html`, or any other same-host path outside `/3/howto/` was emitted (`path_bad_count=0`), despite 9 such live links on the start page and far more reachable from crawled pages.
- Query-string or fragment-bearing variants emitted: **0** (fragments stripped by design; site served canonical URLs).

Coverage completeness (diff of index-linked in-scope targets vs the 28 emitted):

- Index-linked in-scope targets absent from output: **0** — every one of the 26 distinct in-scope targets appears in the output (with fragments stripped), i.e. nothing obviously discoverable was missed.
- Emitted but not index-linked directly: `argparse-optparse.html` (reached via a cross-page link, e.g. inside `argparse.html`, demonstrating in-subtree link following) and `index.html` (canonical resource behind the start URL `/3/howto/`).

## Explicit Known-Page Presence Checks

All live-known HOWTO pages named by the instruction are present with HTTP 200:

| Page | Present |
|---|---|
| `argparse.html` | Yes |
| `descriptor.html` | Yes |
| `enum.html` | Yes |
| `functional.html` | Yes |
| `logging.html` | Yes |
| `logging-cookbook.html` | Yes |
| `regex.html` | Yes |
| `sorting.html` | Yes |
| `unicode.html` | Yes |
| `urllib2.html` | Yes |
| `sockets.html` | Yes |

`urllib2.html` (previously removed from some doc trees) is confirmed live on the current site with HTTP 200.

## Redirect/Status Observations

- Status token grouping across all 28 emitted lines: `200: 28`. No other status token appeared; stderr empty.
- Zero redirect-source entries were emitted because zero redirects were encountered — docs.python.org serves canonical URLs directly (200) with no 301/302/307 hops in this subtree. This is an observation, not a gap; redirect-chain rendering remains covered by `tests/InfinityCrawler.Tests.Cli`.
- No robots-blocked entries: consistent with robots.txt (nothing under `/3/howto/` disallowed).

## Practical Usability Assessment

The intended invocation style works as-is:

- Self-contained executable; no .NET runtime, no config, single URL argument.
- Fast run (9.7 s) with the default request processor; no hang or stall.
- Output is directly usable as a URL inventory: one `[status] URL` line per URL, nothing else on stdout; stderr silent; trivially pipeable, sortable, and redirectable.
- `--help` (exit 0) documents the interface; since Stage 07 the same exe additionally offers `--output <file>` for a bare-URL inventory file (not required here).

Minor, non-blocking friction: the executable lives under `artifacts\publish\win-x64\`, so repeated use requires the full path or copying the exe. This matches the Stage 03 generated-artifact design (ignored by Git, never committed) and does not undermine the goal; no packaging change is warranted.

## Evidence Files

- `TestResults\real-world\python-howto-output.txt` — complete stdout of the primary run (28 lines, 1,563 bytes)
- `TestResults\real-world\python-howto-stderr.txt` — stderr of the primary run (0 bytes)
- `TestResults\real-world\python-howto-index.html` — independently retrieved live starting page used as the scope baseline (19,167 bytes)

All remain uncommitted by design (`TestResults/` is Git-ignored; validation-only stage).

## Source Control

No commit, push, tag, release, or history modification was performed. No source, test, packaging, or documentation files were changed. Git working tree state is unchanged apart from the already-untracked `instructions/`, `reports/`, and the (ignored) `TestResults\real-world\` evidence.

## Unresolved Issues

- None functional. The utility met the original user goal against a real, live target.
- Non-issue noted for completeness: redirect-chain rendering was not exercised by this target because the site emits no redirects in scope; coverage for that path exists in the CLI test suite.
- The previously untracked `reports\06-real-world-end-to-end-validation.md` from an earlier run was replaced by this fresh-run report; both runs observed the same 28-page in-scope inventory. The documented pre-existing net461 test-runner assembly-binding issue (AGENTS.md) remains out of scope and untouched.

## Blockers

- None.

## Per-Step SAST Timestamps

- 21:54 — instructions (00, 06), AGENTS.md, and CONTEXT.md read and confirmed; pre-flight complete: published EXE verified (63,315,377 bytes), `TestResults\real-world\` present.
- 21:54 — `--help` smoke test passed: exit 0, usage text (incl. `--output` from the Stage 07 republish) rendered.
- 21:55 — primary crawl complete: exit 0, 9.69 s elapsed, stdout 28 URL lines, stderr empty.
- 21:56 — output analysis complete: status grouping `200: 28`, unique 28, cross-host 0, out-of-scope 0, all 11 known HOWTO pages present.
- 21:57 — live index baseline resolved (26 distinct in-scope, 9 distinct out-of-scope same-host, 4 cross-host); index-vs-crawl coverage diff complete (0 missing, 2 extra in-scope via canonical/cross-page links).
- 21:58 — PASS confirmed against all success criteria; evidence files finalized; this report written.