$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    $packageRoot = $PSScriptRoot
    $installRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ReportLayout'
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
    if ([IO.Path]::GetFullPath($packageRoot) -ne [IO.Path]::GetFullPath($installRoot)) {
        foreach ($item in Get-ChildItem -LiteralPath $packageRoot) {
            if ($item.Name -notin @('ReportLayout.exe','build-signature.txt','startup-error.txt')) {
                Copy-Item -LiteralPath $item.FullName -Destination $installRoot -Recurse -Force
            }
        }
    }
    $sourceFile = Join-Path $installRoot 'src\ReportLayout.cs'
    $iconFile = Join-Path $installRoot 'assets\ReportLayout.ico'
    $exeFile = Join-Path $installRoot 'ReportLayout.exe'
    $signatureFile = Join-Path $installRoot 'build-signature.txt'
    $signature = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash + (Get-FileHash -LiteralPath $iconFile -Algorithm SHA256).Hash
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
                $compiled = $provider.CompileAssemblyFromSource($compiler, [string[]]@([IO.File]::ReadAllText($sourceFile,[Text.Encoding]::UTF8)))
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
    $wsh = New-Object -ComObject WScript.Shell
    try {
        $desktop = [Environment]::GetFolderPath('DesktopDirectory')
        $startFolder = Join-Path ([Environment]::GetFolderPath('Programs')) 'Report Layout'
        New-Item -ItemType Directory -Path $startFolder -Force | Out-Null
        foreach ($shortcutPath in @((Join-Path $desktop 'Report Layout.lnk'), (Join-Path $startFolder 'Report Layout.lnk'))) {
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
    [System.Windows.Forms.MessageBox]::Show("Setup could not finish. Close any running Report Layout window and try again. If the problem remains, send startup-error.txt.`r`n" + $_.Exception.Message, 'Report Layout Setup') | Out-Null
}
