# InfinityCrawler.Cli Usage Guide

A practical user manual for the `InfinityCrawler.Cli` command line utility — give it a starting URL and get back every URL it discovers beneath that path.

---

## 1. What InfinityCrawler.Cli Does

`InfinityCrawler.Cli` is a URL inventory tool:

- You give it a single starting HTTP or HTTPS URL.
- It fetches that page, reads the links on it, follows the links that stay inside the starting path, and repeats — recursively.
- It seeds discovery from the site's `sitemap.xml` too, and honors `robots.txt`.
- Discovery is restricted to the **starting path subtree**. Starting at `https://example.com/ducks/` will include `https://example.com/ducks/mallard/` but will never wander into `https://example.com/geese/` — even though pages under `/ducks/` usually link to other site sections.
- It is **discovery-based**: it only reports URLs that are actually linked (or listed in the sitemap). It does not brute-force guess hidden directory names.

The result is a clean list of every URL found under the subtree you pointed it at, with the HTTP status of each.

## 2. Executable Location / Publishing

The utility ships as a self-contained, single-file Windows x64 executable:

```
artifacts\publish\win-x64\InfinityCrawler.Cli.exe
```

It does **not** require any .NET runtime to be installed — everything needed is bundled into the single `.exe`.

If the executable has not been published yet, or you have changed the source, publish it from the repository root:

```powershell
.\publish-win-x64.ps1
```

This produces the executable at `artifacts\publish\win-x64\InfinityCrawler.Cli.exe`. Publishing requires the .NET SDK, but *running* the published executable does not.

## 3. Basic Usage

From the repository root in PowerShell:

```powershell
& '.\artifacts\publish\win-x64\InfinityCrawler.Cli.exe' 'https://docs.python.org/3/howto/'
```

Or copy the executable somewhere convenient (any folder, a USB stick, a tools directory) and run it directly by name:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/
```

The crawl runs, prints one discovered URL per line to the console, and exits.

### Validated against the live Python documentation

The basic invocation was validated end-to-end using the published Windows executable:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/
```

At the time of validation this single command discovered **28 URLs** — the complete reachable `/3/howto/` subtree — and:

- **zero same-host scope leakage**: every emitted URL stayed inside `/3/howto/` (out-of-scope links such as `/3/tutorial/`, `/3/library/`, and `/3/genindex.html` on the crawled pages were excluded);
- **zero cross-host leakage**: every emitted URL was on `docs.python.org`;
- **every emitted entry returned HTTP 200**.

Feeding the same crawl through `--output` writes that exact 28-URL inventory to a file, one bare URL per line:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt
```

## 4. Syntax

The complete command syntax is:

```
InfinityCrawler.Cli <url> [--max-pages <number>] [--output <file>]
```

- `<url>` is required.
- `[--max-pages <number>]` and `[--output <file>]` are optional and may appear in any order.

Ask for help at any time:

```
InfinityCrawler.Cli --help
InfinityCrawler.Cli -h
```

`--help` (or `-h`) prints the usage summary and exits with code `0`.

## 5. Options

### `<url>` (required)

The absolute HTTP or HTTPS URL to start the crawl from. The path part of this URL defines the crawl scope (see [Path Scope Rules](#9-path-scope-rules)).

Examples: `https://example.com/ducks/`, `http://example.org/`

### `--max-pages <number>` (optional)

Caps the total number of pages crawled. Use it to bound long crawls or take a sample of a large site. Must be a positive integer. Without it, the crawler keeps going until the discoverable subtree is exhausted.

### `--output <file>` (optional)

Writes the discovered URL inventory to a plain text file containing **only URLs, one per line** (no status prefixes, no headers). See [Plain URL Text File](#7-plain-url-text-file). Both `--output urls.txt` and `--output=urls.txt` spellings are accepted. The crawl continues to print the normal console output regardless.

### `--help` / `-h`

Prints usage information and exits successfully.

## 6. Console Output

The console output is one line per discovered URL:

```
[200] https://docs.python.org/3/howto/
[200] https://docs.python.org/3/howto/argparse.html
[301] https://example.com/ducks/old.html
[403] https://example.com/ducks/private.html
[ROBOTS] https://example.com/ducks/secret.html
```

- The bracketed token is the final HTTP status code of that URL (`200`, `301`, `403`, ...).
- If a URL was never actually requested — for example it was blocked by `robots.txt`, or the retry/redirect limits were exhausted — a fallback label is shown instead: `[ROBOTS]`, `[MAX-RETRIES]`, or `[MAX-REDIRECTS]`.
- **Redirect sources are enumerated too.** If `/ducks/old.html` redirects to `/ducks/new.html`, both appear: the old URL with the redirect status (e.g. `[301]`) and the new URL with its own final status (`[200]`).
- Lines are sorted by URL (ordinal) and deduplicated — the same URL is never listed twice.

## 7. Plain URL Text File

When you want just the URLs — for feeding into another tool, a spreadsheet, or a download manager — use `--output`:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt
```

The file contains **only bare URLs, exactly one per line** — no status codes, no square brackets, no headers, no labels, no timestamps, no comments, no blank lines, no prose:

```text
https://docs.python.org/3/howto/
https://docs.python.org/3/howto/argparse.html
https://docs.python.org/3/howto/functional.html
https://docs.python.org/3/howto/regex.html
```

The file always represents the **same discovered URL inventory** as the console output (same set, same ordering, same redirect-source handling) — just without the status tokens.

File behavior:

- **Overwrites**: if the file already exists it is replaced, not appended to.
- **Relative or absolute paths**: both work. A relative path (e.g. `urls.txt` or `out\urls.txt`) resolves from the directory the command runs in — the process current working directory.
- **Missing directories are created**: `--output reports\june\urls.txt` creates `reports\june\` automatically if it does not exist.
- **Encoding**: UTF-8 without a byte-order mark (BOM). One URL per line, with a final newline after the last URL.
- If the file cannot be written (bad drive, permission denied, invalid path), the tool prints a clear error to stderr and exits with code `2` — it does not silently claim success.

## 8. Combining Options

Options can be combined freely, in any order:

Full crawl plus a URL file:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt
```

Bounded crawl plus a URL file:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --max-pages 10 --output sample.txt
```

Equivalent `=` spelling:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --max-pages=10 --output=sample.txt
```

Help:

```powershell
InfinityCrawler.Cli.exe --help
```

## 9. Path Scope Rules

The crawl scope is derived from the path of the starting URL, using subtree semantics:

| Starting URL | Accepted (crawled) | Rejected (ignored) |
|---|---|---|
| `https://example.com/ducks/` | `/ducks/`, `/ducks/mallard/`, `/ducks/species/a` | `/geese/`, `/blog/`, `/ducklings/` |

- **Same-host, out-of-scope links are ignored** — a `/ducks/` page linking to `/geese/` does not cause `/geese/` to be crawled or emitted.
- **Cross-host URLs are never emitted.** Links to other domains (and other host names) are dropped.
- **Prefix confusion is avoided**: starting at `/ducks/` does *not* admit `/ducklings/` — the scope is a real subtree, not a string prefix.
- **Query strings are allowed** on otherwise in-scope URLs (`/ducks/page.html?sort=name` stays in scope).
- **Fragments** (`#anchors`) are stripped for deduplication, so `page.html#intro` and `page.html#details` count as one URL.
- The starting URL itself is always included in the crawl, so the scope root is never skipped even if nothing links to it.

### Start URL resolution base

Relative links on a page resolve against that page's URL. When you start at a **trailing-slash** URL such as `https://example.com/post_64996/` (one path segment, no file extension), the crawler checks whether the site also serves the identical page — the same discoverable link structure — at the bare form `https://example.com/post_64996` without a redirect. If it does, relative links from that start page are resolved against the **bare form** instead, which is what a browser viewing the page at either address produces. This avoids malformed nested URLs such as `https://example.com/post_64996/post_65113` when the site truly exposes `https://example.com/post_65113`.

The emitted/crawled starting URL is still the one you supplied (the trailing-slash form); only how the start page's relative links resolve is affected.

## 10. Redirects and Statuses

The normal console output preserves useful status information:

- Each line shows the final HTTP status code of that URL.
- Redirect chains are flattened: every redirect **source** URL appears with its own redirect status (301/302/307), alongside the redirect destination with its final status. Nothing in a chain is silently swallowed.

`--output` deliberately writes only the URL inventory — the status metadata (including redirect-source statuses) is intentionally omitted so the file is a clean list of URLs. If you need per-URL statuses, use the console output; if you need a clean URL list, use `--output`. Run without `--output` to always see both console behavior unchanged.

## 11. Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success — the crawl completed (and the output file, if requested, was written). |
| `1` | Invocation error — bad or missing arguments (unknown option, invalid URL, missing `--output` value, ...). Usage is printed to stderr. |
| `2` | Crawl/runtime failure or output-file write failure — the crawl threw (network failure, DNS failure, ...) or the `--output` file could not be written. The error is described on stderr. |

## 12. Redirection and Piping

Normal shell redirection captures the **status-prefixed** console lines:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ > crawl.log
```

`crawl.log` will contain `[200] https://...` lines. That is useful for record-keeping, but not for tools that expect bare URLs.

When you want a clean bare-URL list, use `--output` instead of hand-stripping brackets:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt

# then, for example:
Get-Content urls.txt | ForEach-Object { Invoke-WebRequest $_ }
```

Keep both if you like — the console output and the URL file are always produced by the same crawl:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt | Tee-Object -FilePath statuses.log
```

## 13. Practical Examples

Python documentation HOWTO subtree (real-world example):

```powershell
& '.\artifacts\publish\win-x64\InfinityCrawler.Cli.exe' 'https://docs.python.org/3/howto/'
```

Bounded sample — first 10 pages only:

```powershell
& '.\artifacts\publish\win-x64\InfinityCrawler.Cli.exe' 'https://docs.python.org/3/howto/' --max-pages 10
```

Bare URL list to a relative file (created in the current directory):

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt
```

Bare URL list to an absolute Windows path:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output D:\reports\howto-urls.txt
```

Everything at once — bounded crawl, status log, and bare URL file:

```powershell
InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --max-pages 25 --output howto-urls.txt 2> crawl-errors.txt | Tee-Object -FilePath howto-statuses.log
```

## 14. Important Limitations

- **Discovery, not enumeration**: the tool finds URLs that are publicly linked from pages, listed in `sitemap.xml`, or reachable via redirects. It does not brute-force hidden or unlinked paths.
- **robots.txt and in-page robots rules are respected**: disallowed URLs are not crawled (they appear as `[ROBOTS]` in console output), and pages marked noindex/nofollow in-page are handled per the crawler's robots policy. There is no option to ignore robots.
- **JavaScript-only links are not discoverable**: URLs that only appear after client-side scripting will not be found, because pages are fetched as plain HTML.
- **No authentication**: the CLI performs simple unauthenticated GET requests. Cookie-based or header-authenticated content is out of scope for the CLI (the underlying library can be configured in code if you host it yourself).
- **Windows x64 only**: the published executable targets 64-bit Windows. Other platforms can run the library or build the CLI from source with the .NET SDK.
