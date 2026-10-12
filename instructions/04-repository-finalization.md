\# InfinityCrawler Stage 04: Repository Finalization

\#\# Objective

Finalize the approved InfinityCrawler work into a clean local Git commit and leave the repository in a known-good state.

This stage is source-control closure only. Do not change crawler behavior, CLI behavior, packaging behavior, or documentation content except where strictly necessary to correct an accidental omission discovered during finalization.

\#\# Required Workflow

Read and follow:

\- \`instructions/00-agent-workflow.md\`  
\- \`instructions/01-path-scoped-crawling.md\`  
\- \`instructions/02-url-enumeration-cli.md\`  
\- \`instructions/02a-redirect-source-enumeration-fix.md\`  
\- \`instructions/03-windows-publishable-utility.md\`  
\- \`AGENTS.md\`  
\- \`G:\\My Drive\\Arden\\InfinityCrawler\\CONTEXT.md\`

The mandatory per-step SAST timestamp requirement applies throughout the entire run.

\#\# Scope

1\. Inspect \`git status\`, current branch, configured remotes, and the full pending diff.  
2\. Confirm that only intended InfinityCrawler source/config/test/documentation/script changes are pending.  
3\. Confirm generated content is not staged or tracked, including \`artifacts/\`, \`bin/\`, \`obj/\`, and \`TestResults/\`.  
4\. Re-run the final validation baseline before committing:

\`\`\`powershell  
dotnet build InfinityCrawler.sln  
dotnet test tests/InfinityCrawler.Tests \-f net6.0  
dotnet test tests/InfinityCrawler.Tests.Cli  
\`\`\`

5\. Do not rerun the publish unless needed to investigate a discrepancy. Stage 03 publish validation is already approved.  
6\. Stage all intended project changes only.  
7\. Review the staged diff before committing.  
8\. Create one local commit containing the approved InfinityCrawler implementation.  
9\. Use a clear commit message representing the complete scoped crawler CLI work, for example:

\`\`\`text  
Add path-scoped crawler CLI and Windows publish workflow  
\`\`\`

10\. After committing, verify:  
   \- commit succeeded  
   \- working tree is clean, except for intentionally ignored generated files  
   \- the commit contains only intended project changes  
   \- current branch and HEAD commit SHA are recorded  
11\. Do \*\*not\*\* push to any remote.  
12\. Do \*\*not\*\* create or switch branches unless required to preserve existing repository safety. If a branch concern exists, stop before committing and report it rather than guessing.  
13\. Do \*\*not\*\* amend, rebase, merge, tag, create a release, or alter remotes.

\#\# Source-Control Safety

Before the commit, explicitly inspect the configured remote URLs and current branch so the repository state is understood.

The existence of upstream \`TurnerSoftware/InfinityCrawler\` does not authorize pushing there. This stage ends with a local commit only.

If Git reports an unexpected branch, detached HEAD, unresolved merge/rebase state, staged unrelated changes, or suspicious remote configuration, do not proceed with the commit. Report the exact state instead.

\#\# Required Report

Write the coding-agent report to:

\`\`\`text  
G:\\My Drive\\Engineer-Forge\\Vault\\Projects\\InfinityCrawler\\reports\\04-repository-finalization.md  
\`\`\`

The report must include:

\- starting branch and HEAD  
\- configured remotes  
\- pre-commit \`git status\` summary  
\- validation results  
\- files included in the commit  
\- generated-artifact hygiene result  
\- commit message  
\- resulting commit SHA  
\- post-commit \`git status\`  
\- confirmation that no push occurred  
\- unresolved issues  
\- deviations  
\- relevant errors or blockers  
\- per-step SAST timestamps

\#\# Acceptance Criteria

Stage 04 is complete only when:

\- the final build passes  
\- 37/37 library tests pass on net6.0  
\- 28/28 CLI tests pass  
\- the staged diff contains only intended InfinityCrawler changes  
\- generated artifacts remain ignored and untracked  
\- one local commit is created successfully  
\- the working tree is clean afterward, aside from ignored generated files  
\- no remote push occurs  
\- the required report is written  
