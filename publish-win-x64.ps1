<#
.SYNOPSIS
Publishes InfinityCrawler.Cli as a self-contained single-file Windows x64 executable.

.DESCRIPTION
Produces artifacts/publish/win-x64/InfinityCrawler.Cli.exe using the committed
win-x64 publish profile. Runs from any working directory.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version Latest

function Resolve-RepoRoot
{
	$current = $PSScriptRoot
	if ([string]::IsNullOrEmpty($current))
	{
		$current = (Get-Location).Path
	}

	return (Resolve-Path -LiteralPath $current).Path
}

function Invoke-CliPublish
{
	$repoRoot = Resolve-RepoRoot
	$projectPath = Join-Path $repoRoot 'src\InfinityCrawler.Cli\InfinityCrawler.Cli.csproj'
	$outputDir = Join-Path $repoRoot 'artifacts\publish\win-x64'

	if (-not (Test-Path -LiteralPath $projectPath))
	{
		throw "CLI project not found: $projectPath"
	}

	if (Test-Path -LiteralPath $outputDir)
	{
		Remove-Item -LiteralPath $outputDir -Recurse -Force
	}

	& dotnet publish $projectPath -c Release /p:PublishProfile=win-x64 -o $outputDir
	if ($LASTEXITCODE -ne 0)
	{
		throw "dotnet publish failed with exit code $LASTEXITCODE"
	}

	$exePath = Join-Path $outputDir 'InfinityCrawler.Cli.exe'
	if (-not (Test-Path -LiteralPath $exePath))
	{
		throw "Published executable not found: $exePath"
	}

	Write-Host "Published: $exePath"
}

try
{
	Invoke-CliPublish
	exit 0
}
catch
{
	Write-Error $_
	exit 1
}