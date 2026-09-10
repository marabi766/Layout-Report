param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    $packageRoot = $PSScriptRoot
    $installRoot = if ($BuildOnly) { Join-Path $packageRoot 'bin' } else { Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ReportLayoutPreview' }
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
    if ([IO.Path]::GetFullPath($packageRoot) -ne [IO.Path]::GetFullPath($installRoot)) {
        foreach ($name in @('src','assets','Layout-Report.jsx','Start.vbs','Start.cmd','Build-and-Run.ps1')) {
            Copy-Item -LiteralPath (Join-Path $packageRoot $name) -Destination $installRoot -Recurse -Force
        }
    }
    $sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $installRoot 'src') -Filter '*.cs' | Sort-Object Name)
    $iconFile = Join-Path $installRoot 'assets\ReportLayout.ico'
    $exeFile = Join-Path $installRoot 'ReportLayout.exe'
    $signatureFile = Join-Path $installRoot 'build-signature.txt'
    $signature = (($sourceFiles | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }) -join '') + (Get-FileHash -LiteralPath $iconFile -Algorithm SHA256).Hash
    $previous = if (Test-Path -LiteralPath $signatureFile) { [IO.File]::ReadAllText($signatureFile) } else { '' }
    if (!(Test-Path -LiteralPath $exeFile) -or $signature -ne $previous) {
        $temporaryExe = Join-Path $installRoot ('ReportLayout-build-' + [Guid]::NewGuid().ToString('N') + '.exe')
        try {
            $provider = New-Object Microsoft.CSharp.CSharpCodeProvider
            $compiler = New-Object System.CodeDom.Compiler.CompilerParameters
            $compiler.GenerateExecutable = $true
            $compiler.GenerateInMemory = $false
            $compiler.OutputAssembly = $temporaryExe
            $compiler.CompilerOptions = '/target:winexe /win32icon:"{0}"' -f $iconFile
            $compiler.ReferencedAssemblies.AddRange([string[]]@('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll'))
            try {
                $compiled = $provider.CompileAssemblyFromSource($compiler, [string[]]@($sourceFiles | ForEach-Object { [IO.File]::ReadAllText($_.FullName,[Text.Encoding]::UTF8) }))
                if ($compiled.Errors.HasErrors) {
                    throw (($compiled.Errors | Where-Object { !$_.IsWarning } | ForEach-Object { $_.ToString() }) -join "`r`n")
                }
            } finally { $provider.Dispose() }
            Move-Item -LiteralPath $temporaryExe -Destination $exeFile -Force
            [IO.File]::WriteAllText($signatureFile,$signature)
        } finally {
            if (Test-Path -LiteralPath $temporaryExe) { Remove-Item -LiteralPath $temporaryExe -Force }
        }
    }
    if ($BuildOnly) { Write-Output $exeFile; exit 0 }
    $wsh = New-Object -ComObject WScript.Shell
    try {
        $desktop = [Environment]::GetFolderPath('DesktopDirectory')
        $startFolder = Join-Path ([Environment]::GetFolderPath('Programs')) 'Report Layout Preview'
        New-Item -ItemType Directory -Path $startFolder -Force | Out-Null
        foreach ($shortcutPath in @((Join-Path $desktop 'Report Layout Preview.lnk'), (Join-Path $startFolder 'Report Layout Preview.lnk'))) {
            $shortcut = $wsh.CreateShortcut($shortcutPath)
            $shortcut.TargetPath = $exeFile
            $shortcut.WorkingDirectory = $installRoot
            $shortcut.IconLocation = "$exeFile,0"
            $shortcut.Description = 'Build editable InDesign reports from Word'
            $shortcut.Save()
            [Runtime.InteropServices.Marshal]::ReleaseComObject($shortcut) | Out-Null
        }
    } finally {
        [Runtime.InteropServices.Marshal]::ReleaseComObject($wsh) | Out-Null
    }
    Start-Process -FilePath $exeFile -WorkingDirectory $installRoot
} catch {
    $errorFile = Join-Path $PSScriptRoot 'startup-error.txt'
    $_ | Out-String | Set-Content -LiteralPath $errorFile -Encoding UTF8
    if (!$BuildOnly) { [System.Windows.Forms.MessageBox]::Show("Setup could not finish. Close any running Report Layout window and try again. If the problem remains, send startup-error.txt.`r`n" + $_.Exception.Message, 'Report Layout Setup') | Out-Null }
    Write-Error $_
    exit 1
}
