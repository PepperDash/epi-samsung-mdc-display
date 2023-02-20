@echo off
PowerShell.exe -Command "& '%~dp0\*Scripts\PDT.PowerShell.CustomCommands.ps1' -CommandFile 'ProcSetup.json' -SlnDirRelative '\..\..'"
pause