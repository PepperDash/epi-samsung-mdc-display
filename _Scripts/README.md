![PepperDash](./images/logo_pdt_no_tagline_600.png)

# PowerShell Scripts
This repository is a collection of PowerShell scripts used to bundle software for distribution and load bundled software to processors.

## Create Bundle
This script will create a bundle of compiled files, plugins, configuration files, touch panels and web projects.  The following parameters, and their default values, are used too pass information into the script when executed.

```
-PluginDirectory 'Processor\USER\program1\plugins'
-ConfigDirectory 'Processor\USER'
-PromptEveryTime $false 
-PackageDirRelative '\..\Bundles' 
-SlnDirRelative '\..\'
```

## Load Script
This script will ingest PDT Packages and step through a load process. The following parameters and, their default values, are used to pass information into the script when executed.
```
-PackageDirRelative '\..\Bundles'
```

### Example Quick Start
* Download the example [PDT.PowerShell.Distribution.Example.zip](https://bitbucket.org/jalborough/pdt.powershell.scripts/raw/0c0c8f3156fd6c4c2f2b0e797ecbe8a5da3cf4aa/PDT.PowerShell.Distribution.Example.zip)
* Unpack the zip.
* Double Click PDT.PowerShellScripts\PDT.PowerShell.LoadScript.BAT

### Requirements:
This script must be placed one directory level below the packages.
This script requires a pda address-book file  distribution directory level
This script requires PDT style Packages at the Distribution directory level
For example
* `PDT_Distribution\PDT_Addressbook.pda`
* `PDT_Distribution\PDT_Config01_Bundle`
* `PDT_Distribution\PDT_Config02_Bundle`
* `PDT_Distribution\PDT.PowerShellScripts\PDT.PowerShell.LoadScript.BAT`

### Address-book
The address-book, the pda file, is JSON with fields for configuration, device, and address. The Load script will filter the avaialble addresses based on the package and device selections, The script will also update the pda file with new address. This is the beginnings of a manifest.

### Multiple Entries
The Address Prompt and the  Which App prompt can accept multiple entires separated by a space
* `Select An Address [1 - 3, N for New] (Use spaces between multiple):`**1 2 3 N n N**
* `Which App would you like to load? [1-10, A = All, C = Config] (Use spaces between multiple):`**1 3 6 C**

### Session Example
```---
PS C:\Users\JTA\Documents\Stash Folder\IMF\HQ2\Pegasus\Pegasus.SLN200-DMPSBase\PDT.PowerShellScripts> c:\Users\JTA\Documents\Stash Folder\IMF\HQ2\Pegasus\Pegasus.SLN200-DMPSBase\PDT.PowerShellScripts\PDT.PowerShell.LoadScript.PS1

---------------------
IMF.SLN200
v02.01.063
---------------------
1: CNF.LCD.SingleLaptop
2: CNF.LCD.VTC.SingleLaptop
3: CNF.LCD.VTC
4: CNF.LCD
5: CNF.PRJ.VTC
6: CNF.PRJ
7: CVL
---------------------
Select A Package [1 - 7]: 1
---------------------
1: Processor01
2: TouchPanel01
---------------------
Select A Device [1 - 2]: 1
---------------------
1: 10.11.50.106
2: 10.11.50.108
3: 10.11.50.107
---------------------
Select An Address [1 - 3, N for New] (Use spaces between multiple): 2
---------------------
1: IMF.Pegasus.SLN200.P01-Main_v00.00.38.lpz
2: IMF.Pegasus.SLN200.P02-FlexButtons_v00.00.05.lpz
3: _PDTMM_Flex_FlexActions-App_v00.23_DMPS3-300-C.lpz
4: SLN200.P04-DMPS4k300C_v00.04.lpz
5: _PDTMM_Biamp_Tesira_App_v00.17_DMPS300.lpz
6: DynFusion.cpz
C: IMF.SLN200_v02.01.063_versions.pdtc
C: SLN200.Config.DynFusion.CNF.LCD.SingleLaptop.pdtc
C: SLN200.Config.SYS.CNF.LCD.SingleLaptop.pdtc
---------------------
Which App would you like to load? [1-10, A = All, C = Config] (Use spaces between multiple): 1
 Waiting for program to stop
 Program Stopped Successfully. Deleting program files now.
Deleting program files now from \SIMPL\app01

Looking for *.lpz/*.cpz in the current program directory for App 1.
Unzipping new program now for App 1...........................................


### Version History

### 2018-06-07 JTA
- Changes to the workflow and bugfixes. 

```
