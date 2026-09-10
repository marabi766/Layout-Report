$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Build-and-Run.ps1') -BuildOnly
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }
[void][Reflection.Assembly]::LoadFrom((Join-Path $root 'bin\ReportLayout.exe'))
foreach ($name in @('Economic Report','Book Summary','Research Report','Compact Report')) {
    $preset = [LayoutOptions]::Preset($name)
    $preset.Validate()
    $encoded = [SettingsStore]::Serialize($preset)
    $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $restored = $serializer.Deserialize($encoded, [LayoutOptions])
    $restored.Validate()
    if ($restored.BodySize -ne $preset.BodySize) { throw 'Preset round-trip failed.' }
}
$bad = New-Object LayoutOptions
$bad.BodySize = 0
$rejected = $false
try { $bad.Validate() } catch { $rejected = $true }
if (!$rejected) { throw 'Invalid font size was accepted.' }
if (![ReportLayout]::IsNewerRelease('v2.0.0','2.0.0-preview.1')) { throw 'Stable release upgrade was not detected.' }
if ([ReportLayout]::IsNewerRelease('v1.1.1','2.0.0-preview.1')) { throw 'A downgrade was offered.' }
if ([ReportLayout]::IsNewerRelease('v2.0.0','2.0.0')) { throw 'Same version was offered.' }
if (![ReportLayout]::IsNewerRelease('v2.1.0','2.0.0')) { throw 'Minor update was missed.' }
$dataPath=Join-Path $root ('bin\test-settings-'+[Guid]::NewGuid().ToString('N')+'.json')
try {
    $s=New-Object UserSettings
    $s.Author='Test'
    [SettingsStore]::Write($dataPath,$s)
    $s.Author='Updated'
    [SettingsStore]::Write($dataPath,$s)
    $read=$serializer.Deserialize([IO.File]::ReadAllText($dataPath),[UserSettings])
    if ($read.Author -ne 'Updated') { throw 'Atomic settings replacement failed.' }
} finally { if(Test-Path -LiteralPath $dataPath){Remove-Item -LiteralPath $dataPath} }
Write-Output 'PASS: Windows compilation, presets, JSON round-trip, invalid input, update comparison and settings persistence.'
