@echo off
PowerShell.exe -Command "& '%~dp0\*Scripts\PDT.PowerShell.Authentication.ps1' -NewUsername 'crestron' -NewPassword 'pepperdash' -SelectedAddrBook '1' -SelectedAddress 'a' -SlnDirRelative '\..'"
pause