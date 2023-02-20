##### CONFIDENTIAL

PowerShell Scripts for Crestron Control

Processors & Touch Panels

## April 2021

```
Public
```

```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 2 of 20
```
```
Date Document Version Author Description
2021 - 03 - 23 01.00 Jonathan Arndt Initial Draft
2021 - 04 - 22 01.01 Raymond Montoya Updates to draft
2021 - 04 - 22 01.02 Jonathan Arndt Release
```
## Document History


## The material in which this notice appears is the property of PepperDash Technology Corporation, which

```
claims copyright under the laws of the United States of America in the entire body of material and in all
parts thereof, regardless of the use to which it is being put. Any use, in whole or in part, of this material by
```
## Contents

   - PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 3 of
- Document History
- Contents
- Synopsis
      - Purpose
      - Assumptions
      - Required Tools
      - Recommended Tools
      - Software Versions
- Firmware Requirements
- Pre-Script Execution Guide
      - General Disclaimer
      - Step 1) Setup an Execution Policy
      - Step 2) Create initial folder structure
- Script Execution > Update Firmware
      - Update Crestron Control Processor or Touch Panel Firmware (attended)
      - General Disclaimer
- Script Execution > Enable Authentication
      - Execute Authentication Script (attended)
      - Execute Authentication Script (unattended)
- Script Execution > Custom Commands
      - Execute Custom Commands Script (attended)
      - Execute Custom Commands Script (unattended)
- Notices
      - Notice of Ownership and Copyright
      - PepperDash Technology Corporation reserves all rights under applicable laws. another party without the express written permission of PepperDash Technology Corporation is prohibited.


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 4 of 20

```
Notice of Confidentiality ........................................................................................................................... 19
The material in which this notice appears is confidential to PepperDash Technology Corporation. If you
have been provided a copy of this material, it is with the understanding that you will not share it with
others without the express written consent of PepperDash Technology Corporation. ................................ 19
Notice of Trademark and Servicemark ...................................................................................................... 19
```

```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 5 of 20
```
#### Purpose

This documentation is written for software, created by PepperDash, for Crestron controlled systems. The purpose
of this document is to provide guidance on executing PowerShell scripts that will connect to Crestron control
processors and touch panels performing automated functionality detailed in this document.

#### Assumptions

It is assumed that the user of this document has the following training and/or expertise:

- Familiarity with Crestron Toolbox software
- Basic understanding of working program and folder structure on Crestron 3-Series or 4-Series control
    processor and TSW touch panels
- Basic understanding of how to use and run Microsoft Windows PowerShell

#### Required Tools

- Windows 7 operating system or higher
- Windows PowerShell (32 or 64 bit), minimum version 5
- Crestron PowerShell Scripting Enterprise Development Kit (EDK)
    https://sdkcon78221.crestron.com/downloads/EDK/EDK_Setup_1.0.5.3.exe
- TCP/IP network connectivity to Crestron control processor or touch panel
- Access to PepperDash Portal
- PowerShell script software package from PepperDash (obtained from PepperDash Portal)

#### Recommended Tools

- Microsoft Visual Studio Code
    https://code.visualstudio.com/download

## Synopsis


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 6 of 20
```
#### Software Versions

```
Enable Authentication (from default) Script Release Version Release Date
PDT.PowerShell.Authentication.v01.00.ps1 v01. 00 4 / 22 /202 1
```
```
Load file(s) to Crestron control Processor or Touch Panel Release Version Release Date
PDT.PowerShell.LoadScript.v01.00.PS1 v01.00 4/22/
```
```
Send Custom Commands to Processor or Touch Panel Release Version Release Date
PDT.PowerShell.Authentication.v01.00.ps1 v01.00 4/22/
```

```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 7 of 20
```
The following equipment must be running the required firmware. PepperDash PowerShell scripts assume
Crestron equipment is running latest provided device firmware prior to executing scripts.

```
Manufacturer Model Device Type Crestron PUF Firmware Release Date
Crestron 3 - Series Processor N/A 1.7000.0021 Jan, 202 1
Crestron TSW Touch Panel N/A 3.000.0014 Oct, 2020
```
## Firmware Requirements


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 8 of 20
```
#### General Disclaimer

_All scripts written by PepperDash (and Crestron) require the PowerShell Execution Policy set to ‘remote signed’.
This allows provided scripts to be ran on a local machine that were originally created on another machine. See
instructions below on how to set the execution policy. Note: If you intend on utilizing PowerShell-5 for running
attended scripts but later wish to utilize some other version of PowerShell for unattended (example: PowerShell 7),
you will need to set the execution policy to ‘remote signed’ for both versions._

#### Step 1) Setup an Execution Policy

1. Open PowerShell as an Administrator.
2. Use the following command (cmdlet) to enable running remotely signed scripts (note: all PepperDash
    scripts utilize Crestron cmdlets which are signed): “Set-ExecutionPolicy RemoteSigned”
3. 3. Enter “Y” in the field to accept the execution policy

## Pre-Script Execution Guide


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 9 of 20
```
#### Step 2) Create initial folder structure

1. Create a new folder on your desktop for running PepperDash scripts. Call this new folder something easily
    referenced (typically this folder is called, ‘PepperDash-Scripts’.)
2. Within the ‘PepperDash-Scripts’ folder there should be sub-folder called, ‘Scripts’ where the actual
    PowerShell scripts reside.
3. If you desire to run a PepperDash script unattended or desire to load files to a Crestron controller, then
    create another folder within the ‘PepperDash Scripts’ root folder called, ‘Packages’. Running PepperDash
    scripts unattended requires the use of a JSON formatted address book file with the file extension of *.pda
    (example: addressBook.pda). The address book file must be a Java-Script-Object-Notation formatted file
    oalso known as JSON. Example address books will be provided.
4. No need to create the ‘Logs’ folder as this folder and any associated logs will be generated automatically.


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 10 of 20
```
#### Update Crestron Control Processor or Touch Panel Firmware (attended)

#### General Disclaimer

_PepperDash does not recommend updating Crestron control processor or Touchpanel firmware unattended and
therefore will not be covered in this document._

1. Place firmware PUF file in the ‘Packages’ folder.
2. Navigate to provided PowerShell script titled, ‘PDT.PowerShell.LoadScript.PS1’ (Note: Script file is located
    within the ‘Scripts’ folder).
3. If you are running the scripts attended, hold shift and right-click in the File window (not on the actual
    script file) and select “Open PowerShell window here”.

## Script Execution > Update Firmware


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 11 of 20

4. To run the script, type “.\” in the console and the _name of the desired PowerShell script followed by_ enter
    key.
5. If script discovers multiple files possible to load to the processor or touch panel the script will query for a
    choice of files to load.
6. Enter a value for the desired file selection. The example above allows for a value of either ‘1’ or ‘2’. _Note:_
    _At this prompt you are not able to load multiple files to a Processor or Touch Panel. Should you desire to_
    _load multiple files to multiple locations in a Crestron control processor, please utilize the separate_
    _PowerShell script titled, ‘CreateVersions’ for this purpose._ Please note that use of the CreateVersions
    script is not covered in this document.
7. If prompted with “Is this for a Touchpanel or Processor, Enter the device name of either ‘Processor’ or
    Touchpanel’ exactly as it is shown in the PowerShell. Continue with this for all following questions.
8. Provide the ‘device username’ of the processor or touch panel required to connect via SSH. If the device
    credentials or authentication have not been setup the option to choose the default value of ‘Crestron’ is
    allowed by hitting enter when prompted.
9. Provide the ‘device password’ of the Processor or touch panel required to connect via SSH. If the device


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 12 of 20

```
credentials or authentication have not been setup the option to choose the default empty value is
allowed by hitting enter when prompted.
```
10. Provide the device type that files will be loaded to. A value of ‘1’ for Processor or ‘2’ for Touchpanel are
    the only allowed values.
11. The script will prompt asking if the address provided should be added to an existing address book. By
    default, this value should be ‘N’ for no.
12. Wait for confirmation of successful completion.
13. Any errors during the script process will be added to an error text file within the ‘Log’ folder.


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 13 of 20
```
#### Execute Authentication Script (attended)

1. Start Windows PowerShell and navigate to the path of where the scripts are stored or hold shift and right-
    click in the window where the PowerShell script exists. Select the “Open PowerShell window here”
    option.
2. To execute a PowerShell script from the PowerShell command line, type in the full name of the script with
    the prefix “.\”. See example below.
3. Enter the new username desired for the device after the prompt, ‘NewUsername’.
4. Enter the new password desired for the device after the prompt, ‘NewPassword”.
5. If using an address book enter the desired address book from the list or type ‘n’ to skip this selection.
6. If using an address book enter the desired address or ‘a’ for all or ‘n’ to skip.
7. If no address book file (*.pda) is discovered by the script (located in the Packages folder) enter the device
    hostname or IP address.
8. Enter the device current username (Note: If authentication has not yet been setup on the device, the
    username is ‘Crestron’). Hit enter for the script to enter the default ‘Crestron’ username for you.
9. Enter the device current password (Note: If authentication has not yet been setup on the device, the
    password is blank or empty). Hit enter for the script to enter the default empty password for you.
    Script will provide visual indication of the following: Ongoing job running in the background, indication of
    when the job has completed, and will automatically generate logs showing status of authentication script
    for each device the script connects to.

## Script Execution > Enable Authentication


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 14 of 20
```
10. Wait for confirmation of successful completion.
11. Check the ‘Log’ folder for any errors that may have occurred. Typical log entry looks like example below.

#### Execute Authentication Script (unattended)

1. Navigate to the __AuthenticationScript.bat_ file. Note: PepperDash recommends editing JSON and BAT files
    using a free Microsoft tool called, Visual Studio Code. See download link under ‘Recommended Tools’.
2. Right click the file and select “Open with Code” or Visual Studio Code.


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 15 of 20

3. While inside the editor, input all information within the single quotes that pertains to the devices you will
    be working with. Note: PepperDash recommends removing all but a single address book within the
    “Packages” folder preventing utilization of wrong address book variables (“SelectAddrBook” and
    “SelectAddress”).
4. Upon completion of editing the variables in the *.bat file, the file is ready to be ran unattended from
    either any Windows ‘Task Scheduler’ or from simply double-clicking the *.bat file manually.
5. To run manually, return to the folder where the *.bat file exists, and double click the *.bat file.
6. A command window will open showing the same prompts as the attended script. However, when using
    the *.bat file, the script enters the values automatically.
7. You will see the script in progress, then a completed “---Done---” Message


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 16 of 20
```
#### Execute Custom Commands Script (attended)

1. Start Windows PowerShell and navigate to the path of where the scripts are stored or hold shift and right-
    click in the window where the PowerShell script exists. Select the “Open PowerShell window here”
    option.
2. To execute a PowerShell script from the PowerShell command line, type in the full name of the script with
    the prefix “.\”. See example below.
3. The script will be looking for the JSON formatted ‘CommandFile’. Enter the file name of the *.json file
    with the custom commands desired to be sent to Processor or Touchpanel. An example of a custom
    command JSON formatted file is provided by PepperDash. Note: Custom commands JSON file must be
    located in the ‘Packages’ folder.
4. Select an address book to pull the IP Address from [1 – 4] or “n” to skip.
5. If skipped, enter the IP Address of the processor you want to run the commands on.
6. Enter the Device Username (Or hit enter if the default username is “crestron”).
7. Enter the Device password (Or hit enter if the default username is empty “”).
8. Select enter and wait for a successful completion message.
9. Check the ‘Log’ folder for any error occurred during script process.

## Script Execution > Custom Commands


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 17 of 20
```
#### Execute Custom Commands Script (unattended)

```
Navigate to the _CustomCommandsScript.bat file. Note: PepperDash recommends editing JSON and BAT
files using a free Microsoft tool called, Visual Studio Code. See download link under ‘Recommended
Tools’.
```
8. Right click the file and select “Open with Code” or Visual Studio Code.
9. While inside the editor, input the file path where the Custom Commands script
    “PDT.PowerShell.CustomCommands.vXX.XX.PS1” exists.


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 18 of 20

10. While inside the editor, input all information within the single quotes that pertains to the devices you will
    be working with. Note: PepperDash recommends removing all but a single address book within the
    “Packages” folder preventing utilization of wrong address book variables (“CommandFile”,
    “SelectAddrBook” and “SelectedAddressEntry”). Custom commands JSON file must be located within the
    ‘Packages’ folder.
11. Upon completion of editing the variables in the *.bat file, the file is ready to be ran unattended from
    either any Windows ‘Task Scheduler’ or from simply double-clicking the *.bat file manually.
12. To run manually, return to the folder where the *.bat file exists, and double click the _*.bat file.
13. A command window will open showing the same prompts as the attended script. However, when using
    the *.bat file, the script enters the values automatically.
14. You will see the script in progress, then a completed “---Done---” Message.
15. Navigate the ‘Log’ folder and open the CustomCommand.txt file to view any errors that may have
    occurred. Example of typical error log file entry is below.


```
PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 19 of 20
```
#### Notice of Ownership and Copyright

The material in which this notice appears is the property of PepperDash Technology Corporation, which claims
copyright under the laws of the United States of America in the entire body of material and in all parts thereof,
regardless of the use to which it is being put. Any use, in whole or in part, of this material by another party
without the express written permission of PepperDash Technology Corporation is prohibited. PepperDash
Technology Corporation reserves all rights under applicable laws.

#### Notice of Confidentiality

The material in which this notice appears is confidential to PepperDash Technology Corporation. If you have been
provided a copy of this material, it is with the understanding that you will not share it with others without the

#### express written consent of PepperDash Technology Corporation.

#### Notice of Trademark and Servicemark

PepperDash®, Sentegy®, AVUX® and AV360® are the marks of PepperDash Technology Corporation, registered
with the U.S. Patent and Trademark Office. PepperDash Portal™, PepperDash Essentials™, and PepperDash
Connect™ are the unregistered marks of PepperDash Technology Corporation. Any use, in whole or in part, of
these marks by another party without the express written permission of PepperDash Technology Corporation is
prohibited.

## Notices


PepperDash-PowerShell-Script-DeploymentDoc_v01.02.docx Public | 20 of 20
PepperDash | 800.377.9112 | [http://www.pepperdash.com](http://www.pepperdash.com)


