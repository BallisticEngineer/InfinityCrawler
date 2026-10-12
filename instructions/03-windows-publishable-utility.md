\# InfinityCrawler Stage 03: Windows Publishable Utility

\#\# Objective

Turn the approved scoped-crawl CLI into an easy-to-run Windows utility without expanding crawler functionality.

The result should be a predictable self-contained Windows x64 publish that can be copied to another Windows machine and run without requiring the .NET runtime to be installed separately.

This stage is packaging and release-readiness only. Do not add crawler features.

\#\# Required Workflow

Read and follow:

\- \`instructions/00-agent-workflow.md\`  
\- \`instructions/01-path-scoped-crawling.md\`  
\- \`instructions/02-url-enumeration-cli.md\`  
\- \`instructions/02a-redirect-source-enumeration-fix.md\`  
\- \`AGENTS.md\`  
\- \`G:\\My Drive\\Arden\\InfinityCrawler\\CONTEXT.md\`

The mandatory per-step SAST timestamp requirement applies throughout the entire run.

\#\# Scope

Implement a reproducible Windows x64 publish workflow for \`InfinityCrawler.Cli\`.

Target user experience after publishing:

\`\`\`powershell  
InfinityCrawler.Cli.exe https://example.com/ducks/  
\`\`\`

The published executable must preserve all currently approved CLI behavior:

\- strict starting-path subtree scope enabled by default  
\- deterministic URI/status output  
\- redirect source enumeration  
\- \`--max-pages\`  
\- \`--help\` / \`-h\`  
\- existing exit-code behavior  
\- current User-Agent

\#\# Implementation Requirements

1\. Add a publish profile for \`InfinityCrawler.Cli\` using repository-appropriate MSBuild conventions.  
2\. Publish target:  
   \- Runtime Identifier: \`win-x64\`  
   \- self-contained: \`true\`  
   \- single-file executable: \`true\`  
   \- Release configuration  
3\. Do not enable trimming unless the repository and dependencies are proven safe with it. Prefer reliability over a smaller binary.  
4\. Do not enable ReadyToRun unless there is a demonstrated need. Keep the profile simple and predictable.  
5\. Published output must go to a clear repository-relative location under \`artifacts/\`, for example:

   \`artifacts/publish/win-x64/\`

6\. Ensure \`artifacts/\` is ignored by Git if it is not already ignored. Do not commit generated binaries.  
7\. Add a minimal repository-level PowerShell publish script named something clear such as \`publish-win-x64.ps1\`.  
8\. The PowerShell script must:  
   \- set \`$ProgressPreference \= 'SilentlyContinue'\`  
   \- use strict error handling  
   \- resolve the repository root robustly rather than depending on the caller's current directory  
   \- remove or clean the previous target publish directory before publishing so stale files cannot survive  
   \- invoke the CLI project publish using the committed publish profile  
   \- fail with a non-zero exit code if publish fails  
   \- verify that \`InfinityCrawler.Cli.exe\` exists after publish  
   \- print the final executable path on success  
9\. Do not add installation into Program Files, PATH modification, registry writes, scheduled tasks, MSI/MSIX packaging, Chocolatey/Scoop manifests, signing infrastructure, auto-update logic, or release-upload automation in this stage.  
10\. Do not change CLI arguments or crawler behavior.

\#\# Publish Profile

Prefer a standard \`.pubxml\` profile under the CLI project, for example:

\`\`\`text  
src/InfinityCrawler.Cli/Properties/PublishProfiles/win-x64.pubxml  
\`\`\`

The exact property set may vary with the SDK, but it must achieve the required self-contained single-file Windows x64 Release publish.

Avoid absolute machine-specific paths inside the profile.

\#\# Documentation

Add a concise section to an appropriate existing README if one clearly governs repository usage, or add a small CLI-specific README if that is cleaner.

Document only:

\- how to publish on Windows using the PowerShell script  
\- where the executable is produced  
\- basic invocation  
\- \`--max-pages\`  
\- \`--help\`

Do not turn this stage into a documentation rewrite.

\#\# Testing and Validation

Before packaging validation, ensure the approved test baseline remains green:

\`\`\`powershell  
dotnet build InfinityCrawler.sln  
dotnet test tests/InfinityCrawler.Tests \-f net6.0  
dotnet test tests/InfinityCrawler.Tests.Cli  
\`\`\`

Then run the publish script from the repository root.

Also prove the script is independent of the caller's current directory by invoking it once from a different working directory if practical.

Validate the published executable directly, not through \`dotnet run\`:

\`\`\`powershell  
& .\\artifacts\\publish\\win-x64\\InfinityCrawler.Cli.exe \--help  
& .\\artifacts\\publish\\win-x64\\InfinityCrawler.Cli.exe not-a-url  
\`\`\`

Confirm expected exit codes:

\- \`--help\` \=\> \`0\`  
\- invalid URL \=\> \`1\`

If practical, perform one safe real HTTP/HTTPS crawl against a small public path using the published executable. Keep the crawl bounded with \`--max-pages\` and record the exact command and representative output. Do not make an external site an automated test dependency.

\#\# Generated Artifact Hygiene

After validation:

\- generated publish output may remain locally for verification  
\- it must not be tracked by Git  
\- the final diff must contain only source/config/documentation changes intended for Stage 03  
\- do not commit \`bin/\`, \`obj/\`, \`TestResults/\`, or \`artifacts/\` output

\#\# Required Report

Write the coding-agent report to:

\`\`\`text  
G:\\My Drive\\Engineer-Forge\\Vault\\Projects\\InfinityCrawler\\reports\\03-windows-publishable-utility.md  
\`\`\`

The report must include:

\- implementation result  
\- files changed  
\- publish profile details  
\- publish script behavior  
\- published executable path and approximate file size  
\- tests and validation performed  
\- direct executable validation results and exit codes  
\- public smoke-test command/output if performed  
\- Git/generated-artifact hygiene check  
\- unresolved issues  
\- deviations  
\- relevant errors or blockers

\#\# Acceptance Criteria

Stage 03 is complete only when:

\- the approved CLI behavior remains unchanged  
\- the solution builds successfully  
\- all required library and CLI tests pass  
\- a committed reproducible \`win-x64\` self-contained single-file publish configuration exists  
\- a committed PowerShell script produces the executable under \`artifacts/publish/win-x64/\`  
\- the publish script works regardless of caller working directory  
\- the resulting \`InfinityCrawler.Cli.exe\` runs directly and passes help/invalid-argument smoke checks with expected exit codes  
\- generated publish artifacts are not tracked by Git  
\- no installer, PATH manipulation, updater, GUI, database, or unrelated feature expansion is introduced  
\- the required report is written  
