Option Explicit
Dim shell, fso, base, ps, script, command
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
ps = shell.ExpandEnvironmentStrings("%SystemRoot%") & "\System32\WindowsPowerShell\v1.0\powershell.exe"
script = base & "\Build-and-Run.ps1"
command = Chr(34) & ps & Chr(34) & " -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File " & Chr(34) & script & Chr(34)
shell.Run command, 0, False
