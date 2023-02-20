## ********************************************************************************************
## Notice of Ownership and Copyright
##
## The material in which this notice appears is the property of PepperDash Technology Corporation,
## which claims copyright under the laws of the United States of America in the entire body of
## material and in all parts thereof, regardless of the use to which it is being put.  Any use,
## in whole or in part, of this material by another party without the express written permission
## of PepperDash Technology Corporation is prohibited.
##
## PepperDash Technology Corporation reserves all rights under applicable laws.
## *******************************************************************************************/

param (
    [Parameter(Mandatory = $false)][String]$PackageDirRelative = "\..\..\_Bundles",
    [Parameter(Mandatory = $false)][String]$AddressbookRelative = "\..\..\_Bundles\",
    [Parameter(Mandatory = $false)][String]$LogsDirRelative = "\..\..\_Bundles\Logs",
    [Parameter(Mandatory = $false)][String]$KillUnusedProgs = "",
    [Parameter(Mandatory = $false)][String]$DeleteNVRAM = "",
    [Parameter(Mandatory = $false)][String]$Configuration = "",
    [Parameter(Mandatory = $false)][String]$Model = "",
    [Parameter(Mandatory = $false)][String]$DeviceTypeQuestion = "",
    [Parameter(Mandatory = $false)][String]$Username = "",
    [Parameter(Mandatory = $false)][String]$Password = "",
    [Parameter(Mandatory = $false)][String]$PreLoadScript = "",
    [Parameter(Mandatory = $false)][String]$PostLoadScript = "",
    [Parameter(Mandatory = $false)][bool]$RunProgLoad = $true,
    [Parameter(Mandatory = $false)]$SelectedAddrBook,
    [Parameter(Mandatory = $false)]$SelectedAddress
)

<#######################################################################################
.DESCRIPTION
variables
#######################################################################################>

[Bool]$Global:Debug = $false
$Global:PackageDirectory = $null
$Global:AddressbookDirectory = $null
$Global:SelectedAddressbook = 0
$MaxThreads = 25

<#######################################################################################
.DESCRIPTION
Arrays & Lists

.NOTE
Lists can be created with one of the following:
'System.Collections.ArrayList'
'System.Collections.Generic.List[System.Object]'
#######################################################################################>
$Global:NetworkAddress = @()
$Apps = @("","","","","","","","","","")
$TPs = @()

<#######################################################################################
.DESCRIPTION
Creates requested folder structure if it does not yet exist

.Example
Add-Folder -Path "$PSScriptRoot\..\Logs"

Note: Above Add-Folder request looks to the current script file location, navigates (up)
a single folder, then creates the 'Log' folder at that location. Nothing happens if
requested folder path already exists.
#######################################################################################>
function Add-Folder
{
    param ($Path)
    $global:foldPath = $null
    foreach($foldername in $path.split("\")) {
        $global:foldPath += ($foldername+"\")
        if (!(Test-Path $global:foldPath)){
            New-Item -ItemType Directory -Path $global:foldPath -Force
            Write-Host "$global:foldPath Folder Created Successfully"
        }
    }
    if ($Global:Debug) {
            Write-Host ("`n" * 4)
            Write-Host ("$" * 80)
            Write-Host "[$($MyInvocation.MyCommand)] Add-Folder contains the following items:"
            foreach ($item in $DirectorySearchList) {
                Write-Host "[$($MyInvocation.MyCommand)]"
            }
            Write-Host ("$" * 80)
            Write-Host ("`n" * 4)
        }
}

Write-Host "---Creating folders---"
Add-Folder -Path "$PSScriptRoot$LogsDirRelative"
Add-Folder -Path "$PSScriptRoot$AddressbookRelative"

<#######################################################################################
.DESCRIPTION
Gets the parent directory of the path provided

.PARAMETER Path
Specifices the path provided to get the parent directory

.EXAMPLE
Get-ParentDirectory -Path (Get-Location).Path

.EXAMPLE
Get-ParentDirectory -Path C:\Projects\CUstomer\CUS001\Code
#######################################################################################>
Function Get-ParentDirectory
{
    Param(
        [Parameter(Mandatory=$true)][String] $Path
    )

    Return (Get-Item $Path).Parent.FullName;
}

<#######################################################################################
.DESCRIPTION
Prompts the user for a new device address to add to the address book

.EXAMPLE
Get-NewAddress
#######################################################################################>
function Get-NewAddress {
    $NewAddressAnswer = Read-Host -Prompt 'Input the device Hostname or IP address'
    $AddUserName = Read-Host -Prompt 'Enter the device username (hit enter for default "Crestron")'
    if ($null -eq $AddUserName) {
        $AddUserName = "Crestron"
    }
    $AddPassword = Read-Host -Prompt 'Enter the device password (hit enter for default empty password)'

    #Device type selection for processor or touchpanel
    Write-Host "---------------------"
    Write-Host "1: Processor"
    Write-Host "2: Touchpanel"
    Write-Host "---------------------"
    $DeviceTypeQuestion = $null
    do {
        $DeviceTypeQuestion = (Read-Host -Prompt 'What type of device (1-2)')
        }
    until(![String]::IsNullOrEmpty($DeviceTypeQuestion))
    if ($DeviceTypeQuestion = "1")
    {
        $DeviceType = "Processor"
    }
    elseif ($DeviceTypeQuestion = "2")
    {
        $DeviceType = "Touchpanel"
    }
    else {
        Write-Host "Error specifing the device type"`n
        return
    }
     #End device type selection for processor or touchpanel

    $AddAddressQuestion = Read-Host -Prompt 'Would you like to add this address? (Y = Yes N = No)'
    if ($AddAddressQuestion -eq "Y") {
        $AddRoomQuestion = Read-Host -Prompt 'What is the name for this address book entry?'
        $DeviceModelAnswer = $null
        do {
            $DeviceModelAnswer = (Read-Host -Prompt 'What is the device model?')
            }
        until(![String]::IsNullOrEmpty($DeviceModelAnswer))
    }
    else { $AddRoomQuestion = "" }
    $NewAddressBookData = @()
    $NewAddressBookData += $AddressBookData
    $NewAddress = [PdaAddress]@{
        Name          = $AddRoomQuestion
        Configuration = $Configuration
        Device        = $DeviceType
        Model         = $DeviceModelAnswer
        Address       = $NewAddressAnswer
        UserName      = $AddUserName
        Password      = $AddPassword
    }

    if ($AddAddressQuestion -eq "Y") {
        $NewAddressBookData += $NewAddress
        if ($null -eq $SelectedAddrBookPath)
        {
            $NewAddrBookName = Read-Host -Prompt "No addressbooks selected, input the name of the new addressbook"
            $SelectedAddrBookPath = $Global:AddressbookDirectory+$NewAddrBookName+".pda"
            Write-Host "Saving new address book to $SelectedAddrBookPath"
        }

        $Data = $NewAddressBookData | ConvertTo-Json
        Set-Content -Path $SelectedAddrBookPath -Value $Data  -Force
      }
    return $NewAddress
}

<#######################################################################################
.DESCRIPTION
Deletes a directory

.PARAMETER Path
Specificies the directory to delete

.PARAMETER Processor
An object representing the processor that is the FTPServer

.EXAMPLE
DeleteDir -Path $myPath
#######################################################################################>
function DeleteDir {
    param ($Path)
    if (Test-Path $Path) {
        Get-ChildItem -Path $Path -Recurse | Remove-Item -force -Recurse
        Remove-Item $Path -Force
    }
}

<#######################################################################################
.DESCRIPTION
PdaAddress json object definition
#######################################################################################>
class PdaAddress {
    [String]$Name
    [String]$Configuration
    [String]$Device
    [String]$Model
    [String]$Address
    [String]$Username
    [String]$Password
    [String]$Port
}

<#######################################################################################
.DESCRIPTION
Scriptblock to send programs and files to a processor

.PARAMETER ArgumentList
Comma separated values a,b,c where
a = an object representing the processor to send the files to
b = an array of program file paths to send to the processor
c = an array of config file paths to send to the processor

.PARAMETER Name
Names this instance in the scriptblock output list

.EXAMPLE
Start-Job $ProcScriptBlock -ArgumentList $myProcessor, $myApps, $myConfigFiles -Name $myName
#######################################################################################>
$ProcScriptBlock = {
    Param ($proc, $proglist, $configs, $firmware, $postScript)

    $port = 22

    if($proc.Port -ne $null)
    {
        $port = $proc.Port
    }
    #Open Session#
    $session = Open-CrestronSession $proc.Address -Port $port -Secure -Username $proc.UserName -Password $proc.Password -ErrorAction 0 -WarningAction 0
    if ($session) {
        #Send Firmware#
        if($firmware.Length -gt 0)
        {
            Write-Output "Loading firmware to $($proc.Address) $($port) ($($proc.Name))"
            'cd romdisk\user\system', 'delete *.puf' | Invoke-CrestronCommand -Device $proc.Address -Secure -Username $proc.UserName -Password $proc.Password | Out-Null
            Send-CrestronFirmware -Device $proc.Address -Port $port -LocalFile $firmware.FullName -Secure -Username $proc.UserName -Password $proc.Password | Out-Null
            Invoke-CrestronCommand 'puf' -Device $proc.Address -Port $port -Secure -Username $proc.UserName -Password $proc.Password | Out-Null
        }

        #Load Files#
        if($configs.Length -gt 0)
        {
            Write-Output "Loading files to $($proc.Address) ($($proc.Name))"
            if ($DeleteNVRAM -eq "true") {
                'cd \nvram', 'delete *' | Invoke-CrestronCommand -Device $proc.Address -Secure -Username $proc.UserName -Password $proc.Password | Out-Null
            }

            foreach ($item in $configs)
            {
                try
                {
                    $path =  $item.FullName.Substring($Item.FullName.IndexOf("\Processor") + 10)
                    if($item.attributes -match 'Directory')
                    {
                        if (!$path.StartsWith("\Program","CurrentCultureIgnoreCase") -and !$path.EndsWith("\User","CurrentCultureIgnoreCase"))
                        {
                            # $CrestronSession = Open-CrestronSession $proc.Address -Secure -Username $proc.UserName -Password $proc.Password
                            Invoke-CrestronSession $session ('cd ' + $path)
                            if ($DeleteNVRAM -eq "true")
                            {
                                Invoke-CrestronSession $session 'cd ' + $path
                                Invoke-CrestronSession $session 'Del *'
                                Invoke-CrestronSession $session ('REMOVEDIR ' + $path)
                            }
                            Invoke-CrestronSession $session ('MAKEDIR ' + $path)
                            # Close-CrestronSession $session
                        }
                    }
                    else
                    {
                        Send-FTPFile $proc.Address -Port $port -LocalFile $item.FullName -RemoteFile $path -Secure -Username $proc.UserName -Password $proc.Password | Out-Null
                    }
                }
                catch
                {
                    Write-Output "Error loading $item.Fullname to $($proc.Address) ($($proc.Name))"
                }
            }
            if($RunProgLoad)
            {
                Invoke-CrestronSession $session ("progload -p:all") | Out-Null
            }
            if($PostLoadScript.Length -gt 0)
            {
                & "$PSScriptRoot\PDT.PowerShell.CustomCommands.ps1" -CommandFile $PostLoadScript -Addresses $Item
            }

        }
        Write-Output "Load to $($proc.Address) ($($proc.Name)) successful"
        Close-CrestronSession $session 
        if($PostLoadScript.Length -gt 0)
        {
            & "$PSScriptRoot\PDT.PowerShell.CustomCommands.ps1" -CommandFile $PostLoadScript -Addresses $proc
        }
        Out-Null

    }
    else {
        Write-Output "Unable to connect to $($proc.Address) ($($proc.Name))"
    }
}

<#######################################################################################
.DESCRIPTION
Scriptblock to send a touchpanel project to a touchpanel

.PARAMETER ArgumentList
Comma separated values a,b where
a = an object representing the panel to send the project to
b = the path to the project file

.PARAMETER Name
Names this instance in the scriptblock output list

.EXAMPLE
Start-Job $TPScriptBlock -ArgumentList $myTouchpanel, $myPath -Name $myName
#######################################################################################>

$TPScriptBlock = {
    Param ($panel, $panelPath, $firmware)

    $TPport = 22

    if($panel.Port -ne $null)
    {
        $TPport = $panel.Port
    }

    if($panelPath.Length -gt 0)
    {
        Send-CrestronProject -Device $panel.Address -Port $TPport -Secure -LocalFile $panelPath -Username $panel.UserName -Password $panel.Password
    }

    #Send Firmware#
    if($firmware.Length -gt 0)
    {
        'cd romdisk\user\system', 'delete *.puf' | Invoke-CrestronCommand -Device $panel.Address -Secure -Username $panel.UserName -Password $panel.Password | Out-Null
        Send-CrestronFirmware -Device $panel.Address -LocalFile $firmware.FullName -Secure -Username $panel.UserName -Password $panel.Password | Out-Null
        Invoke-CrestronCommand 'puf' -Device $panel.Address -Secure -Username $panel.UserName -Password $panel.Password | Out-Null
    }
}


<#######################################################################################
# Code below will automatically run when the script is executed
#######################################################################################>
try {
    if ($Global:Debug) {
        Write-Host ('$' * 80)
    }
    else {
        Clear-Host;
    }

    # Set the path location
    # this overcomes a path issue when the script is ran
    Set-Location $PSScriptRoot
    $TempLocalPath = (Get-Item $PSScriptRoot)

    # get the project unique directories
    $Global:PackageDirectory = "$TempLocalPath$PackageDirRelative"
    $Global:AddressbookDirectory = "$TempLocalPath$AddressbookRelative"

    if($Global:Debug) { Write-Output "PackageDirectory= $Global:PackageDirectory `nAddressbookDirectory= $Global:AddressbookDirectory"
                        Write-Host ("$" * 80)
                        Write-Host ("`n" * 4)
                      }

    $Packages = Get-ChildItem -Include *.zip -Path $Global:PackageDirectory\*
    $SoloFiles = Get-ChildItem -File -Include *.vtz, *.lpz, *.cpz, *.puf, *json -Path $Global:PackageDirectory\*

    if (!$Packages.Exists) {
        if (!$SoloFiles.Exists) {
            Write-Output "No packages or project files found in $Global:PackageDirectory. Add a package or file and try again."
            Exit
        }
        else { Write-Output "No packages found in $Global:PackageDirectory. Continuing with project files only." }
    }
    else {
        # SLN NAME and Version
        Write-Output "------Packages-------"
        Write-Output $Packages[0].Name.Split("_")[0]
        Write-Output $Packages[0].Name.Split("_")[1]
        Write-Output "---------------------"
    }
    #region Select Package
    $count = 1
    foreach ($Package in $Packages) {
        Write-Output ([string]$count + ": " + $Package.Name)
        $count++
    }
    Write-Output "---------------------"`n
    $countForFiles = $count
    if ($SoloFiles.Exists) {
        Write-Output "-------Files---------"
        foreach ($SoloFile in $SoloFiles) {
            Write-Output ([string]$countForFiles + ": " + $SoloFile.Name)
            $countForFiles++
        }
        Write-Output "---------------------"`n
    }

    if($Global:Debug) { Write-Output "Selection =" $selection }
    if($Global:Debug) { Write-Output "CountForFiles =" $countForFiles }


    ##The reason there is a while loop is to continue to query the user for a valid value
    $selection = 0
    while (($selection -lt 1) -or ($selection -ge ($countForFiles))) {
        [int]$selection = Read-Host ("Select a package or file [1 - " + ($countForFiles - 1) + "]")
    }
    if ($selection -lt $count) { #This is true if the selection is a package
        $SelectedPackage = $selection - 1
        $Configuration = $Packages[$SelectedPackage].Name.Split("_")[2]

        #region Select Device

        # Delete any existing temp folder
        $ThisPackageDir = "$Global:PackageDirectory\temp\" + $Packages[$SelectedPackage].BaseName
        DeleteDir $ThisPackageDir

        Add-Type -AssemblyName System.IO.Compression.FileSystem

        [System.IO.Compression.ZipFile]::ExtractToDirectory($Packages[$SelectedPackage].FullName, $ThisPackageDir)

        #endregion

        #region Define Paths
        $ConfigFiles = Get-ChildItem -Path ($ThisPackageDir + "\Processor\") -Recurse -EA SilentlyContinue

        $App01 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program01\"),($ThisPackageDir + "\Processor\Simpl\App01\")) -Recurse -EA SilentlyContinue
        $App02 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program02\"),($ThisPackageDir + "\Processor\Simpl\App02\")) -Recurse -EA SilentlyContinue
        $App03 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program03\"),($ThisPackageDir + "\Processor\Simpl\App03\")) -Recurse -EA SilentlyContinue
        $App04 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program04\"),($ThisPackageDir + "\Processor\Simpl\App04\")) -Recurse -EA SilentlyContinue
        $App05 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program05\"),($ThisPackageDir + "\Processor\Simpl\App05\")) -Recurse -EA SilentlyContinue
        $App06 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program06\"),($ThisPackageDir + "\Processor\Simpl\App06\")) -Recurse -EA SilentlyContinue
        $App07 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program07\"),($ThisPackageDir + "\Processor\Simpl\App07\")) -Recurse -EA SilentlyContinue
        $App08 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program08\"),($ThisPackageDir + "\Processor\Simpl\App08\")) -Recurse -EA SilentlyContinue
        $App09 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program09\"),($ThisPackageDir + "\Processor\Simpl\App09\")) -Recurse -EA SilentlyContinue
        $App10 = Get-ChildItem -Include *.lpz, *.cpz -Path (($ThisPackageDir + "\Processor\Program10\"),($ThisPackageDir + "\Processor\Simpl\App10\")) -Recurse -EA SilentlyContinue
        $Apps = $App01, $App02, $App03, $App04, $App05, $App06, $App07, $App08, $App09, $App10
        #Get all vtz projects in this package and print to user for selection
        if (Test-Path -Path ($ThisPackageDir + "\Touchpanel"))
        {
            $TPs = Get-ChildItem -Include *.vtz -Path ($ThisPackageDir + "\Touchpanel\") -Recurse
        }
        else
        {
            Write-Output "No touchpanels found"
        }
    }
    else { #Then the selection must be a file and not a package
        $SelectedFile = $SoloFiles[$selection - $count]
        $ThisPackageDir = ""
        If($SelectedFile.Extension.Equals(".vtz"))
        {
            $Model = ""
            while($Model.Length -lt 1) {$Model = Read-Host ("Enter the model name: ")}
            $Configuration = "Touchpanel"
            $ConfigFiles = ""
            $Apps = ""
            $TPs = $SelectedFile
            $Firmware = ""
        }
        ElseIf($SelectedFile.Extension.Equals(".puf"))
        {
            $Configuration = ""
            $Model = ""
            while(!$Configuration.Equals("Touchpanel") -and !$Configuration.Equals("Processor")) {$Configuration = Read-Host ("Is this for a Touchpanel or Processor?: ")}
            $Model = Read-Host ("Enter the model name: ")
            $ConfigFiles = ""
            $Apps = ""
            $TPs = ""
            $Firmware = $SelectedFile
        }
        ElseIf($SelectedFile.Extension.Equals(".lpz") -or $SelectedFile.Extension.Equals(".cpz"))
        {
            $Model = ""
            while(($AppSelection -lt 1) -or ($AppSelection -gt 10)){ [int]$AppSelection = Read-Host ("Select which app to load (1-10): ")}
            $Model = Read-Host ("Enter the model name: ")
            $Configuration = "Processor"
            $ConfigFiles = ""
            $Apps[$AppSelection - 1] = $SelectedFile
            $TPs = ""
            $Firmware = ""
        }
        ElseIf($SelectedFile.Extension.Equals(".json"))
        {
            $Model = ""
            $Configuration = "Processor"
            $ConfigFiles = $SelectedFile
            $Apps = ""
            $TPs = ""
            $Firmware = ""
        }
    }
    #endregion

    if($Global:Debug){Write-Output `n "Checking AddressbookDirectory $Global:AddressbookDirectory `n"}

    #region Select Addresses
    $AddressBooks = Get-ChildItem -Include *.pda -Path $Global:AddressbookDirectory*
    $AddressBookData = @()
    [System.Collections.ArrayList]$SelectedDeviceAddresses = @()
    $count = 1
    if ($AddressBooks.Exists) {
        1
        Write-Output "---------------------"
        foreach ($AddressBook in $AddressBooks) {
            Write-Output ([string]$count + ": " + $AddressBook.Name)
            $count = $count + 1
        }
        Write-Output "---------------------"
        while ((($Global:SelectedAddressbook -lt 1) -or ($Global:SelectedAddressbook -gt ($count - 1))) -and ($Global:SelectedAddressbook -ne "n")) {
            $Global:SelectedAddressbook = Read-Host ("Select an address book or type n to skip [1 - " + ($count - 1) + "]")
        }

        if ($Global:SelectedAddressbook -eq "n") { $Global:NetworkAddress = Get-NewAddress }
        else {
            $Global:SelectedAddressbook = [int]$Global:SelectedAddressbook - 1
            $SelectedAddrBookPath = $AddressBooks[$Global:SelectedAddressbook].FullName
            $AddressBookData = Get-Content -Path $SelectedAddrBookPath | ConvertFrom-Json
            Write-Output "---------------------"

            #Gather all addresses in address book that apply to the selected device and configuration type
            forEach ($Address in $AddressBookData) {
                if ($Address.Configuration -eq $Configuration) {
                    $SelectedDeviceAddresses += $Address
                    $match = 1
                }
                elseif ($Configuration -eq $Address.Device) #For individual files only
                {
                    if($Model.Length -gt 0)
                    {
                        if($Address.Model -contains $Model)
                        {
                            $SelectedDeviceAddresses += $Address
                            $match = 1
                        }
                    }
                    else
                    {
                        $SelectedDeviceAddresses += $Address
                        $match = 1
                    }
                }
            }

            #Only continue if there is a match in this address book for this device and configuration type
            if ($match -eq 1) {
                do {
                    $count = 1
                    foreach ($Address in $SelectedDeviceAddresses) {
                        Write-Output ([string]$count + ": " + $Address.Name + " <" + $Address.Address + ">" + " [" + $Address.Model + "]" + " {" + $Address.Configuration + "}")
                        $count++
                    }
                    Write-Output "---------------------"
                    $SelectedAddress = Read-Host ("Select an address [1 - " + ($count - 1) + (", a for all, n for new address, m to filter by model, c to filter by config] (Use commas between multiple)"))
                    $SelectedAddress = $SelectedAddress.Split(",")

                    #If user entered 'm', ask for filter
                    if ($SelectedAddress -eq "m") {
                        $tempArray= @()
                        $filter = Read-Host ("Enter the model to select: ")
                        foreach($item in ($SelectedDeviceAddresses)){
                            if($item.Model -eq $filter) {
                                $tempArray += $item
                            }
                        }
                        $SelectedDeviceAddresses = $tempArray
                    }
                    #If user entered 'c', ask for filter
                    elseif ($SelectedAddress -eq "c") {
                        $tempArray= @()
                        $filter = Read-Host ("Enter the configuration to select: ")
                        foreach($item in ($SelectedDeviceAddresses)){
                            if($item.Configuration -eq $filter) {
                                $tempArray += $item
                            }
                        }
                        $SelectedDeviceAddresses = $tempArray
                    }
                    #If user entered 'n', ask for new address
                    elseif ($SelectedAddress -eq "n") {
                        $Global:NetworkAddress = Get-NewAddress
                    }
                    #If user entered 'a', add all addresses that were printed out to task list
                    elseif ($SelectedAddress -eq "a") {
                        $Global:NetworkAddress = $SelectedDeviceAddresses
                    }
                    #Otherwise, add each address to the task list individually
                    else {
                        foreach ($SelectedAdd in $SelectedAddress) {
                            if ([int]$SelectedAdd -ge 0) {
                                [int]$index = $SelectedAdd - 1
                                $Global:NetworkAddress += $SelectedDeviceAddresses[$index]
                            }

                            else {
                                Write-Output ("Error: Incorrect address selection: " + $SelectedAdd)
                            }
                        }
                    }
                } while ($Global:NetworkAddress.count -lt 1)
            }
            else {
                Write-Output ("No address book entries match this device and package type")
                $Global:NetworkAddress += Get-NewAddress
            }
        }
    }
    else {
        if($Global:Debug){Write-Output `n "No *.pda files found. Querying user for IP Address... `n"}
        $Global:NetworkAddress = Get-NewAddress
    }

    #endregion

    Write-Output "---------------------"

    #Check if a touchpanel was selected for loading. If so, get the proper file
    foreach ($Item in $Global:NetworkAddress) {
        if ($Item.Device -eq "Touchpanel") {
            if ($TPs.Exists -and ($TPs.count -gt 1)) {
                Write-Output "---------------------"
                $count = 1
                foreach ($TP in $TPs) {
                    Write-Output ([string]$count + ": " + $TP.Name)
                    $count = $count + 1
                }
                Write-Output "---------------------"
                while (($SelectedTP -lt 0) -or ($SelectedTP -gt ($count - 1))) {
                    [int]$SelectedTP = Read-Host ("Select a touchpanel project or select 0 to skip [0 - " + ($count - 1) + "]")
                }
                [int]$SelectedTP = $SelectedTP - 1
                if ($SelectedTP -ge 0) { $TPPath = $TPs[$SelectedTP].FullName }
            }
            elseif ($TPs.Exists -and ($TPs.count -eq 1))
            {
                Write-Output "---------------------"
                Write-Output ("Using touchpanel file: " + $TPs[0].Name)
                Write-Output "---------------------"
                $TPPath = $TPs[0].FullName
            }
            break #only need to select once
        }
    }
    #endregion


    #region Parallel Loading Process
    foreach ($Item in $Global:NetworkAddress) {
        if($PreLoadScript.Length -gt 0)
        {
            & "$PSScriptRoot\PDT.PowerShell.CustomCommands.ps1" -CommandFile $PreLoadScript -Addresses $Item
        }
        #Processor Load
        if ($Item.Device -eq "Processor") {
            #Run script block above in parallel operations for each processor
            #(Testing purposes)
            if($PostLoadScript.Length -gt 0)
            {
                Invoke-Command $ProcScriptBlock -ArgumentList $Item, $Apps, $ConfigFiles, $Firmware, $PostLoadScript
            }
            else 
            {
                Start-Job $ProcScriptBlock -ArgumentList $Item, $Apps, $ConfigFiles, $Firmware, $PostLoadScript -Name $Item.Name
            }
        }

        #Touchpanel Load
        Elseif (($Item.Device -eq "Touchpanel") -and (($TPPath.Length -gt 0 ) -or ($Firmware.Length -gt 0))) {
            #Run script block above in parallel operations for each touchpanel
            #(Testing purposes)
            #Invoke-Command $TPScriptBlock -ArgumentList $Item, $TPPath, $Firmware
             Start-Job $TPScriptBlock -ArgumentList $Item, $TPPath, $Firmware -Name $Item.Name 
        }


        Start-Sleep -Milliseconds 500
        while ($(Get-Job -State 'Running').Count -ge $MaxThreads) {
            #Only allow up to a certain number of threads at once
            Start-Sleep 1
        }

        Get-Job -State Completed | ForEach-Object {
            $_ | Receive-Job
            $_ | Remove-Job
        }
    }
    #endregion

    # Wait for it all to complete
    While (Get-Job -State "Running") {
        Start-Sleep 1
    }

    Get-Job -State Completed | ForEach-Object {
        $_ | Receive-Job
        $_ | Remove-Job
    }
    If ($ThisPackageDir.Exists) { DeleteDir $ThisPackageDir }
    Write-Output "-----Done-----"
}
catch {
    # output the last error message
    "Error: $($Error[0].Exception.Message)";
    If ($ThisPackageDir.Exists) { DeleteDir $ThisPackageDir }
}
finally {
    #delete temp folder
    If ($ThisPackageDir.Exists) { DeleteDir $ThisPackageDir }
}

#endregion



