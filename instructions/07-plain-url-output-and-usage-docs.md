# InfinityCrawler Stage 07: Plain URL Output File and Usage Documentation

## Objective

Add one small practical CLI feature and document the finished utility properly.

1. Add an optional CLI argument that writes the discovered URL inventory to a plain text file containing only URLs, one URL per line.
2. Create `docs/usage.md` with detailed, user-focused usage instructions for the finished `InfinityCrawler.Cli` utility.

This stage must remain tightly scoped. Do not redesign the crawler, output model, packaging, architecture, or command structure beyond what is necessary for this feature.

## Required Workflow

Read and follow:

- `instructions/00-agent-workflow.md`
- `AGENTS.md`
- `G:\My Drive\Arden\InfinityCrawler\CONTEXT.md`
- existing CLI source and tests
- existing `README.md`
- `reports/06-real-world-end-to-end-validation.md`

The mandatory per-step SAST timestamp requirement applies throughout the run.

## Current State

The current CLI supports:

- required absolute HTTP/HTTPS start URL
- `--max-pages <number>`
- `--help` / `-h`
- default console output in `[status] URL` form
- path-scoped crawling by default
- deterministic URL ordering and deduplication
- redirect-source enumeration

The current CLI does **not** provide a built-in plain-URL text-file output option.

Shell redirection alone is not equivalent because stdout contains status prefixes such as `[200]`.

## Feature Requirement: `--output <file>`

Add an optional output-file argument:

`--output <file>`

Also support:

`--output=<file>`

Do not add additional aliases unless there is a strong existing convention in the codebase.

### Behavior

Example:

`InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt`

The crawl should continue to produce the existing normal console output unchanged.

In addition, `urls.txt` must contain only discovered URLs, exactly one per line:

```text
https://docs.python.org/3/howto/
https://docs.python.org/3/howto/argparse.html
https://docs.python.org/3/howto/descriptor.html
```

The file must contain:

- no status codes
- no square brackets
- no headers
- no labels
- no timestamps
- no comments
- no blank lines between URLs
- no explanatory prose
- no diagnostic text

A final newline after the last URL is acceptable.

### URL Set and Ordering

The URL-only file must represent the same final discovered URL inventory used by the CLI formatter, including redirect-source URLs where applicable.

Requirements:

- same deduplication semantics as the normal CLI output
- same deterministic ordinal URI ordering
- one line per unique URL
- no status/filtering differences between console inventory and file inventory, except that the file omits the status/fallback token

Do not implement a second independent crawl/result-discovery algorithm.

Prefer factoring/reusing the existing result-entry preparation so status console output and bare-URL output cannot silently diverge.

### File Semantics

- Accept relative or absolute file paths.
- Relative paths resolve from the process current working directory.
- Overwrite an existing file of the same path.
- Do not append.
- If the parent directory does not exist, create it when practical.
- Use UTF-8 text without adding a BOM if the target framework/runtime API makes that straightforward.
- File write failures must produce a clear stderr message and a non-zero exit code.
- Do not partially claim success if the requested output file could not be written.

### Backward Compatibility

Without `--output`, existing CLI behavior must remain unchanged.

Existing invocation must still work:

`InfinityCrawler.Cli.exe https://docs.python.org/3/howto/`

Existing `--max-pages` behavior must remain unchanged and be combinable with `--output`:

`InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --max-pages 10 --output urls.txt`

Option order should remain flexible under the existing parser style.

## Parser and Help Requirements

Update the CLI parser and help text.

The help should clearly document:

`--output <file>       Write discovered URLs to a plain text file, one URL per line.`

Exact spacing may follow existing formatting.

Parser errors must cover at minimum:

- `--output` with no following value
- empty `--output=` value
- repeated/otherwise malformed use if the current parser architecture needs explicit handling

Do not over-engineer path validation. Actual filesystem errors can be handled when writing.

## Tests

Add focused tests covering the new behavior.

At minimum verify:

1. parser accepts `--output urls.txt`
2. parser accepts `--output=urls.txt`
3. parser rejects missing output value
4. parser rejects empty `--output=`
5. output argument combines with `--max-pages`
6. plain URL file/output formatter contains only URLs, one per line
7. no `[200]`, fallback labels, headers, or extra characters appear in bare-URL output
8. ordering/dedup matches the normal result inventory
9. redirect-source URLs are represented consistently in bare-URL output where redirect-chain test fixtures exist
10. existing parser/formatter/integration tests remain green
11. existing CLI behavior without `--output` remains unchanged

Prefer testing a factored formatter/writer rather than depending entirely on filesystem integration, but include at least one integration-level file-write validation if practical.

## Real-World Verification

After tests pass, publish or use the existing published executable as appropriate so the validation exercises the updated CLI binary.

Run against:

`https://docs.python.org/3/howto/`

with an output file, for example:

`InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output TestResults\real-world\python-howto-urls.txt`

Verify:

- exit 0
- normal console output still contains statuses
- URL file exists
- URL file contains the same discovered URL set as console output after status-prefix removal
- every line in the URL file is a URL and nothing else
- no scope leakage
- no cross-host leakage
- expected real HOWTO pages are present

Generated evidence remains uncommitted.

## Documentation: `docs/usage.md`

Create:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\docs\usage.md`

This must be a practical user manual, not an architecture document.

Include:

### 1. What InfinityCrawler.Cli Does

Explain in plain language:

- give it a starting HTTP/HTTPS URL
- it recursively discovers publicly linked URLs
- the CLI restricts discovery to the starting path subtree
- example: `/ducks/` includes `/ducks/mallard/` but excludes `/geese/`
- it is discovery-based, not brute-force hidden-directory enumeration

### 2. Executable Location / Publishing

Document the current Windows x64 published executable path:

`artifacts\publish\win-x64\InfinityCrawler.Cli.exe`

Document publishing from repo root:

`.\publish-win-x64.ps1`

Explain that the published EXE is self-contained and does not require a separate .NET runtime installation.

### 3. Basic Usage

Examples from PowerShell:

`& '.\artifacts\publish\win-x64\InfinityCrawler.Cli.exe' 'https://docs.python.org/3/howto/'`

Also show use when the EXE is copied somewhere convenient / invoked directly by filename.

### 4. Syntax

Document the complete current syntax after this feature:

`InfinityCrawler.Cli <url> [--max-pages <number>] [--output <file>]`

Document `--help` and `-h`.

### 5. Options

Detailed explanation of:

- `<url>`
- `--max-pages`
- `--output`
- `--help` / `-h`

### 6. Console Output

Explain normal `[status] URL` console output with examples and what redirect-source lines mean.

### 7. Plain URL Text File

Explain `--output`.

Make explicit that the file contains **only bare URLs, one per line**.

Show an example file body.

Explain overwrite behavior and relative-vs-absolute path behavior.

### 8. Combining Options

Examples:

- full crawl + output
- bounded crawl + output
- help

### 9. Path Scope Rules

Explain:

- start `/ducks/`
- accepts `/ducks/`, `/ducks/mallard/`, `/ducks/species/a`
- rejects `/geese/`, `/blog/`, `/ducklings/`
- same-host unrelated links are ignored
- cross-host URLs are not emitted
- query strings on otherwise in-scope URLs are allowed
- fragments are deduplicated/stripped according to crawler behavior

### 10. Redirects and Statuses

Explain the normal console output preserves useful status information and redirect-source URLs.

Clarify that `--output` deliberately removes status metadata and writes only the URL inventory.

### 11. Exit Codes

Document:

- `0` success
- `1` invocation/argument error
- `2` crawl/runtime/output-write failure, if implementation keeps the existing crawl-error class for file-write errors

If implementation introduces a different output-write exit code, document the actual final behavior and justify it in the report. Prefer preserving the existing simple exit-code model.

### 12. Redirection and Piping

Explain that normal shell stdout redirection captures `[status] URL` lines.

Explain that `--output` should be used when the user wants a clean bare-URL list.

PowerShell examples are preferred.

### 13. Practical Examples

Include at least:

- Python HOWTO real-world example
- bounded example with `--max-pages`
- bare URL list to a relative file
- bare URL list to an absolute Windows path

### 14. Important Limitations

Keep this concise and factual:

- discovers publicly discoverable links/sitemap/redirect paths, not hidden brute-force paths
- respects crawler/robots behavior
- JavaScript-only links that do not appear in fetched HTML may not be discoverable
- authentication-protected content is not part of the simple CLI workflow unless the underlying crawler is explicitly configured in code
- current published target is Windows x64

Do not invent features that are not implemented.

## README

Update the existing README Windows CLI section only if necessary to mention `--output` and point users to `docs/usage.md`.

Keep README changes concise. The detailed manual belongs in `docs/usage.md`.

## Validation

Run at minimum:

`dotnet build InfinityCrawler.sln`

`dotnet test tests/InfinityCrawler.Tests -f net6.0`

`dotnet test tests/InfinityCrawler.Tests.Cli`

Then perform the real-world `--output` check described above.

Known pre-existing net461 binding problems remain out of scope.

## Source Control

Do not push automatically.

Do not force-push, rebase, reset, amend unrelated history, create tags, or create releases.

Leave the implementation ready for review. Sean/Arden will decide the commit/push step after review.

## Required Report

Write:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\reports\07-plain-url-output-and-usage-docs.md`

Include:

- files changed
- parser/interface changes
- output-file behavior
- exact plain-file semantics
- tests added/updated
- build/test results
- real-world verification result
- `docs/usage.md` creation confirmation
- README changes, if any
- unresolved issues
- deviations
- errors/blockers
- Git status summary
- per-step SAST timestamps

## Acceptance Criteria

Stage 07 passes only if:

- `--output <file>` and `--output=<file>` work
- output file contains only bare URLs, one per line
- URL set/order matches the discovered console inventory
- existing console behavior remains unchanged
- `--max-pages` remains compatible
- parser/help text are correct
- tests pass
- real-world output-file validation passes
- `docs/usage.md` exists and accurately documents the finished CLI
- no unrequested architectural expansion occurred
- required report is written
