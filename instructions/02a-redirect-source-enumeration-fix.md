\# InfinityCrawler Stage 02A: Redirect Source Enumeration Correction

\#\# Objective

Correct Stage 02 so the CLI enumerates redirect source URLs as well as the final redirect destination.

The current CLI formats only \`CrawlResult.CrawledUris\`. InfinityCrawler preserves redirect source hops inside each final \`CrawledUri.RedirectChain\`, where every \`CrawledUriRedirect\` contains the redirect source \`Location\` and its \`Requests\`. Ignoring that chain causes discovered/crawled redirect URLs such as \`/ducks/old/\` to disappear from CLI output even when the source returned an actual 301/302/307 response.

This correction is limited to the CLI/output layer. Do not modify crawler internals or Stage 01 path scoping.

\#\# Required Workflow

Read and follow:

\- \`instructions/00-agent-workflow.md\`  
\- \`instructions/02-url-enumeration-cli.md\`  
\- \`AGENTS.md\`  
\- \`G:\\My Drive\\Arden\\InfinityCrawler\\CONTEXT.md\`

The mandatory per-step SAST timestamp rule applies throughout the entire run.

\#\# Implementation Requirements

1\. Update the CLI result-shaping/formatting path so output includes:  
   \- every final \`CrawledUri.Location\`  
   \- every redirect source \`CrawledUriRedirect.Location\` contained in \`CrawledUri.RedirectChain\`  
2\. For a redirect source entry, render the HTTP status using that redirect entry's own final request status code when available.  
3\. Example expected behavior:

\`\`\`text  
\[301\] https://example.com/ducks/old/  
\[200\] https://example.com/ducks/new/  
\`\`\`

4\. Preserve deterministic ordinal URI sorting across both redirect-source and final-result entries.  
5\. Preserve deduplication by URI across the combined output set.  
6\. If the same URI appears more than once through multiple result/redirect paths, emit one line only. Prefer the representation backed by an actual HTTP status code over a fallback textual crawler state if a choice is necessary.  
7\. Do not invent redirect status codes. Use the \`Requests\` preserved on \`CrawledUriRedirect\`.  
8\. Do not modify library result models merely for CLI convenience. The existing \`RedirectChain\` model already exposes the required data.  
9\. Preserve all existing Stage 02 argument parsing, exit codes, User-Agent, path scoping, and \`--max-pages\` behavior.  
10\. Do not add new CLI options or unrelated functionality.

\#\# Testing Requirements

Add or update tests to prove at minimum:

\- a redirect source URI appears in formatted output  
\- its actual redirect HTTP status is rendered, such as \`\[301\]\`  
\- the final redirect destination still appears with its own final HTTP status  
\- redirect-source and final-result lines participate in one deterministic ordinal sort  
\- duplicates across redirect chains and final results collapse to one URI line  
\- existing Stage 02 formatter/parser/runner tests continue to pass  
\- the in-process path-scoped integration test remains green

Prefer a focused formatter unit test using a \`CrawledUri\` with \`RedirectChain\` plus the existing integration infrastructure where useful. Do not create an external-network dependency.

\#\# Validation

Run:

\`\`\`powershell  
dotnet build InfinityCrawler.sln  
dotnet test tests/InfinityCrawler.Tests \-f net6.0  
dotnet test tests/InfinityCrawler.Tests.Cli  
\`\`\`

Also run:

\`\`\`powershell  
dotnet run \--project src/InfinityCrawler.Cli \-- \--help  
dotnet run \--project src/InfinityCrawler.Cli \-- not-a-url  
\`\`\`

Do not use the documented pre-existing net461 issue as a release gate.

\#\# Report

Update the existing Stage 02 report or write a correction report at:

\`\`\`text  
G:\\My Drive\\Engineer-Forge\\Vault\\Projects\\InfinityCrawler\\reports\\02a-redirect-source-enumeration-fix.md  
\`\`\`

The report must include:

\- implementation result  
\- files changed  
\- redirect-flattening/deduplication design  
\- tests added or changed  
\- validation results  
\- unresolved issues  
\- deviations  
\- blockers/errors

\#\# Acceptance Criteria

This correction is complete only when redirect source URLs are no longer lost from CLI enumeration, their real HTTP redirect statuses are shown when available, the final destination remains present, sorting/deduplication remain deterministic, all required tests pass, and no unrelated functionality is introduced.

