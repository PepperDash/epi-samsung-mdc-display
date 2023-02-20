@ECHO OFF
PowerShell.exe -Command "& '%~dp0\*Scripts\PDT.PowerShell.CreateBundle.ps1' -PluginDirectory 'Processor\USER\program1\plugins' -ConfigDirectory 'Processor\USER' -PromptEveryTime $false -PackageDirRelative '\..\..\_Bundles' -SlnDirRelative '\..\..\'"
PAUSE
