# InfinityCrawler Stage 08: Finalize Stage 07, Update Usage, Commit, and Push

## Objective

Finalize the already-approved Stage 07 work by updating `docs/usage.md` to reflect the final validated user workflow, validating the complete pending diff, staging only the intended Stage 07 changes, committing them, pushing `main` to `origin`, and verifying the remote state afterward.

This stage is the final source-control closure for the current InfinityCrawler goal.

Do not add new crawler features.

## Required Reading

Read and follow:

- `instructions/00-agent-workflow.md`
- `instructions/07-plain-url-output-and-usage-docs.md`
- `reports/07-plain-url-output-and-usage-docs.md`
- `reports/06-real-world-end-to-end-validation.md`
- `AGENTS.md`
- `G:\My Drive\Arden\InfinityCrawler\CONTEXT.md`
- current `docs/usage.md`
- current `README.md`

The mandatory per-step SAST timestamp requirement applies throughout the entire run.

## Current Approved State

Stage 07 is already implemented and reviewed as approved.

Current expected base HEAD before finalization:

`e257ce0bb6c3825f6b5511da84568873206b6c36`

Current branch:

`main`

Current remote:

`origin` -> `https://github.com/BallisticEngineer/InfinityCrawler`

The Stage 07 implementation is intentionally uncommitted at the start of this task.

## Part 1: Update `docs/usage.md`

Before staging, review and update:

`docs/usage.md`

The document must accurately reflect the finished, validated utility as it exists now.

At minimum, ensure the usage guide clearly includes:

1. The basic real-world invocation:
   `InfinityCrawler.Cli.exe https://docs.python.org/3/howto/`

2. The plain URL inventory workflow:
   `InfinityCrawler.Cli.exe https://docs.python.org/3/howto/ --output urls.txt`

3. A clear explanation that:
   - normal console output is `[status] URL`
   - `--output <file>` writes only bare URLs, one URL per line
   - the output file contains the same discovered URL inventory as console output
   - output is deterministically sorted and deduplicated
   - redirect-source URLs are included in the inventory when encountered

4. The validated path-scoping behavior:
   - starting at `/3/howto/` stays inside `/3/howto/`
   - unrelated same-host paths are excluded
   - cross-host URLs are excluded
   - query strings on in-scope paths remain valid
   - fragments are stripped for deduplication

5. The real-world validation result:
   - target: `https://docs.python.org/3/howto/`
   - 28 URLs discovered
   - zero same-host scope leakage
   - zero cross-host leakage
   - all emitted entries in the final validation were HTTP 200
   - the crawl completed successfully using the published Windows executable

6. Practical output examples showing both:
   - console `[200] URL` form
   - bare-URL file form

7. The existing limitations must remain accurate:
   - discovery-based, not hidden-directory brute force
   - robots.txt respected
   - no JavaScript-rendered link discovery
   - no authentication workflow
   - Windows x64 published utility

Do not claim capabilities not implemented.

Do not turn `usage.md` into release notes or an internal engineering report. Keep it a practical user manual.

## Part 2: Pre-Commit Safety Inspection

Inspect:

- current branch
- HEAD
- remotes
- upstream tracking
- `git status`
- full pending diff
- untracked files

Confirm:

- branch is `main`
- repository is not in detached HEAD
- no merge/rebase/cherry-pick/revert/bisect is in progress
- `origin` still points to `https://github.com/BallisticEngineer/InfinityCrawler`
- no unexpected local commits exist before staging
- only intended Stage 07 source/test/docs changes plus the requested `docs/usage.md` update are candidates for commit

Known non-implementation/untracked items may exist:

- `instructions/`
- reports created after prior commits
- ignored `TestResults/`
- tooling artifact `PowerShellEditorServices.json`

Do not stage tooling artifacts, instruction files, report files, TestResults, published artifacts, bin, obj, or generated indexes unless explicitly required by the existing repository policy.

If any unrelated source/config change is present, STOP and report it instead of guessing.

## Part 3: Validation

Run:

```powershell
dotnet build InfinityCrawler.sln
dotnet test tests/InfinityCrawler.Tests -f net6.0
dotnet test tests/InfinityCrawler.Tests.Cli
```

Expected:

- build: 0 errors
- library tests: 37/37
- CLI tests: 49/49

Then verify the published utility still exposes `--output`:

```powershell
& ".\artifacts\publish\win-x64\InfinityCrawler.Cli.exe" --help
```

If the published EXE is stale relative to source, republish using:

```powershell
.\publish-win-x64.ps1
```

Do not commit generated publish output.

## Part 4: Stage and Review

Stage only the intended Stage 07 implementation, tests, README change, and `docs/usage.md`.

Expected intended project changes include:

- `README.md`
- `docs/usage.md`
- `src/InfinityCrawler.Cli/CliArguments.cs`
- `src/InfinityCrawler.Cli/CliArgumentParser.cs`
- `src/InfinityCrawler.Cli/CrawlResultFormatter.cs`
- `src/InfinityCrawler.Cli/Program.cs`
- `src/InfinityCrawler.Cli/UrlFileWriter.cs`
- `tests/InfinityCrawler.Tests.Cli/CliArgumentParserTests.cs`
- `tests/InfinityCrawler.Tests.Cli/CrawlResultFormatterTests.cs`
- `tests/InfinityCrawler.Tests.Cli/UrlCrawlRunnerTests.cs`
- `tests/InfinityCrawler.Tests.Cli/UrlFileWriterTests.cs`

Review the complete staged diff before committing.

Confirm no unintended file is staged.

## Part 5: Commit

Create one local commit.

Suggested commit message:

`Add plain URL output and usage documentation`

Do not amend the previous commit.

Do not squash, rebase, merge, reset, or rewrite history.

## Part 6: Push

Before pushing:

1. Fetch `origin`.
2. Confirm `origin/main` has not advanced unexpectedly.
3. Confirm the local history is a simple fast-forward from `origin/main`.
4. If remote history has diverged, STOP. Do not merge, rebase, or force-push.

If safe, perform a normal non-force push of local `main` to `origin/main`.

After pushing:

1. Fetch `origin` again.
2. Verify local `main` and `origin/main` point to the same new commit SHA.
3. Verify divergence is `0 0`.
4. Verify no extra local commits remain unpushed.
5. Verify working tree has no modified or staged project files.
6. Untracked instruction/report/tooling files may remain, but must be explicitly listed.

Do not create tags or releases.

## Required Report

Write:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\reports\08-stage07-finalization-and-push.md`

Include:

- starting branch and HEAD
- remote URL
- complete pre-stage Git status
- `docs/usage.md` changes made
- validation results
- exact files staged
- staged-diff review result
- commit message
- new commit SHA
- pre-push fetch/divergence result
- exact push command
- push result
- resulting `origin/main` SHA
- post-push divergence result
- post-push Git status
- confirmation that no force push, tag, release, history rewrite, branch change, or remote change occurred
- unresolved issues
- deviations
- errors/blockers
- per-step SAST timestamps

## Acceptance Criteria

Stage 08 is complete only when:

- `docs/usage.md` accurately reflects the final validated utility
- build passes with 0 errors
- library tests pass 37/37
- CLI tests pass 49/49
- only intended Stage 07 files are staged
- staged diff is reviewed
- exactly one new commit is created
- normal non-force push succeeds
- `origin/main` points to the new commit
- local and remote `main` are synchronized
- no unrelated project changes remain staged or modified
- required report is written
