#!/usr/bin/env python3
"""Render Semgrep CE and Trivy JSON as Actions summaries and warnings.

Secrets and source snippets are excluded from generated summaries and annotations.
"""

from __future__ import annotations

import html
import json
import os
import sys
from collections import Counter
from pathlib import Path
from urllib.parse import quote

MAX_ROWS = 200
MAX_ANNOTATIONS = 50


def safe(value: object) -> str:
    return html.escape(str(value or "").replace("\n", " ").replace("\r", " ").replace("|", "\\|"))


def command_safe(value: object) -> str:
    return (
        str(value or "")
        .replace("%", "%25")
        .replace("\r", "%0D")
        .replace("\n", "%0A")
        .replace(":", "%3A")
        .replace(",", "%2C")
    )


def as_line(value: object) -> int:
    try:
        return max(1, int(value))
    except (TypeError, ValueError):
        return 1


def semgrep_findings(report: dict) -> list[dict]:
    findings = []
    for item in report.get("results", []):
        extra = item.get("extra") or {}
        findings.append(
            {
                "path": str(item.get("path") or ""),
                "line": as_line((item.get("start") or {}).get("line")),
                "severity": str(extra.get("severity") or "UNKNOWN").upper(),
                "rule": str(item.get("check_id") or "unknown-rule"),
                "detail": str(extra.get("message") or "Security finding"),
            }
        )
    return findings


def trivy_findings(report: dict) -> list[dict]:
    findings = []
    for result in report.get("Results", []):
        path = str(result.get("Target") or "")
        for item in result.get("Vulnerabilities") or []:
            pkg = str(item.get("PkgName") or "package")
            installed = str(item.get("InstalledVersion") or "unknown")
            fixed = str(item.get("FixedVersion") or "not provided")
            findings.append(
                {
                    "path": path,
                    "line": 1,
                    "severity": str(item.get("Severity") or "UNKNOWN").upper(),
                    "rule": str(item.get("VulnerabilityID") or "CVE-unknown"),
                    "detail": f"{pkg} {installed}; fixed version: {fixed}",
                }
            )
        for item in result.get("Misconfigurations") or []:
            cause = item.get("CauseMetadata") or {}
            start = cause.get("StartLine") or (cause.get("Code") or {}).get("StartLine")
            findings.append(
                {
                    "path": path,
                    "line": as_line(start),
                    "severity": str(item.get("Severity") or "UNKNOWN").upper(),
                    "rule": str(item.get("ID") or "misconfig"),
                    "detail": str(item.get("Title") or "Configuration issue"),
                }
            )
        for item in result.get("Secrets") or []:
            findings.append(
                {
                    "path": path,
                    "line": as_line(item.get("StartLine")),
                    "severity": str(item.get("Severity") or "UNKNOWN").upper(),
                    "rule": str(item.get("RuleID") or "secret"),
                    "detail": "Potential secret; inspect locally (value withheld)",
                }
            )
    return findings


def main(argv: list[str]) -> int:
    if len(argv) != 3 or argv[1] not in {"semgrep", "trivy"}:
        print("Usage: security_report.py semgrep|trivy report.json", file=sys.stderr)
        return 2
    scanner, name = argv[1:]
    label = "Semgrep CE" if scanner == "semgrep" else "Trivy"
    path = Path(name)
    if not path.is_file():
        print(f"::error title={label} report missing::Expected {name}; scanner may have failed")
        return 1
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(data, dict):
            raise ValueError("JSON root is not an object")
        if scanner == "semgrep" and not isinstance(data.get("results"), list):
            raise ValueError("Semgrep results list is missing")
        if scanner == "trivy":
            if data.get("SchemaVersion") != 2 or not data.get("ArtifactType"):
                raise ValueError("Trivy report metadata is missing")
            results = data.get("Results")
            if results is None:
                results = []
            if not isinstance(results, list):
                raise ValueError("Trivy Results is not a list")
            data["Results"] = results
        findings = semgrep_findings(data) if scanner == "semgrep" else trivy_findings(data)
    except (OSError, ValueError, TypeError, KeyError) as exc:
        print(f"::error title={label} report invalid::{command_safe(exc)}")
        return 1

    findings.sort(key=lambda x: (x["path"], x["line"], x["rule"]))
    counts = Counter(item["severity"] for item in findings)
    repository = os.getenv("GITHUB_REPOSITORY", "")
    sha = os.getenv("GITHUB_SHA", "")
    summary = [
        f"## {label} security findings",
        "",
        f"**Total findings: {len(findings)}** (report-only; successful scan != zero findings)",
        "",
        "Severity: " + ", ".join(f"{safe(k)}: {v}" for k, v in sorted(counts.items())) if counts else "No findings reported.",
        "",
        "| Severity | Rule | Location | Details |",
        "| --- | --- | --- | --- |",
    ]
    for item in findings[:MAX_ROWS]:
        location = f"{item['path']}:{item['line']}" if item["path"] else "unknown"
        if repository and sha and item["path"]:
            url = f"https://github.com/{repository}/blob/{sha}/{quote(item['path'], safe='/')}#L{item['line']}"
            location_cell = f"[{safe(location)}]({url})"
        else:
            location_cell = safe(location)
        detail = item["detail"][:400]
        summary.append(f"| {safe(item['severity'])} | {safe(item['rule'])} | {location_cell} | {safe(detail)} |")
    if len(findings) > MAX_ROWS:
        summary.append(f"\nOnly the first {MAX_ROWS} shown; JSON artifact contains all {len(findings)}.\n")
    summary.append("\nDownload the JSON artifact for full machine-readable detail.\n")
    summary_path = os.getenv("GITHUB_STEP_SUMMARY")
    if summary_path:
        with open(summary_path, "a", encoding="utf-8") as stream:
            stream.write("\n".join(summary) + "\n")
    else:
        print("\n".join(summary))
    for item in findings[:MAX_ANNOTATIONS]:
        if item["path"]:
            title = f"{label} {item['severity']}: {item['rule']}"
            print(
                f"::warning file={command_safe(item['path'])},line={item['line']},"
                f"title={command_safe(title)}::{command_safe(item['detail'][:500])}"
            )
    if len(findings) > MAX_ANNOTATIONS:
        print(f"::notice title={label} annotations limited::{len(findings) - MAX_ANNOTATIONS} further findings in summary and JSON")
    print(f"{label}: {len(findings)} findings, {min(MAX_ANNOTATIONS, len(findings))} annotations")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
