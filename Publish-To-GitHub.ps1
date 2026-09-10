[CmdletBinding()]
param(
    [string]$Repository = "report-layout",
    [ValidateSet("private", "public")]
    [string]$Visibility = "private",
    [string]$Version = "1.1.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Require-Command([string]$Name, [string]$InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name is required. $InstallHint"
    }
}

Require-Command "git" "Install Git for Windows, then reopen PowerShell."
Require-Command "gh" "Install GitHub CLI with: winget install --id GitHub.cli"

gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "GitHub authentication is required."
    gh auth login
}

$owner = (gh api user --jq .login).Trim()
if (-not $owner) { throw "Could not determine the authenticated GitHub account." }

$tag = "v$Version"
$dist = Join-Path $root "dist"
$stage = Join-Path $env:TEMP "Report-Layout-$Version"
$zip = Join-Path $dist "Report-Layout-Windows-$tag.zip"
$checksums = Join-Path $dist "SHA256SUMS.txt"

Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item $stage -ItemType Directory -Force | Out-Null
New-Item $dist -ItemType Directory -Force | Out-Null

$packageItems = @(
    "Build-and-Run.ps1", "Start.cmd", "Start.vbs", "Layout-Report.jsx",
    "README.md", "PROJECT_MEMORY.md", "CHANGELOG.md", "assets", "src", "tests"
)
foreach ($item in $packageItems) {
    Copy-Item (Join-Path $root $item) $stage -Recurse -Force
}

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zip -Leaf)" | Set-Content $checksums -Encoding ascii

if (git status --porcelain) {
    throw "The working tree has uncommitted changes. Commit them before publishing."
}

git rev-parse $tag 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    git tag -a $tag -m "Report Layout $tag"
}

$repoName = "$owner/$Repository"
gh repo view $repoName 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    gh repo create $repoName "--$Visibility" --source . --remote origin --push
} else {
    git remote get-url origin 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        git remote add origin "https://github.com/$repoName.git"
    }
    git push -u origin main
}

git push origin $tag

gh release view $tag --repo $repoName 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    gh release upload $tag $zip $checksums --repo $repoName --clobber
} else {
    gh release create $tag $zip $checksums `
        --repo $repoName `
        --title "Report Layout $tag" `
        --notes-file (Join-Path $root "release/RELEASE-NOTES-$tag.md")
}

Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Published successfully: https://github.com/$repoName/releases/tag/$tag"
