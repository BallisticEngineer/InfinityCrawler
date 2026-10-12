# InfinityCrawler Stage 06: Real-World End-to-End Validation

## Objective

Prove the original user outcome in the real world.

The goal is not to validate build mechanics, packaging, Git state, or unit tests again. The goal is to demonstrate that the published `InfinityCrawler.Cli.exe` can be given a real starting URL and return the discovered URL inventory beneath that path without wandering into unrelated same-host paths.

The project must not be considered complete until this succeeds.

## Required Workflow

Read and follow:

- `instructions/00-agent-workflow.md`
- `AGENTS.md`
- `G:\My Drive\Arden\InfinityCrawler\CONTEXT.md`

The mandatory per-step SAST timestamp requirement applies throughout the run.

## Primary Real-World Target

Use:

`https://docs.python.org/3/howto/`

Why this target is suitable:

- It is a real public documentation subtree.
- It contains many linked pages beneath `/3/howto/`.
- Pages also contain navigation links to many unrelated same-host paths under `docs.python.org`, which provides a practical test of path scoping.
- It is conventional server-rendered HTML and does not require authentication.

## Published Utility

Use the already-published executable:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\artifacts\publish\win-x64\InfinityCrawler.Cli.exe`

Do not use `dotnet run` for the primary validation unless the published executable is unexpectedly missing or unusable.

## Test Procedure

1. Verify the published EXE exists.
2. Run `--help` once to confirm the executable starts normally.
3. Run the published executable against:

   `https://docs.python.org/3/howto/`

4. Do not use `--max-pages` for the primary run unless the crawl is clearly pathological or unreasonably large. The purpose is to observe the complete discoverable subtree, not an arbitrarily truncated sample.
5. Capture the full stdout and stderr output to files under:

   `G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\TestResults\real-world\`

   Suggested files:

   - `python-howto-output.txt`
   - `python-howto-stderr.txt`

6. Record:
   - exit code
   - elapsed time
   - total emitted URL lines
   - counts grouped by rendered status token, if practical
7. Inspect the output for scope correctness:
   - every emitted `docs.python.org` URL must remain within `/3/howto` subtree semantics
   - URLs such as `/3/tutorial/`, `/3/library/`, `/3/reference/`, `/3/genindex.html`, or other same-host paths outside `/3/howto/` must not be emitted
8. Confirm the crawl includes multiple known HOWTO pages linked from the real index, such as examples from the live site including:
   - `argparse.html`
   - `descriptor.html`
   - `enum.html`
   - `functional.html`
   - `logging.html`
   - `logging-cookbook.html`
   - `regex.html`
   - `sorting.html`
   - `unicode.html`
   - `urllib2.html`
   - `sockets.html`
   Exact live filenames should be verified from the site/output rather than assumed if the documentation has changed.
9. Independently inspect the live starting page or use another simple retrieval method to establish that it contains both:
   - links into `/3/howto/`
   - same-host navigation links outside `/3/howto/`
   This proves that absence of unrelated paths in InfinityCrawler output is meaningful rather than accidental.
10. Review redirects/statuses in the output. Confirm that any redirect-source entries are represented sensibly and that final successful pages remain present.
11. If the tool crashes, hangs, emits only the start URL, escapes the subtree, misses obviously linked HOWTO pages, or produces unusable output, classify the validation as FAILED and diagnose the gap. Do not declare success just because the process exits 0.
12. If a defect is found, stop after gathering enough evidence to explain it. Do not silently redesign or implement a fix in this task unless Sean explicitly authorizes it.

## Practical Usability Check

Evaluate whether Sean can realistically use the utility in the intended style:

`InfinityCrawler.Cli.exe https://docs.python.org/3/howto/`

The output should be directly useful as a URL inventory without requiring code changes or developer tooling.

Note any friction that materially undermines the original goal, such as:

- executable location being awkward for normal repeated use
- excessive non-URL noise
- output format unsuitable for piping/redirection
- unexpectedly slow or hanging behavior
- missing obvious discoverable URLs
- scope leakage

Do not manufacture usability complaints if the current invocation is already practical.

## Success Criteria

Stage 06 passes only if all of the following are true:

- the published EXE executes successfully against the real target
- it returns multiple real discovered URLs beneath `/3/howto/`
- clearly linked HOWTO pages are present
- unrelated same-host paths are absent
- no cross-host URLs are emitted
- output is practically usable as the requested URL inventory
- no functional gap is found that prevents the original user goal
- full evidence is captured and reported

## Required Report

Write:

`G:\My Drive\Engineer-Forge\Vault\Projects\InfinityCrawler\reports\06-real-world-end-to-end-validation.md`

Include:

- exact executable path
- exact command used
- target URL
- exit code
- elapsed time
- emitted URL count
- representative in-scope output
- explicit scope-leak checks
- explicit known-page presence checks
- redirect/status observations
- usability assessment
- PASS or FAIL classification against the original end goal
- unresolved issues
- any blockers
- per-step SAST timestamps

## Source Control

This is primarily a validation task.

Do not create a commit, push, tag, release, or modify Git history.

Generated test evidence under `TestResults\real-world\` should remain uncommitted.

If the validation fails and code changes are required, stop and report the failure first. A separate instruction will define the correction.
