# Report: Stage 07 Plain URL Output File and Usage Documentation

Task: `instructions/07-plain-url-output-and-usage-docs.md`
Date: 2026-09-08
Status: Complete

## Objective Recap

1. Added the `--output <file>` CLI option that writes the discovered URL inventory to a plain text file containing only bare URLs, one per line.
2. Created `docs/usage.md`, a detailed user-focused manual for the finished utility.
3. Everything else (crawler, output model, packaging, architecture, command structure) was left untouched.

## Files Changed

Implementation (all under `src/InfinityCrawler.Cli/`):

- `CliArguments.cs` — added `OutputPath`; `ForRun` now carries the output path.
- `CliArgumentParser.cs` — parses `--output <file>` and `--output=<file>`; new errors: missing value, empty value (`--output=` and empty space-form value), repeated `--output` (both forms).
- `CrawlResultFormatter.cs` — entry preparation factored into a private `PrepareEntries` pipeline; `FormatResultLines` and the new `FormatUrlLines` both consume it, so status output and bare-URL output cannot silently diverge.
- `UrlFileWriter.cs` (new) — writes bare URLs one per line; overwrite (never append), creates missing parent directories, UTF-8 without BOM, final newline after the last URL, empty inventory produces an empty file; rejects null/empty path with a clear exception.
- `Program.cs` — help/usage text updated (`[--output <file>]` added); after a successful crawl, console output prints unchanged and the URL file (if requested) is written afterwards; file-write failures print `Failed to write the output file: <message>` to stderr and exit `2` — distinct from the crawl-failure message.

Tests (all under `tests/InfinityCrawler.Tests.Cli/`):

- `CliArgumentParserTests.cs` — 9 new parser tests; existing `ValidAbsoluteHttpUrlAccepted` extended to assert `OutputPath` stays null without `--output`.
- `CrawlResultFormatterTests.cs` — 4 new formatter tests (bare URLs only, parity with status-line inventory after prefix removal, redirect sources enumerated, fallback labels omitted but URLs kept).
- `UrlFileWriterTests.cs` (new) — 7 filesystem tests (one-per-line + no BOM + no decorations, missing parent directory created, overwrite without appending, empty set → empty file, relative path resolves from current directory, null-URL and null/empty-path rejection).
- `UrlCrawlRunnerTests.cs` — new integration test `OutputFileMatchesConsoleInventory`: full scoped crawl against the in-process test site, file written, verified equal to the console inventory and to the formatter URL lines, redirect source/destination present, no scope leakage, no brackets.

Documentation:

- `docs/usage.md` (new) — 14-section practical user manual.
- `README.md` — Windows CLI section now lists `--output` and links to `docs/usage.md` (concise, per spec).

## Parser / Interface Changes

- New syntax: `InfinityCrawler.Cli <url> [--max-pages <number>] [--output <file>]`.
- `--output` (space form) and `--output=<file>` (equals form) both accepted; option order remains flexible (`--output` before or after the URL, combinable with `--max-pages`).
- Parser errors added: missing value for `--output`; empty `--output=` (and empty space-form value); `--output` specified more than once (explicit rejection for deterministic behavior, mirroring the parser's existing error style for ambiguous input such as repeated positional URLs).
- Help text documents the option: `--output <file>      Write discovered URLs to a plain text file, one URL per line.`
- No aliases were added (no existing convention suggested any).

## Output-File Behavior — Exact Plain-File Semantics

- Contains only bare URLs, exactly one per line; verified in tests and in the real-world run: no status codes, no square brackets, no headers, no labels, no timestamps, no comments, no blank lines, no prose, no diagnostics.
- Same final discovered URL inventory as the console output: identical deduplication and ordinal ordering, including redirect-source URLs, because both outputs are produced from the single factored `PrepareEntries` pipeline (no second discovery algorithm exists).
- Encoding: UTF-8 without BOM (byte-verified). Line separator is the platform newline (`CRLF` on Windows); a final newline follows the last URL.
- Overwrites an existing file; never appends.
- Accepts relative and absolute paths; relative paths resolve from the process current working directory (verified by test).
- Missing parent directories are created when possible (`Directory.CreateDirectory` on the resolved parent).
- Write failures produce a clear stderr message (`Failed to write the output file: ...`) and exit code `2`; the process never claims success (exit `0`) when the file could not be written.

## Exit-Code Decision

File-write failures reuse the existing crawl/runtime failure exit code `2`, per the spec's preference for preserving the simple exit-code model. The stderr message distinguishes write failures from crawl failures (`Failed to write the output file: ...` vs `The crawl failed: ...`). Documented as such in `docs/usage.md` §11. Verified in the real world: `--output Z:\missing-drive\urls.txt` → exit `2`, message `Could not find a part of the path 'Z:\missing-drive'.`

## Tests Added / Updated

21 new tests (Tests.Cli went from 28 to 49 tests):

- Parser: accepts `--output urls.txt`; accepts `--output=urls.txt`; accepts option-before-URL; rejects missing value; rejects empty `--output=`; rejects empty space-form value; rejects repeated space-form; rejects repeated mixed forms; combines with `--max-pages`.
- Formatter: URL lines contain only bare URLs (no `[`/`]`); URL lines equal the status-line inventory after prefix removal (same order/dedup); redirect sources enumerated in URL lines; fallback labels omitted but URLs kept.
- Writer: BOM-free one-per-line content; parent directory creation; overwrite; empty file for empty inventory; relative-path resolution; null URL sequence rejected; null/empty path rejected.
- Integration: test-site crawl → output file equals console inventory (including redirect source/destination), no leakage, no brackets.

## Build / Test Results

- `dotnet build InfinityCrawler.sln`: **success, 0 errors** (only pre-existing warnings: NETSDK1138 for net6.0, NU1504 duplicate PackageReference in TestSite).
- `dotnet test tests/InfinityCrawler.Tests -f net6.0`: **37/37 passed**.
- `dotnet test tests/InfinityCrawler.Tests.Cli`: **49/49 passed** (28 pre-existing + 21 new).
- net461 matrix (pre-existing binding issue) intentionally not run — out of scope per instruction.

## Real-World Verification

Published the updated binary first (`.\publish-win-x64.ps1` → success), so validation exercised the new code:

- `--help` smoke: exit 0, usage now shows `[--output <file>]` and the option description.
- Primary run: `InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output TestResults\real-world\python-howto-urls.txt`
  - Exit code: **0**; elapsed **7.82 s**.
  - Console: **28** `[status] URL` lines (statuses still present, behavior unchanged); stderr empty.
  - URL file: **28** lines, 1,395 bytes.
  - File hygiene: 0 lines not starting with `https://docs.python.org/`; 0 lines containing `[` or `]`; 0 blank lines; **no BOM**; ends with newline.
  - Parity: console inventory after status-prefix removal **exactly equals** the file's lines.
  - Scope: **0 cross-host**, **0 out-of-scope** URLs (all within `/3/howto` subtree semantics).
  - Known pages: all 11 live-known HOWTO pages present (argparse, descriptor, enum, functional, logging, logging-cookbook, regex, sorting, unicode, urllib2, sockets).
- Backward compatibility (real world): `... --max-pages 3` without `--output` → exit 0, status lines only, no file written.
- Combination (real world): `--max-pages 3 --output` → exit 0, 3 console lines + 3 file lines.
- Write failure (real world): unwritable target → exit 2, clear stderr message.
- Evidence (uncommitted): `TestResults\real-world\python-howto-output-stage07.txt`, `python-howto-stderr-stage07.txt`, `python-howto-urls.txt`.

## docs/usage.md Creation Confirmation

Created `docs/usage.md` with all 14 required sections: what the tool does; executable location/publishing; basic usage; syntax; options (`<url>`, `--max-pages`, `--output`, `--help`/`-h`); console output (status tokens, fallback labels, redirect sources); plain URL text file (bare URLs only, overwrite, relative/absolute paths, missing-directory creation, UTF-8 no BOM, write-failure behavior); combining options; path scope rules (subtree semantics, sibling rejection, prefix confusion, query strings, fragments, cross-host drops); redirects and statuses; exit codes (0/1/2); redirection and piping (status lines on stdout vs `--output` for bare URLs); practical examples (HOWTO, bounded, relative file, absolute Windows path); important limitations (discovery-based, robots respected, no JS-rendered links, no auth, Windows x64). No unimplemented features are claimed in the document.

## README Changes

The README Windows CLI "Options" list gained the `--output` bullet and a single pointer line to `docs/usage.md`. Nothing else in the README changed.

## Unresolved Issues

- None for this stage.
- Untracked, non-project tooling artifact `PowerShellEditorServices.json` appeared in the repo root during the session (created by PowerShell tooling, not by this work). Left untouched as out of scope; harmless for review but can be deleted or git-ignored at Sean's discretion.
- Pre-existing net461 test-runner binding issue and EOL-framework warnings remain documented, out of scope, untouched.

## Deviations

- None from the instruction. The only discretionary choice was explicitly rejecting a repeated `--output` (rather than last-wins) for deterministic behavior — covered under parser changes above and aligned with the "explicit handling of repeated/malformed use" allowance.

## Errors / Blockers

- One transient test failure during development: `UrlFileWriterTests.ExistingFileIsOverwrittenWithoutAppending` failed with `DirectoryNotFoundException` because the test's `[TestInitialize]` built the temp path without creating the directory (only `UrlFileWriter` itself creates parent directories). Fixed by creating the directory in `[TestInitialize]`; full suite then passed 49/49. The production code was correct; no implementation change was required.
- No blockers.

## Git Status Summary

- Branch `main`, HEAD `e257ce0bb6c3825f6b5511da84568873206b6c36` (unchanged — nothing committed, per the instruction).
- Modified (tracked): `README.md`, `src/InfinityCrawler.Cli/{CliArgumentParser,CliArguments,CrawlResultFormatter,Program}.cs`, `tests/InfinityCrawler.Tests.Cli/{CliArgumentParserTests,CrawlResultFormatterTests,UrlCrawlRunnerTests}.cs` — 8 files, +329/−14 lines.
- New (untracked): `docs/usage.md`, `src/InfinityCrawler.Cli/UrlFileWriter.cs`, `tests/InfinityCrawler.Tests.Cli/UrlFileWriterTests.cs`.
- Untracked as before: `instructions/`, `reports/04…06`, plus the tooling artifact noted above. `artifacts/`, `TestResults/`, `bin/`, `obj/` remain Git-ignored.
- No push, no tag, no release, no history rewrite, no remote changes. Implementation left ready for review.

## Per-Step SAST Timestamps

- 18:25 — instructions (00, 07), AGENTS.md, CONTEXT.md, and reports/06 re-read; CLI sources, tests, publish script, `.gitignore`, and README reviewed; plan set.
- 18:26 — implementation complete: `CliArguments`, `CliArgumentParser` (both option forms + errors), `CrawlResultFormatter` (factored `PrepareEntries` + `FormatUrlLines`), new `UrlFileWriter`, `Program` (help text + write + exit-code semantics).
- 18:27 — tests written: 9 parser tests (+1 extended), 4 formatter tests, 7 writer tests, 1 integration test.
- 18:28 — `dotnet build InfinityCrawler.sln`: success, 0 errors; `dotnet test tests/InfinityCrawler.Tests -f net6.0`: 37/37 passed.
- 18:29 — Tests.Cli: 49/49 passed (after fixing the test-initialization temp-directory bug noted above).
- 18:30 — `.\publish-win-x64.ps1` succeeded; `--help` of the published EXE verified with the new option.
- 18:31 — real-world run complete: exit 0, 7.82 s, 28 console lines / 28 file lines; hygiene, parity, scope, and known-page checks all passed.
- 18:32 — backward-compat, combination, and write-failure smoke checks passed (exit 2 semantics verified).
- 18:33 — `docs/usage.md` created (14 sections); README updated; Git status collected.
- 18:34 — this report written.
