Option Explicit
Dim shell, fso, base, ps, script, command, exitCode, exe
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
ps = shell.ExpandEnvironmentStrings("%SystemRoot%") & "\System32\WindowsPowerShell\v1.0\powershell.exe"
script = base & "\Build-and-Run.ps1"
command = Chr(34) & ps & Chr(34) & " -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File " & Chr(34) & script & Chr(34) & " -BuildOnly"
exitCode = shell.Run(command, 0, True)
If exitCode = 0 Then
    exe = base & "\bin\ReportLayout.exe"
    shell.Run Chr(34) & exe & Chr(34) & " --image-placeholders", 1, False
Else
    MsgBox "Placeholder edition could not be built. See startup-error.txt.", vbCritical, "Report Layout"
End If
