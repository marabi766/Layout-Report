[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9_.-]+$')][string]$Repository='report-layout',
    [ValidateSet('private','public')][string]$Visibility='private',
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string]$Version='2.0.0-preview.1'
)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
function Check-Exit([string]$Step) { if ($LASTEXITCODE -ne 0) { throw "$Step failed (exit $LASTEXITCODE). Nothing further will be published." } }
foreach($name in @('git','gh')) { if (!(Get-Command $name -ErrorAction SilentlyContinue)) { throw "$name is required. Install Git for Windows and GitHub CLI first." } }
git rev-parse --is-inside-work-tree
Check-Exit 'Git checkout check'
$branch=git branch --show-current
Check-Exit 'Branch check'
if($branch -ne 'main'){throw 'Switch to the intended main branch before publishing.'}
$dirty=git status --porcelain
Check-Exit 'Working tree check'
if($dirty){throw 'Commit the intended source changes before publishing.'}
gh auth status
if($LASTEXITCODE -ne 0){gh auth login;Check-Exit 'GitHub login'}
$owner=gh api user --jq .login
Check-Exit 'GitHub identity check'
$repoName="$owner/$Repository"
$tag="v$Version"
$notes=Join-Path $PSScriptRoot "release/RELEASE-NOTES-$tag.md"
if(!(Test-Path -LiteralPath $notes)){throw "Release notes are missing: $notes"}
$head=git rev-parse HEAD
Check-Exit 'Commit check'
$tags=@(git tag --list $tag)
Check-Exit 'Tag check'
if($tags.Count -gt 0){
    $tagCommit=git rev-list -n 1 $tag
    Check-Exit 'Tag commit check'
    if($tagCommit -ne $head){throw 'The existing version tag points at another commit. Use a new version; do not force-move a published tag.'}
}
$remotes=@(git remote)
Check-Exit 'Remote check'
if($remotes -contains 'origin'){
    $origin=git remote get-url origin
    Check-Exit 'Origin check'
    $expected='^(https://github\.com/|git@github\.com:)'+[regex]::Escape($repoName)+'(\.git)?/?$'
    if($origin -notmatch $expected){throw "Origin does not match $repoName. Check the selected account/repository before publishing."}
}
gh repo view $repoName --json nameWithOwner
if($LASTEXITCODE -ne 0){
    if($remotes -contains 'origin'){throw 'The configured remote is inaccessible. Check GitHub permissions before retrying.'}
    gh repo create $repoName "--$Visibility" --source . --remote origin
    Check-Exit 'Repository creation'
}elseif(!($remotes -contains 'origin')){
    git remote add origin "https://github.com/$repoName.git"
    Check-Exit 'Remote creation'
}
gh auth setup-git
Check-Exit 'Git authentication setup'
if($tags.Count -eq 0){git tag -a $tag -m "Report Layout $tag";Check-Exit 'Tag creation'}
git push -u origin main
Check-Exit 'Branch push'
git push origin $tag
Check-Exit 'Tag push'

$dist=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$zip=Join-Path $dist "Report-Layout-Windows-$tag.zip"
if(Test-Path -LiteralPath $zip){throw 'A local release ZIP already exists. Move it aside before building a new release asset.'}
# Archive the exact tagged source; includes launchers, runtime assets, docs and tests.
git archive --format=zip "--output=$zip" $tag
Check-Exit 'Release archive'
$checksum=Join-Path $dist "SHA256SUMS-$tag.txt"
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($checksum,"$hash  $([IO.Path]::GetFileName($zip))`n",[Text.Encoding]::ASCII)
$releaseArgs=@('release','create',$tag,$zip,$checksum,'--repo',$repoName,'--verify-tag','--title',"Report Layout $tag",'--notes-file',$notes)
if($Version.Contains('-')){$releaseArgs+=@('--prerelease','--latest=false')}
# gh refuses an existing release; no clobber, force-push or tag replacement.
& gh @releaseArgs
Check-Exit 'Release publication'
Write-Output "Published successfully: https://github.com/$repoName/releases/tag/$tag"
