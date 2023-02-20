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

.EXAMPLE
Example *.bat file is given in the below three lines.

@echo off
PowerShell.exe -Command "& '%~dp0\PDT.PowerShell.Authentication.ps1' -NewUsername 'admin' -NewPassword 'password' -SelectedAddrBook '1' -SelectedAddress 'a' -SlnDirRelative '\..'"
pause
#######################################################################################>
param (
    [Parameter(Mandatory = $false)][String]$SlnDirRelative = "",
    [Parameter(Mandatory = $true)][String]$NewUsername = "",
    [Parameter(Mandatory = $true)][String]$NewPassword = "",
    [Parameter(Mandatory = $false)][String]$Username = "",
    [Parameter(Mandatory = $false)][String]$Password = "",
    [Parameter(Mandatory = $false)]$SelectedAddrBook,
    [Parameter(Mandatory = $false)]$SelectedAddress,
    [Parameter(Mandatory = $false)]$Exit = ""
)

<#######################################################################################
.DESCRIPTION
Global Variables
#######################################################################################>
[Bool]$Global:Debug = $false
$AddressBookData = ""
$SelectedAddrBookPath = ""
$NetworkAddress = @()
$Global:SolutionDirectory = ""
$timestamp = (Get-Date).ToString("yyyy-MM-dd")
$MaxThreads = 25

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
            Write-Host ("`n" * 1)
        }
}

Add-Folder -Path "$PSScriptRoot\..\Logs"
Add-Folder -Path "$PSScriptRoot\..\Bundles"

<#######################################################################################
.DESCRIPTION
Set Directories
#######################################################################################>
if ($SlnDirRelative.Length) {
    $TempLocalPath = (Get-Item $PSScriptRoot)
    $Global:SolutionDirectory = "$TempLocalPath + $SlnDirRelative"
}
else {
    $Global:SolutionDirectory = (Get-Item $PSScriptRoot).Parent.Parent.FullName
}

$logfile1 = "$PSScriptRoot\..\Logs\AuthenticationScript_$timestamp.log"

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

<#######################################################################################
.DESCRIPTION
Prompts the user for a new device address to add to the address book

.EXAMPLE
Get-NewAddress
#######################################################################################>
function Get-NewAddress {
    $NewAddressAnswer = Read-Host -Prompt 'Input device Hostname or IP address'
    $AddUserName = Read-Host -Prompt 'Enter device username (hit enter for default "crestron")'
    if (!$AddUserName) { $AddUserName = "crestron" }
    $AddPassword = Read-Host -Prompt 'Enter device password (hit enter for default empty password)'
    if ($AddressBooks -And $AddressBooks[$SelectedAddrBook].Exists) { $AddAddressQuestion = Read-Host -Prompt 'Would you like to add this address? (Y = Yes N = No)'}


    if ($AddAddressQuestion -eq "Y") {
        $AddRoomQuestion = Read-Host -Prompt 'What is the name for this address book entry?'
    }
    else {$AddRoomQuestion = ""}

    $NewAddressBookData = @() ## This is creating an empty array
    $NewAddressBookData += $AddressBookData ## The += Appends
    $NewAddress = [PdaAddress]@{
        Name          = $AddRoomQuestion
        Configuration = $Configuration
        Device        = $DeviceType
        Address       = $NewAddressAnswer
        Username      = $AddUserName
        Password      = $AddPassword
    }
    if ($AddAddressQuestion -eq "Y") {
        $NewAddressBookData += $NewAddress
        if ($AddressBooks[$SelectedAddrBook].Exists) {$NewAddressBookData | ConvertTo-Json | Tee-Object $AddressBooks[$SelectedAddrBook].FullName | out-null}
    }
    return $NewAddress
}

$ScriptBlock = {
    Param ($newDevice, $newUser, $newPass, $logfile)

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
        $Line = "$Stamp,$L,$M"

        If ($mtx.WaitOne(5000)) 
        {
            If ($log) {
                #Add-Content -Path $log -value $Line
                $Line | Out-File $log -Append -encoding ASCII
            }
            Else {
                Write-Output $Line
            }
            [void]$mtx.ReleaseMutex()
        }
        Else
        {
            Write-Warning "Timed out acquiring mutex!"
        }
    }

    Function Set-Authentication {
        [CmdletBinding()]
        param(
            [Parameter(Mandatory = $true, ValueFromPipeLine = $true)]
            $Device,

            [Parameter(Mandatory = $false)]
            [AllowEmptyString()]
            $NewUsername1,

            [Parameter(Mandatory = $false)]
            [AllowEmptyString()]
            $NewPassword1,

            [parameter(Mandatory = $false)]
            [AllowNull()]
            [AllowEmptyString()]
            $Username,

            [parameter(Mandatory = $false)]
            [AllowNull()]
            [AllowEmptyString()]
            $Password
        )


        try {
            if ($Global:Debug) {
                Write-Host ('$' * 80)
            }
            else {
                Clear-Host;
            }
            # local variables
            [bool]$ret = $false
            [string]$x = ''
            
            $name = $Device.name
            $address = $Device.address

            # open a session
            # Write-Log -M "Opening a session for $Device" -log $logfile
            try {
                if ($Password) {$s = Open-CrestronSession -Device $address -Secure -Username $Username -Password $Password -ErrorAction 0 -WarningAction 0}
                else {$s = Open-CrestronSession -Device $address -Secure -Username $Username -ErrorAction 0 -WarningAction 0}
            }
            catch {
                $s = Open-CrestronSession -Device $address -Secure -Username "Crestron" -ErrorAction 0 -WarningAction 0
                $x = $_.Exception.GetBaseException().Message
            }

            if ($s) {
                # turn on authentication
                $x = Invoke-CrestronSession $s 'AUTHENTICATION'
                # remove all spaces and tabs
                $x = $x -replace ' ','' -replace "`t",''
                if($Global:Debug){Write-Host -M "Proc Response to AUTHENTICATION: $x"}

                if ($x.ToLower().Contains('authentication:on')) {
                    Write-Log -M "$Name, $Address, Authentication already enabled, closing connection" -log $logfile
                        $ret = $true
                        return $ret
                }
                if($Global:Debug){Write-Hot "Post Auth-On request" -log $logfile}

                # authentication currently off, now what?
                if ($x.ToLower().Contains('authentication:off')) {
                    Write-Log -M "$Name, $Address, Authentication is off. Enabling now." -log $logfile
                    $x = Invoke-CrestronSession $s 'AUTHENTICATION ON' -Prompt ':'
                    if($Global:Debug){Write-Host $x}

                    # Either enter you current admin's creds or jump to IF to create local admin creds
                    if ($x.ToLower().Contains("enter your administrator's credentials")) {
                        $x = $x -replace ' ','' -replace "`t",''
                        if ($x.ToLower().Contains('username')) {
                            Write-Log -M "$Name, $Address, Post-1, Entering new username..." -log $logfile
                            $x = Invoke-CrestronSession $s "$NewUsername1" -Prompt ':'
                            if($Global:Debug){Write-Log -M "$Name, $Address, Post-1, NewUsername1 submission. Device RX: $x" -log $logfile}
                            if($Global:Debug){Write-Host $x}
                            if ($x.ToLower().Contains('password')) {
                                if($Global:Debug){Write-Log -M "$Name, $Address, Post-1, Entering new password..." -log $logfile}
                                $x = Invoke-CrestronSession $s "$NewPassword1"
                                if($Global:Debug){Write-Log -M "$Name, $Address, Post-1, NewPassword1 submission. Device RX: $x" -log $logfile}
                                Close-CrestronSession $s

                                if ($x.ToLower().Contains('reboot to')) {
                                    Write-Log -M "$Name, $Address, Post-1, Authentication enabled, reboot required, rebooting devcice" -log $logfile
                                    $x = Reset-CrestronDevice -Device $address -NoWait -Secure:$true -Username $NewUsername1 -Password $NewPassword1
                                    $ret = $true
                                    return $ret                          
                                }
                                else{
                                    Write-Log -M "$Name, $Address, Post-1, Reboot not required, Authentication enabled" -log $logfile
                                    $ret = $true
                                    return $ret
                                }
                            }
                        }
                    }

                    # Create local admin creds
                    if ($x.ToLower().Contains('create a local administrator account')) {
                        if ($x.ToLower().Contains('username')) {
                            if($Global:Debug){Write-Log -M "$Name, $Address, Entering new username: $NewUsername1" -log $logfile}
                            $x = Invoke-CrestronSession $s "$NewUsername1" -Prompt ':'
                            if($Global:Debug){Write-Log -M "$Name, $Address, Post-2, NewUsername submission. Device RX: $x" -log $logfile}
                            if ($x.ToLower().Contains('password')) {
                                if($Global:Debug){Write-Log -M "$Name, $Address, Entering new password: $NewPassword1" -log $logfile}
                                $x = Invoke-CrestronSession $s "$NewPassword1" -Prompt ':'
                                if($Global:Debug){Write-Log -M "$Name, $Address, Post-2, NewPassword submission. Device RX: $x" -log $logfile}

                                $x = $x -replace ' ','' -replace "`t",''
                                if($Global:Debug){Write-Host -M "$Name, $Address, Verify Password Device RX: $x"}

                                if ($x.ToLower().Contains('verifypassword')) {
                                    if($Global:Debug){Write-Log -M "$Name, $Address, Verifying password: $NewPassword1" -log $logfile}
                                    $x = Invoke-CrestronSession $s "$NewPassword1"
                                    Close-CrestronSession $s

                                    if ($x.ToLower().Contains('reboot to')) {
                                        Write-Log -M "$Name, $Address, Authentication enabled, reboot required, rebooting devcice" -log $logfile
                                        $x = Reset-CrestronDevice -Device $address -NoWait -Secure:$true -Username $NewUsername1 -Password $NewPassword1
                                        $ret = $true
                                        return $ret                          
                                    }
                                    else{
                                        Write-Log -M "$Name, $Address, Reboot not required, Authentication enabled" -log $logfile
                                        $ret = $true
                                        return $ret
                                    }
                                }
                            }
                        }
                    }
                    else {
                        Write-Log -L 'ERROR' -M "$Name,$Address,Unexpected response to auth on request: $s" -log $logfile
                        $ret = $false
                        return $ret
                    }
                }
                else {
                    Write-Log -L 'ERROR' -M "$Name,$Address,Unexpected response to auth on request: $s" -log $logfile
                    $ret = $false
                    return $ret
                }

            } 
            else {
                Write-Log -L 'ERROR' -M "$Name,$Address,Unable to connect to device" -log $logfile
            }
        }
        catch {
            $x = $_.Exception.GetBaseException().Message
            Write-Log -L 'ERROR' -M "$Name,$Address,Failure for device: $x" -log $logfile
        }
        finally {
            # close any opened sessions
            if (Test-Path variable:\s)
            {Close-CrestronSession $s -ErrorAction SilentlyContinue}

            if (-not $ret) {Write-Warning $x}
        }
    }

    try{
        if($Global:Debug){Write-Log -M "$Name, $Address, Set-Authentication End-Of-ScriptBlock" -log $logfile}
        Set-Authentication -Device $newDevice -Username $newDevice.Username -Password $newDevice.Password -NewUsername $NewUser -NewPassword $NewPass
    }
    catch{
        $x = $_.Exception.GetBaseException().Message
            Write-Log -L 'ERROR' -M "$Name,$Address,Failure for device: $x" -log $logfile
    }
    
    if ($Global:Debug) {
        if($Global:Debug){Write-Log -M "$Name, $Address, End-Of-ScriptBlock" -log $logfile}
        Write-Host ('$' * 15)
    }
}

#region Select Addresses
$AddressBooks = Get-ChildItem -Include *.pda -Path $Global:SolutionDirectory -Recurse ## This get any PDA file
$AddressBookData = @()
$count = 1
if ($AddressBooks.Exists) {
    Write-Output "---------------------"
    foreach ($AddressBook in $AddressBooks) {
        write-output ([string]$count + ": " + $AddressBook.Name)
        $count = $count + 1
    }
    Write-Output "---------------------"
    while ((($SelectedAddrBook -lt 1) -or ($SelectedAddrBook -gt ($count - 1))) -and ($SelectedAddrBook -ne "n")) {
        $SelectedAddrBook = Read-Host ("Select an address book or type n to skip [1 - " + ($count - 1) + "]")
    }

    if ($SelectedAddrBook -eq "n") {$NetworkAddress = Get-NewAddress}
    else {
        $SelectedAddrBook = $SelectedAddrBook - 1
        $SelectedAddrBookPath = $AddressBooks[$SelectedAddrBook].FullName
        $AddressBookData = Get-Content -Path $SelectedAddrBookPath | ConvertFrom-Json
        Write-Output "---------------------"
            
        $count = 1
        foreach ($Address in $AddressBookData) {
            write-output ([string]$count + ": " + $Address.Name + " <" + $Address.Address + ">")
            $count = $count + 1
        }

        Write-Output "---------------------"
        $SelectedAddress = Read-Host ("Select an address [1 - " + ($count - 1) + (", a for all, n for new address] (Use commas between multiple)"))
        $SelectedAddress = $SelectedAddress.Split(",")
                
        #If user entered 'n', ask for new address
        if ($SelectedAddress -eq "n") {
            $NetworkAddress += Get-NewAddress
        }
        #If user entered 'a', add all addresses that were printed out to task list
        elseif ($SelectedAddress -eq "a") {
            $NetworkAddress = $AddressBookData
        }
        #Otherwise, add each address to the task list individually
        else {
            foreach ($SelectedAdd in $SelectedAddress) {
                if ($SelectedAdd -ge 0) {
                    $index = $SelectedAdd - 1
                    $NetworkAddress += $AddressBookData[$index]
                }
                else {
                    Write-Output ("Error: Incorrect address selection: " + $SelectedAdd)
                }
            }
        }
    }
}    
else {$NetworkAddress = Get-NewAddress}
#endregion

foreach ($deviceItem in $NetworkAddress) {
    #Run script block above in parallel operations for each device
    write-output "forEach deviceItem in NetworkAddress"
    Start-Job $ScriptBlock -Name $deviceItem.name -ArgumentList $deviceItem, $NewUsername, $NewPassword, $logfile1
    Start-Sleep -Milliseconds 500
    while($(Get-Job -State 'Running').Count -ge $MaxThreads) #Only allow up to a certain number of threads at once
    {
        Start-Sleep 1
    }
    
    Get-Job -State Completed | ForEach-Object{
        $output = Receive-Job $_
        $_ | Remove-Job
    
        if($output -match $true)
        {
            #Add new username and password to address book
            if ($SelectedAddrBookPath -match ".pda") {
                $AddressBookData | ForEach-Object {if ($_.Address -match $deviceItem.address) {
                    write-host "Saving new address entry"
                    $_.Username = $NewUsername; $_.Password = $NewPassword}}                
            }
        }
    }
}
# Wait for it all to complete
While (Get-Job -State "Running") {
    Start-Sleep 1
}

Get-Job -State Completed | ForEach-Object{
    Write-Output $_
    $_ | Receive-Job
    $_ | Remove-Job
    if($output -match $true)
    {
        #Add new username and password to address book
        if ($SelectedAddrBookPath -match ".pda") {
            $AddressBookData | ForEach-Object {if ($_.Address -match $deviceItem.address) {
                write-host "Saving new address entry"
                $_.Username = $NewUsername; $_.Password = $NewPassword}}                
        }
    }
}
        
# Save any address book changes
if($SelectedAddrBookPath){
    $AddressBookData | ConvertTo-Json | set-content $SelectedAddrBookPath
}

Write-Output "-----Done-----"