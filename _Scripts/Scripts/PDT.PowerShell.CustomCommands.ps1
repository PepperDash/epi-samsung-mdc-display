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

<#######################################################################################
.DESCRIPTION
Parameters below define *.bat or command line script execution parameter definitions.
The definitions allow for attended or unattended script execution. Be sure to review
each variable below as all but the 'SlnDirRelative' is required for unattended execution.
Leaving parameter variables undefined may result with the script waiting for user response.
#######################################################################################>
param (
    [Parameter(Mandatory = $false)][String]$SlnDirRelative = "\..\..\",
    [Parameter(Mandatory = $true)][String]$CommandFile = "CustomCommands.json",
    [Parameter(Mandatory = $false)]$Addresses,
    [Parameter(Mandatory = $false)]$SelectedAddrBook,
    [Parameter(Mandatory = $false)]$SelectedAddressEntry,
    [Parameter(Mandatory = $false)]$CustomScriptsDirRelative = "..\..\_CustomScripts"
)

<#######################################################################################
.DESCRIPTION
Global Variables
#######################################################################################>
$MaxThreads = 25
[Bool]$Global:Debug = $true
$AddressBookData = ""
$SelectedAddrBookPath = ""
$NetworkAddress = @()
$ContainsProc = "false"
$ContainsTP = "false"
$Global:SolutionDirectory = ""
$Global:CommandFilePath = ""
$timestamp = (Get-Date).ToString("yyyy-MM-dd_HH-mm")
$Global:CustomScriptsDirectory = ""
$Global:LogDirectory = ""

<#######################################################################################
.DESCRIPTION
Creates requested folder structure if it does not yet exist

.Example
Add-Folder -Path "$PSScriptRoot\..\Logs"

Note: Above Add-Folder request looks to the current script file location, navigates (up)
a single folder, then creates the 'Log' folder at that location. Nothing happens if
requested folder path already exists.
#######################################################################################>


function Add-Folder {
    param ($Path)
    $global:foldPath = $null
    foreach($foldername in $path.split("\")) {
        $global:foldPath += ($foldername+"\")
        if (!(Test-Path $global:foldPath)){
            New-Item -ItemType Directory -Path $global:foldPath -Force
            Write-Host "$global:foldPath Folder Created Successfully"
        }
    }
}

Set-Location $PSScriptRoot
$TempLocalPath = (Get-Item $PSScriptRoot)

$Global:CustomScriptsDirectory = "$TempLocalPath\$CustomScriptsDirRelative"
$Global:LogDirectory = "$TempLocalPath\$CustomScriptsDirRelative\Logs"

Add-Folder -Path $Global:CustomScriptsDirectory
Add-Folder -Path $Global:LogDirectory

$Global:CustomScriptsDirectory = (Get-Item $Global:CustomScriptsDirectory)
$Global:LogDirectory = (Get-Item $Global:LogDirectory)

<#######################################################################################
.DESCRIPTION
Set Directories
#######################################################################################>
if ($SlnDirRelative.Length) {
    $Global:SolutionDirectory = "$TempLocalPath + $SlnDirRelative"
}
else {
    $Global:SolutionDirectory = (Get-Item $PSScriptRoot).Parent.Parent.FullName
}

if ($CommandFile.Length)
{
    $Global:CommandFilePath = "$Global:CustomScriptsDirectory\$CommandFile"
    Write-Output $Global:CommandFilePath

}

$logFile = "$Global:LogDirectory\CustomScript_$timestamp.log"

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
}

class CommandList {
    [String]$Command
    [String]$Response
    [String]$Device
    [String]$LogResponse
}



$commandList = Get-Content -Path $Global:CommandFilePath | ConvertFrom-Json
foreach ($item in $commandList) {
    if ($item.Device -eq "All") {
        $ContainsProc = "true"
        $ContainsTP = "true"
    }
    elseif ($item.Device -eq "Processor") { $ContainsProc = "true" }
    elseif ($item.Device -eq "Touchpanel") { $ContainsTP = "true" }
}

<#######################################################################################
.DESCRIPTION
Prompts the user for a new device address to add to the address book

.EXAMPLE
Get-NewAddress
#######################################################################################>
function Get-NewAddress {
    do
    {
        $DeviceType = Read-Host -Prompt 'Input the device type: P = Processor or T = Touchpanel'
    }
    until($DeviceType -eq "t" -or $DeviceType -eq "p")
    if($DeviceType -eq "t")
    {
        $DeviceType = "Touchpanel";
    }
    else
    {
        $DeviceType = "Processor";
    }

    $NewAddressAnswer = Read-Host -Prompt 'Input the device Hostname or IP address'
    $AddUserName = Read-Host -Prompt 'Enter the device username (hit enter for default "crestron")'
    if (!$AddUserName) { $AddUserName = "crestron" }
    $AddPassword = Read-Host -Prompt 'Enter the device password (hit enter for default empty password)'
    if ($AddressBooks[$SelectedAddrBook].Exists) { $AddAddressQuestion = Read-Host -Prompt 'Would you like to add this address? (Y = Yes N = No)' }


    if ($AddAddressQuestion -eq "Y") {
        $AddRoomQuestion = Read-Host -Prompt 'What is the name for this address book entry?'
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
        if ($AddressBooks[$SelectedAddrBook].Exists) { $NewAddressBookData | ConvertTo-Json | Tee-Object $AddressBooks[$SelectedAddrBook].FullName | Out-Null }
    }
    return $NewAddress
}

$ScriptBlock = {
    Param ($scriptDevice, $cmdList, $scriptLog)

    Function Write-Log {

        [CmdletBinding()]
        Param(
            [Parameter(Mandatory = $False)]
            [ValidateSet("INFO", "WARN", "ERROR", "FATAL", "DEBUG")]
            [String]
            $L = "INFO",

            [Parameter(Mandatory = $True)]
            [string]
            $M,

            [Parameter(Mandatory = $False)]
            [string]
            $log
        )

        $mtx = New-Object System.Threading.Mutex($false, "Mutex")
        $Stamp = (Get-Date).toString("yyyy-MM-dd HH:mm:ss")
        $command = "$Stamp,$L, $M"

        If ($mtx.WaitOne(5000)) {
            If ($log) {
                #Add-Content -Path $log -value $Line
                $command | Out-File $log -Append -encoding ASCII
                $resonse | Out-File $log -Append -encoding ASCII
            }
            Else {
                Write-Output $Line
            }
            [void]$mtx.ReleaseMutex()
        }
        Else {
            Write-Warning "Timed out acquiring mutex!"
        }
    }


    try {
        # local variables
        [string]$x = ''
        $name = $scriptDevice.name
        $address = $scriptDevice.address
        $username = $scriptDevice.username
        $password = $scriptDevice.password

        # open a session
        if ($password) { $s = Open-CrestronSession -Device $address -Secure -Username $username -Password $password -ErrorAction SilentlyContinue }
        else { $s = Open-CrestronSession -Device $address -Secure -Username $username -ErrorAction 0 -WarningAction 0 }

        if ($s) {
            # Connected, start sending custom commands
            $count=0
            foreach ($cmd in $cmdList) {
                if(($cmd.Device -eq "All") -or ($cmd.Device -eq $scriptDevice.Device))
                {
                    if($cmd.Command -eq "reboot")
                    {
                        $response = Invoke-CrestronSession $s -Command $cmd.Command -Timeout 10
                    }
                    else
                    {
                        $response = Invoke-CrestronSession $s -Command $cmd.Command -Prompt '>' -Timeout 180
                    }
                    if ($cmd.Response) {
                        if (!$x.Contains($cmd.Response)) {
                            Write-Log -L "ERROR" -M "$name,$address,FAILED response from $cmd : $response" -log $scriptLog
                        }
                        else {$count++}
                    }
                    else {$count++}
                    if($cmd.LogResponse -eq "true")
                    {
                        Write-Log -M "$name,$address,`nCommand> $($cmd.Command)`n$response" -log $scriptLog

                    }
                }
            }
            #Write-Log -M "$name,$address,Successful" -log $scriptLog
        }
        else {
            Write-Log -L "ERROR" -M "$name,$address,Unable to connect" -log $scriptLog
        }
    }
    catch {
        $x = $_.Exception.GetBaseException().Message
        Write-Log -L "ERROR" -M "$name,$address,Failure : $x" -log $scriptLog
    }
    finally {
        # close any opened sessions
        if ($s) {
            Close-CrestronSession $s -ErrorAction SilentlyContinue
        }
    }
}


if($null -ne $Addresses)
{
    $NetworkAddress = $Addresses
}
else
{
    #region Select Addresses
    $AddressBooks = Get-ChildItem -Include *.pda -Path $Global:SolutionDirectory -Recurse
    $AddressBookData = @()
    [System.Collections.ArrayList]$SelectedDeviceAddresses = @()
    $count = 1
    if ($AddressBooks.Exists) {
        Write-Output "---------------------"
        foreach ($AddressBook in $AddressBooks) {
            Write-Output ([string]$count + ": " + $AddressBook.Name)
            $count = $count + 1
        }
        Write-Output "---------------------"
        while ((($SelectedAddrBook -lt 1) -or ($SelectedAddrBook -gt ($count - 1))) -and ($SelectedAddrBook -ne "n")) {
            $SelectedAddrBook = Read-Host ("Select an address book or type n to skip [1 - " + ($count - 1) + "]")
        }

        if ($SelectedAddrBook -eq "n") { $NetworkAddress = Get-NewAddress }
        else {
            $SelectedAddrBook = $SelectedAddrBook - 1
            $SelectedAddrBookPath = $AddressBooks[$SelectedAddrBook].FullName
            $AddressBookData = Get-Content -Path $SelectedAddrBookPath | ConvertFrom-Json
            Write-Output "---------------------"

            $SelectedDeviceAddresses = $AddressBookData

            do {
                $count = 1
                foreach ($Address in $SelectedDeviceAddresses) {
                    Write-Output ([string]$count + ": " + $Address.Name + " <" + $Address.Address + ">" + " [" + $Address.Model + "]" + " {" + $Address.Configuration + "}")
                    $count++
                }
                Write-Output "---------------------"

                if($SelectedAddressEntry){$SelectedAddress = $SelectedAddressEntry}
                else{
                    $SelectedAddress = Read-Host ("Select an address [1 - " + ($count - 1) + (", a for all, n for new address] {Filter m= By Model, c= By Config} (Use commas between multiple)"))
                }
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
                    $NetworkAddress = Get-NewAddress
                }
                #If user entered 'a', add all addresses that were printed out to task list
                elseif ($SelectedAddress -eq "a") {
                    $NetworkAddress = $SelectedDeviceAddresses
                }
                #Otherwise, add each address to the task list individually
                else {
                    foreach ($SelectedAdd in $SelectedAddress) {
                        if ($SelectedAdd -ge 0) {
                            $index = $SelectedAdd - 1
                            $NetworkAddress += $SelectedDeviceAddresses[$index]
                        }
                        else {
                            Write-Output ("Error: Incorrect address selection: " + $SelectedAdd)
                            Write-Log -M "Error: Incorrect address selection: $SelectedAdd" -log $logfile
                        }
                    }
                }
            } while ($NetworkAddress.count -lt 1)
        }
    }
    else { $NetworkAddress = Get-NewAddress }
}
#endregion

foreach ($deviceItem in $NetworkAddress) {
    if($Global:Debug){write-output "forEach > $deviceItem Item = " + $deviceItem.Device}
    if (($deviceItem.Device -eq "Processor" -and $ContainsProc -eq "true") -or ($deviceItem.Device -eq "Touchpanel" -and $ContainsTP -eq "true")) {

        #Run script block above in parallel operations for each device
        if($Global:Debug){write-output "forEach deviceItem in NetworkAddress"}
        Start-Job $ScriptBlock -Name $deviceItem.name -ArgumentList $deviceItem, $commandList, $logFile
        # Debug
        #Invoke-Command $ScriptBlock -ArgumentList $deviceItem, $commandList, $logFile
        Start-Sleep -Milliseconds 500
        while ($(Get-Job -State 'Running').Count -ge $MaxThreads)
        {
            #Only allow up to a certain number of threads at once
            Start-Sleep 1
        }

        Get-Job -State Completed | ForEach-Object {
            $_ | Receive-Job
            $_ | Remove-Job
        }
    }
}
# Wait for it all to complete
While (Get-Job -State "Running") {
    Start-Sleep 1
}

Get-Job -State Completed | ForEach-Object {
    Write-Output $_
    $_ | Receive-Job
    $_ | Remove-Job
}

Write-Output "-----Done-----"

