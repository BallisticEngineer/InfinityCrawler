# Report: Stage 03 Windows Publishable Utility

Task: `instructions/03-windows-publishable-utility.md`
Date: 2026-09-08
Status: Complete

## Implementation Result

`InfinityCrawler.Cli` can now be published as a self-contained, single-file, Release-class Windows x64 executable via a committed MSBuild publish profile and a committed PowerShell publish script. The produced `InfinityCrawler.Cli.exe` runs directly on Windows without a separately installed .NET runtime, preserves all approved CLI behavior (path-scoped crawling by default, deterministic URI/status output, redirect source enumeration, `--max-pages`, `--help`/`-h`, exit codes, `InfinityCrawler.Cli/1.0` User-Agent), and is proven to run straight from `artifacts/publish/win-x64/`. No crawler or CLI behavior was changed.

## Files Changed

New:
- `src/InfinityCrawler.Cli/Properties/PublishProfiles/win-x64.pubxml` — committed MSBuild publish profile.
- `publish-win-x64.ps1` — repository-root PowerShell publish script.

Modified:
- `.gitignore` — kept `artifacts/` (already ignored), and added a negation (`!**/Properties/PublishProfiles/*.pubxml`) so the committed publish profile is not caught by the generic `*.pubxml` ignore rule for web publish settings.
- `README.md` — added a concise "Windows Publishable Utility (InfinityCrawler.Cli)" section: how to publish, where the executable is produced, basic invocation, `--max-pages`, and `--help`.

No library, CLI source, or test files were changed in this stage.

## Publish Profile Details

`src/InfinityCrawler.Cli/Properties/PublishProfiles/win-x64.pubxml`:

```xml
<Project>
	<PropertyGroup>
		<Configuration>Release</Configuration>
		<RuntimeIdentifier>win-x64</RuntimeIdentifier>
		<SelfContained>true</SelfContained>
		<PublishSingleFile>true</PublishSingleFile>
		<PublishTrimmed>false</PublishTrimmed>
		<PublishReadyToRun>false</PublishReadyToRun>
	</PropertyGroup>
</Project>
```

- `RuntimeIdentifier` = `win-x64`, `SelfContained` = `true`, `PublishSingleFile` = `true`, `Configuration` = `Release`.
- Trimming is explicitly disabled: the repository and its dependencies (HtmlAgilityPack, TurnerSoftware.RobotsExclusionTools/SitemapTools) are not proven trim-safe, and reliability is preferred over binary size.
- ReadyToRun is not enabled; no demonstrated need, and the profile is kept simple and predictable as required.
- No absolute machine-specific paths in the profile.

## Publish Script Behavior

`publish-win-x64.ps1` (invoked as `.\publish-win-x64.ps1`):

- `$ProgressPreference = 'SilentlyContinue'` set.
- Strict error handling: `$ErrorActionPreference = 'Stop'` plus `Set-StrictMode -Version Latest`; the whole body is wrapped in `try`/`catch` that writes the error and `exit 1` on any failure.
- Resolves the repository root from `$PSScriptRoot` (fallback to current directory), so the caller's working directory does not matter.
- Removes the previous `artifacts/publish/win-x64/` directory before publishing so stale files cannot survive.
- Invokes `dotnet publish <cli.csproj> -c Release /p:PublishProfile=win-x64 -o <repo>/artifacts/publish/win-x64`, using the committed publish profile.
- Checks `$LASTEXITCODE` and throws on non-zero publish failure.
- Verifies `InfinityCrawler.Cli.exe` exists after publish, and prints the final executable path.

## Published Executable

- Path: `G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\artifacts\publish\win-x64\InfinityCrawler.Cli.exe`
- Approximate size: 63,311,281 bytes (~60.4 MiB)
- Single file: the publish directory contains exactly `InfinityCrawler.Cli.exe` and nothing else.

## Tests and Validation Performed

Baseline (unchanged approved behavior):
- `dotnet build InfinityCrawler.sln` — succeeded, 0 errors (only known `NETSDK1138` and pre-existing `NU1504` warnings).
- `dotnet test tests/InfinityCrawler.Tests -f net6.0` — 37/37 passed.
- `dotnet test tests/InfinityCrawler.Tests.Cli` — 28/28 passed.

Publish:
- `.\publish-win-x64.ps1` run from the repository root — succeeded, `SCRIPT_EXIT=0`, `Published:` path printed.
- `publish-win-x64.ps1` invoked a second time from `C:\Users\Sean\AppData\Local\Temp\opencode` (a different working directory) — succeeded, `SCRIPT_EXIT=0`, output written to the correct repository-relative path (proves caller-cwd independence and stale-output cleanup).

## Direct Executable Validation and Exit Codes

Ran the published binary directly (not via `dotnet run`):

```powershell
& .\artifacts\publish\win-x64\InfinityCrawler.Cli.exe --help
```

- Prints help to stdout and exits `0` (`HELP_EXIT=0`).

```powershell
& .\artifacts\publish\win-x64\InfinityCrawler.Cli.exe not-a-url
```

- Prints `'not-a-url' is not a valid absolute HTTP or HTTPS URL.` plus usage to stderr and exits `1` (`INVALID_EXIT=1`).

## Public Smoke-Test Command/Output

One safe, bounded real crawl using the published executable (not an automated test dependency):

```powershell
& .\artifacts\publish\win-x64\InfinityCrawler.Cli.exe https://example.com/ --max-pages 3
```

Output:

```text
[200] https://example.com/
```

`CRAWL_EXIT=0`. Representative deterministic output; example.com is a deliberately tiny single-page site.

## Git/Generated-Artifact Hygiene Check

- `artifacts/`, `bin/`, `obj/`, and `TestResults/` are all confirmed ignored via `git check-ignore` (`artifacts/` from `.gitignore:54`, `[Tt]est[Rr]esult*/`, `[Bb]in/`, `[Oo]bj/`).
- `git status --short --ignored` shows `!! artifacts/` and every project's `bin/`/`obj/` as ignored; none of the generated publish output is tracked or staged.
- The publish profile `src/InfinityCrawler.Cli/Properties/PublishProfiles/win-x64.pubxml` is visible to Git (`git check-ignore -q` → not ignored; appears as untracked), so it will be committed.
- The Stage 03 diff consists solely of intended changes: `.gitignore` (+2 lines: the negation), `README.md` (+21 lines: the utility section), new `publish-win-x64.ps1`, and new `win-x64.pubxml`.

## Unresolved Issues

None.

## Deviations

None from the instruction. Notes:

- Self-contained publish for `net6.0` requires the one-time NuGet restore of the `Microsoft.NETCore.App.Runtime.win-x64` runtime pack (pulled automatically on first publish; network needed the first time).
- The optional public crawl smoke test was included (spec says "if practical"); it is a one-off manual validation, not an automated dependency.

## Relevant Errors or Blockers

- None. The only publish-time messages were the expected `NETSDK1138` (net6.0 out of support) warnings.

## Timestamps (SAST, this run)

- 09:12 — instructions read; exploration began.
- 09:13 — exploration complete; `.gitignore` negation, `win-x64.pubxml`, `publish-win-x64.ps1`, and README section written; build + init tests green; gitignore behavior verified.
- 09:14 — publish from repo root and from a different cwd passed (exit 0); direct exe `--help` (exit 0) and `not-a-url` (exit 1) passed; public bounded crawl (`[200] https://example.com/`, exit 0) passed; hygiene check clean.
- ~09:15 — report written; CONTEXT.md updated.