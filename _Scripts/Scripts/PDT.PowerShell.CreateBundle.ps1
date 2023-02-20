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
    [Parameter(Mandatory = $false)][Bool]$Headless = $false,
    [Parameter(Mandatory = $false)][Bool]$PromptEveryTime = $false,
    [Parameter(Mandatory = $false)][Bool]$IncludeNet47 = $false,
    [Parameter(Mandatory = $false)][String]$SlnDirRelative = "\..\..\",
    [Parameter(Mandatory = $false)][String]$PackageDirRelative = "..\..\_Bundles",
    [Parameter(Mandatory = $false)][String]$ConfigDirectory = "Processor\User",
    [Parameter(Mandatory = $false)][String]$PluginDirectory = "\Processor\USER\program1\plugins"
)

<#######################################################################################
.DESCRIPTION
variables
#######################################################################################>
[Bool]$Global:Debug = $false
[String[]]$Global:ManifestFiles = $null
[String]$Global:SolutionDirectory = $null
[String]$Global:ParentDir = $null
[String]$Global:Solution = $null
[Int]$Global:VersionMajor = $null
[Int]$Global:VersionMinor = $null
[Int]$Global:VersionCompile = $null
[String]$Global:PackageDirectory = $null
[bool]$Global:PromptEveryTime = $false


# define the prefixes here... if not defined the user will be prompted
# when the script runs to input the prefixes used for searching
[string]$SearchPatternApp = "P"
[string]$SearchPatternConfig = "Config"
[string]$SearchPatternTp = "Touch"
[string]$SearchPatternPlugins = "*Plugins"
[string]$SearchPatternWebProject = "WebProject"

[String]$PackageDirectory = "Bundles"
[String]$PackageSuffix = "Bundle"
[String]$ProgramDirectory = "Processor"
[String]$AppDirectoryPrefix = "Program"

[String]$TouchpanelDirectory = "Touchpanel"
[String]$WebProjectDirectory = "WebProject"

<#######################################################################################
.DESCRIPTION
Hash Table Object Signature
#######################################################################################>
$ListObject = [ordered]@{
    Key  = $null
    Id   = $null
    File = $null
};

<#######################################################################################
.DESCRIPTION
Arrays & Lists

.NOTE
Lists can be created with one of the following:
'System.Collections.ArrayList'
'System.Collections.Generic.List[System.Object]'
#######################################################################################>
$DirectorySearchList = New-Object 'System.Collections.ArrayList'
$FilesFoundList = New-Object 'System.Collections.ArrayList'
$PackageFileList = New-Object 'System.Collections.ArrayList'
$PackageList = New-Object 'System.Collections.ArrayList'

<#######################################################################################
.DESCRIPTION
Prompts user for input using the index

.PARAMETER Index
The index will properly fill in the prompt and uses constant values, see below.

1 = program
2 = config
3 = touchpanel
4 = webProject

.EXAMPLE
Get-UserInput -Index 1

.EXAMPLE
Get-UserInput -Index 3
#######################################################################################>
Function Get-SearchPattern {
    Param(
        [Parameter(Mandatory = $true)][Int] $Index
    )

    do {
        switch ($Index) {
            1 {
                # programs
                $promptText = "program"
                Break
            }
            2 {
                # configs
                $promptText = "config"
                Break
            }
            3 {
                # touchpanels
                $promptText = "touchpanel"
                Break
            }
            4 {
                # webProjects
                $promptText = "webProject"
                Break
            }
            5 {
                # plugins
                $promptText = "plugins"
                Break
            }
        }

        $userInput = Read-Host("Enter the search pattern for the $promptText folder(s)")
    }
    until(![String]::IsNullOrEmpty($userInput))

    Return $userInput
}

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
Function Get-ParentDirectory {
    Param(
        [Parameter(Mandatory = $true)][String] $Path
    )

    Return (Get-Item $Path).Parent.FullName
}

<#######################################################################################
.SYNOPSIS

.DESCRIPTION
Populates the list, $DirectorySearchList

.EXAMPLE
Get-Directories

.NOTES
Example of $DirectorySearchList contents:

Key Id         File
--- --         ----
  0 P01        C:\Projects\Customer\project-solution001\*P01*
  1 P02        C:\Projects\Customer\project-solution001\*P02*
  2 P03        C:\Projects\Customer\project-solution001\*P03*
  3 Config     C:\Projects\Customer\project-solution001\*Config*
  4 Touchpanel C:\Projects\Customer\project-solution001\*Touchpanel*
  5 WebProject  C:\Projects\Customer\project-solution001\*WebProject*
#######################################################################################>
Function Get-Directories {
    Write-Host "Building a list of directories to search...."

    # initialize the list
    $DirectorySearchList.Clear()

    # get the path of the script, go up one-level
    #$parentDir = Get-ParentDirectory -Path $PSScriptRoot;
    #$Global:ParentDir = Get-ParentDirectory -Path (Get-Location);

    # build program search path, "P{0:00}" should cover "P{0:00}" & "this{0:00}"
    for ($i = 0; $i -lt 10; $i++) {
        $thisObject = New-Object PSObject -Property $ListObject
        $thisObject.Key = $i
        $thisObject.Id = "$SearchPatternApp{0:00}" -f ($i + 1)
        $thisObject.File = "$Global:SolutionDirectory\*$($thisObject.Id)*"
        $DirectorySearchList.Add($thisObject)
    }

    # build plugin search path
    $thisObject = New-Object PSObject -Property $ListObject
    $thisObject.Key = $DirectorySearchList.Count + 1
    $thisObject.Id = "$SearchPatternPlugins"
    $thisObject.File = "$Global:SolutionDirectory\*$($thisObject.Id)*"
    $DirectorySearchList.Add($thisObject)

    # build config search path
    $thisObject = New-Object PSObject -Property $ListObject
    $thisObject.Key = $DirectorySearchList.Count + 1
    $thisObject.Id = "$SearchPatternConfig"
    $thisObject.File = "$Global:SolutionDirectory\*$($thisObject.Id)*"
    $DirectorySearchList.Add($thisObject)

    # build touchpanel search path
    $thisObject = New-Object PSObject -Property $ListObject
    $thisObject.Key = $DirectorySearchList.Count + 1
    $thisObject.Id = "$SearchPatternTp"
    $thisObject.File = "$Global:SolutionDirectory\*$($thisObject.Id)*"
    $DirectorySearchList.Add($thisObject)

    # build webProject search path
    $thisObject = New-Object PSObject -Property $ListObject
    $thisObject.Key = $DirectorySearchList.Count + 1
    $thisObject.Id = "$SearchPatternWebProject"
    $thisObject.File = "$Global:SolutionDirectory\*$($thisObject.Id)*"
    $DirectorySearchList.Add($thisObject)

    if ($Debug) {
        Write-Host ("`n" * 4)
        Write-Host ("$" * 80)
        Write-Host "[$($MyInvocation.MyCommand)] DirectorySearchList contains the following items:"
        foreach ($item in $DirectorySearchList) {
            Write-Host "[$($MyInvocation.MyCommand)] $($item.Id) : $($item.File)"
        }
        Write-Host ("$" * 80)
        Write-Host ("`n" * 4)
    }
}

<#######################################################################################
.SYNOPSIS

.DESCRIPTION
Uses $DirectorySearchList.File to search the paths found and populates the $FilesFoundList with any
file that matches the include criteria.

The include criteria is set to:
Code Files: *.lpz, *.cpz
Config Files: *any*
Touchpanel Files: *.vtz
WebProject Files: *.zip

.EXAMPLE
Get-Files

.NOTES
Example of $FilesFoundList contents:

Key Id         File
--- --         ----
  1 P01        C:\Projects\Customer\typical.sln001\tw.typical.p01-main\PROJ001-Typical_P01_AV3_v00.27.lpz
  2 P01        C:\Projects\Customer\typical.sln001\tw.typical.p01-main\PROJ001-Typical_P01_CP3N_v00.27.lpz
  3 P02        C:\Projects\Customer\typical.sln001\tw.typical.p02-routing.01-hd-md\PROJ001-Routing_HD-MD_P02_RMC3_v00.03.lpz
  4 P03        C:\Projects\Customer\typical.sln001\tw.typical.p03-devices\PROJ001-Devices_P03_CP3N_v00.09.lpz
  5 Config     C:\Projects\Customer\typical.sln001\tw.typical.configs\T02-Typical\PROJ001-Typical_SYS_Config_v00.15.json
  6 Config     C:\Projects\Customer\typical.sln001\tw.typical.configs\_Template\PROJ001-Typical_SYS_Config_v00.15.json
  7 Touchpanel C:\Projects\Customer\typical.sln001\tw.typical-t02.touchpanel.01-dge\PROJ001_Customer_T02_DGE_v00.02.vtz
  8 Touchpanel C:\Projects\Customer\typical.sln001\tw.typical-t02.touchpanel.02-1060\PROJ001_Customer_T02_v00.20.vtz
#######################################################################################>
Function Get-Files {
    Write-Host "Buidling a list of files matching the search criteria...."

    # initialize the list
    $FilesFoundList.Clear();

    # use the directory list as the search path
    foreach ($item in $DirectorySearchList) {
        # config directory search
        if ($item.Id -like $SearchPatternConfig) {
            if ($item.Id -eq $SearchPatternConfig ) {
                $files = Get-ChildItem -Path $item.File -Directory -Include * -Exclude ("sftp-config.json")
            }
            else {
                $files = Get-ChildItem -Path $item.File -Directory -Recurse -Exclude ("sftp-config.json")
            }
            foreach ($file in $files) {
                $thisObject = New-Object PSObject -Property $ListObject
                $thisObject.Key = $FilesFoundList.Count + 1
                $thisObject.Id = $item.Id
                $thisObject.File = $file
                $FilesFoundList.Add($thisObject)
            }
        }
        #webProject file search
        elseif ($item.Id -eq $SearchPatternWebProject) {
            $files = Get-ChildItem -Path $item.File -Include "*.zip" -Recurse
            foreach ($file in $files) {
                $thisObject = New-Object PSObject -Property $ListObject
                $thisObject.Key = $FilesFoundList.Count + 1
                $thisObject.Id = $item.Id
                $thisObject.File = $file
                $FilesFoundList.Add($thisObject)
            }
        }
        #plugins search
        elseif ($item.Id -eq $SearchPatternPlugins) {
            # set the include criteria
            $files = Get-ChildItem -Path $item.File -Include '*.cplz' -Recurse
            foreach ($file in $files) {
                if ($file.DirectoryName.Contains("net47") -and $IncludeNet47 -eq $false) {
                    # Skip
                }
                else {
                    $thisObject = New-Object PSObject -Property $ListObject
                    $thisObject.Key = $FilesFoundList.Count + 1
                    $thisObject.Id = $item.Id
                    $thisObject.File = $file
                    $FilesFoundList.Add($thisObject)
                }
            }
        }
        # compiled file search
        else {
            # set the include criteria
            $include = ('*.lpz', '*.cpz', '*.vtz') # "*archive.zip", "*.vta", "*compiled.zip")

            $files = Get-ChildItem -Path $item.File -Include $include -Recurse -Exclude ("*.hash", "*.asv")
            foreach ($file in $files) {
                if ($file.DirectoryName.Contains("net47") -and $IncludeNet47 -eq $false) {
                    # Skip
                }
                else {
                    $thisObject = New-Object PSObject -Property $ListObject
                    $thisObject.Key = $FilesFoundList.Count + 1
                    $thisObject.Id = $item.Id
                    $thisObject.File = $file
                    $FilesFoundList.Add($thisObject)
                }
            }
        }
    }

    if ($Debug) {
        Write-Host ("`n" * 4)
        Write-Host ("$" * 80)
        Write-Host "[$($MyInvocation.MyCommand)] FilesFoundList contains the following items:"
        foreach ($item in $FilesFoundList) {
            Write-Host "[$($MyInvocation.MyCommand)] $($item.Id) : $($item.File)"
        }
        Write-Host ("$" * 80)
        Write-Host ("`n" * 4)
    }
}

<#######################################################################################
.DESCRIPTION
Build a list of the files to include in the package

.PARAMETER  SearchId
String used to search the Id property of the input object

.PARAMETER InputObject
Object to search for matching Id's

.EXAMPLE
Add-PackageList -SearchId P01 -InputObject $FilesFoundList

.EXAMPLE
Add-PackageList -SearchId Config -InputObject $FilesFoundList

.EXAMPLE
Add-PackageList -SearchId Touchpanel -InputObject $FilesFoundList

.EXAMPLE
Add-PackageList -SearchId WebProject -InputObject $FilesFoundList
#######################################################################################>
Function Add-PackageList {
    Param(
        [Parameter(Mandatory = $true)][String] $MenuTitle,
        [Parameter(Mandatory = $true)][String] $SearchId,
        [Parameter(Mandatory = $true)][Object[]] $InputObject
    )

    # initialize the list
    $thisList = New-Object 'System.Collections.ArrayList'
    $allList = New-Object 'System.Collections.ArrayList'
    # iterate through $FilesFoundList to build the user prompt based on the -like criteria
    foreach ($item in $InputObject) {
        if ($item.Id -like $SearchId) {
            # create a temporary object to build the list
            $thisObject = New-Object PSObject -Property $ListObject;
            $thisObject.Key = $thisList.Count + 1
            $thisObject.Id = $item.Id
            $thisObject.File = $item.File
            $thisList.Add($thisObject)
        }
    }

    # continue ONLY if we have a list of files
    if ($thisList.Count -gt 0) {
        # build menu prompt
        Write-Host ("-" * 80)
        if ($thisList.Count -gt 1 -or $Global:PromptEveryTime) {
            # menu title
            Write-Host ("-" * 80)
            Write-Host ("Select the $MenuTitle files to include in the $PackageSuffix")
            Write-Host ("-" * 80)

            # menu items for selection
            foreach ($item in $thisList) {
                Write-Host "$($item.Key): $([IO.Path]::GetFileName($item.File))"
                $allList.Add($item.Key);

            }
            Write-Host ("-" * 80)

            $entries = ""
            # process user input
            if($Headless)
            {
                $entries = "a"
            }
            else
            {
                do {

                    $entries = (Read-Host "Enter the number of the file to include (0 = none, `",`" for multiple sections, a for all)").Split(",")
                }
                until(![String]::IsNullOrEmpty($entries))
            }
            if ($entries -eq "a") {
                $entries = $allList
            }
            else {
                $entries = $entries.Split(" ")
            }
        }
        else {
            $entries = "1"
        }
        foreach ($entry in $entries) {
            [int]$entryInt = $entry
            if (($entryInt -gt 0) -and ($entryInt -le $thisList.Count)) {
                $tempObject = New-Object PSObject -Property $ListObject
                $tempObject.Key = 1
                $tempObject.Id = $item.Id
                $tempObject.File = $item.File

                # This is the issue with creating multiple packages here for some reason its changing the list
                ######
                $tempObject = $thisList | Where-Object { $_.Key -eq $entryInt }
                #######
                # $tempObject.Key = $PackageFileList.Count + 1
                $PackageFileList.Add($tempObject)
            }
        }
    }

    if ($Debug) {
        Write-Host ("`n" * 4)
        Write-Host ("$" * 80)
        Write-Host "[$($MyInvocation.MyCommand)] PackageFileList contains the following items:"
        foreach ($item in $PackageFileList) {
            Write-Host "[$($MyInvocation.MyCommand)] $($item.Id) : $($item.File)"
        }
        Write-Host ("$" * 80)
        Write-Host ("`n" * 4)
    }
}

<#######################################################################################
.SYNOPSIS
.DESCRIPTION
Build a zip package file from the file list
.PARAMETER  <Parameter-Name>
.INPUTS
.OUTPUTS
.EXAMPLE
.LINK
.NOTE
#######################################################################################>
Function Add-Package {
    #Write-Host "Checking for package folder..."

    # check that the package directory exists
    if (!(Test-Path $Global:PackageDirectory)) {
        # if it does not exist, create it
        New-Item -Path $Global:PackageDirectory -ItemType Directory
    }

    foreach ($item in $PackageFileList) {
        if ($item.Id -eq "Config") {
            # determine the package type from the file
            $type = Split-Path -Path $item.File -Leaf
            $thisObject = New-Object PSObject -Property $ListObject
            $thisObject.Key = $PackageList.Count + 1
            $thisObject.Id = $type
            $thisObject.File = "$Global:PackageDirectory\$($Global:Solution)_$($type)_$($PackageSuffix)"
            $PackageList.Add($thisObject)
        }
    }

    foreach ($listItem in $PackageList) {
        # Move old packages to Archive
        $Archive = "$($Global:PackageDirectory)\Archive"
        $TEST = "$($Global:PackageDirectory)\*_$($listItem.Id)_*"
        if (!(Test-Path -path $Archive)) { New-Item $Archive -Type Directory }
        Move-Item -Path $TEST -Destination $Archive

        # build list of paths for current package
        $packageDirs = [ordered]@{};
        $packageDirs.Add($packageDirs.Count + 1, "$($listItem.File)")
        $packageDirs.Add($packageDirs.Count + 1, "$($listItem.File)\$ConfigDirectory")
        $packageDirs.Add($packageDirs.Count + 1, "$($listItem.File)\$TouchpanelDirectory")

        # create the directories
        foreach ($dir in $packageDirs.GetEnumerator()) {
            if (!(Test-Path -Path $($dir.Value) -PathType Container)) {
                New-Item -Path $($dir.Value) -ItemType "directory"
            }
        }

        # copy the files from the package list to the package directories
        foreach ($fileItem in $PackageFileList) {
            #Write-Host "Copying files from $([IO.Path]::GetFileName($fileItem.File)) to $(Split-Path -Path $listItem.File -Leaf)..."
            if ($fileItem.Id -like $SearchPatternConfig) {
                $type = Split-Path -Path $fileItem.File -Leaf
                if ($type -like $listItem.Id) {
                    Copy-Item -Path "$($fileItem.File)\*" -Destination "$($listItem.File)\$ConfigDirectory\"  -Recurse -Exclude "*sftp-config.json"
                }
            }
            elseif ($fileItem.Id -like $SearchPatternTp) {
                Copy-Item -Path $fileItem.File -Destination "$($listItem.File)\$TouchpanelDirectory\$([IO.Path]::GetFileName($fileItem.File))"
            }
            elseif ($fileItem.Id -like $SearchPatternWebProject) {
                Copy-Item -Path $fileItem.File -Destination "$($listItem.File)\$WebProjectDirectory\$([IO.Path]::GetFileName($fileItem.File))"
            }
            elseif ($fileItem.Id -like $SearchPatternPlugins) {
                if (!(Test-Path -Path $("$($listItem.File)\$PluginDirectory") -PathType Container)) {
                    New-Item -Path $("$($listItem.File)\$PluginDirectory") -ItemType "directory"
                }
                Copy-Item -Path $fileItem.File -Destination "$($listItem.File)\$PluginDirectory\$([IO.Path]::GetFileName($fileItem.File))"
            }
            else {
                $include = ('lpz', 'cpz', 'sig');
                for ($i = 1; $i -le 10; $i++) {
                    if ($fileItem.Id -like ("$SearchPatternApp{0:00}" -f $i)) {
                        $fileItemFilename = [IO.Path]::GetFileNameWithoutExtension($fileItem.File)
                        $fileItemDir = [IO.Path]::GetDirectoryName($fileItem.File)

                        if (!(Test-Path -Path $("$($listItem.File)\$ProgramDirectory\$AppDirectoryPrefix{0:00}" -f $i) -PathType Container)) {
                            New-Item -Path $("$($listItem.File)\$ProgramDirectory\$AppDirectoryPrefix{0:00}" -f $i) -ItemType "directory"
                        }

                        foreach ($item in $include) {
                            #Write-Host "Testing for $fileItemDir\$fileItemFilename.$item";
                            if (Test-Path -Path "$fileItemDir\$fileItemFilename.$item") {
                                #Write-Host "Copying $fileItemDir\$fileItemFilename.$item";
                                Copy-Item -Path "$fileItemDir\$fileItemFilename.$item" -Destination ("$($listItem.File)\$ProgramDirectory\$AppDirectoryPrefix{0:00}\$fileItemFilename.$item" -f $i)
                            }
                        }
                    }
                }
            }
        }

        Write-Host "Creating manifest file...`n";
        Write-ManifestFile -Path "$($listItem.File)\Processor\User" -Filename "$($Global:Solution)_$($listItem.Id)_Manifest.pdtc" -Overwrite 1 | Out-Null
        Write-Host "`n";

        Write-Host "Creating package file...`n";
        #Create package as archive
        Compress-Archive -Path "$($listItem.File)\*" -DestinationPath "$($listItem.File).zip" -Force

        #Delete source files after archive created
        if (Test-Path "$($listItem.File)") {
            Get-ChildItem -Path "$($listItem.File)" -Recurse | Remove-Item -force -Recurse
            Remove-Item "$($listItem.File)" -Force
        }
        Write-Host "`n";
    }
}

<#######################################################################################
.DESCRIPTION
Creates a file, if it does not already exist

.PARAMETER Filename
Specifies the file to create

.PARAMETER Overwrite $true(1)/$fales(0)
Optional, will overwrite file if a file exists, $true/$false or 1/0

.EXAMPLE
Write-File -File 'C:\Users\All Users\This-File.txt'

.EXAMPLE
Write-File -File 'C:\Users\All Users\This-File.txt' -Overwrite $true

.EXAMPLE
Write-File -File 'C:\Users\All Users\This-File.txt' -Overwrite 0
#######################################################################################>
Function Write-ManifestFile {
    Param(
        [Parameter(Mandatory = $true)][String] $Path,
        [Parameter(Mandatory = $true)][String] $Filename,
        [Parameter(Mandatory = $false)][Bool] $Overwrite
    )

    if (Test-Path "$Path") {
        if ($Overwrite -eq $true) {
            if (Test-Path "$Path\$Filename") {
                Remove-Item "$Path\$Filename" -Force
            }
        }
    }

    Write-Host "Creating $([IO.Path]::GetFileName($Filename))"
    $FileResults = New-Item -Path $Path -Name $Filename -ItemType File;

    "Title = $Global:Solution" | Out-File -FilePath $FileResults -Append
    "Major = $("{0:000}" -f $Global:VersionMajor)" | Out-File -FilePath $FileResults -Append
    "Minor = $("{0:000}" -f $Global:VersionMinor)" | Out-File -FilePath $FileResults -Append
    "Compile = $("{0:000}" -f $Global:VersionCompile)" | Out-File -FilePath $FileResults -Append
    "CompileTime = $(Get-Date -Format o | ForEach-Object { $_ -replace ":", "." })" | Out-File -FilePath $FileResults -Append
    "Programmer = $env:USERNAME" | Out-File -FilePath $FileResults -Append
    "Package Directory = $(Split-Path -Path $Global:PackageDirectory -Leaf)" | Out-File -FilePath $FileResults -Append

    # programs
    ("`n" * 2) | Out-File -FilePath $FileResults -Append
    "Program Files" | Out-File -FilePath $FileResults -Append
    ("-" * 80) | Out-File -FilePath $FileResults -Append
    foreach ($item in $PackageFileList) {
        for ($i = 1; $i -le 10; $i++) {
            if ($item.Id -like ("$SearchPatternApp{0:00}" -f $i)) {
                $("$AppDirectoryPrefix{0:00}.Version = $([IO.Path]::GetFileName($item.File))") -f $i | Out-File -FilePath $FileResults -Append
            }
        }
    }

    # plugins
    ("`n" * 2) | Out-File -FilePath $FileResults -Append
    "Pliugins" | Out-File -FilePath $FileResults -Append
    ("-" * 80) | Out-File -FilePath $FileResults -Append
    foreach ($item in $PackageFileList) {
        if ($item.Id -like $SeachPatterPlugins) {
            "Plugin.Version = $([IO.Path]::GetFileName($item.File))" | Out-File -FilePath $FileResults -Append
        }
    }

    # touchpanels
    ("`n" * 2) | Out-File -FilePath $FileResults -Append
    "Touchpanel Files" | Out-File -FilePath $FileResults -Append
    ("-" * 80) | Out-File -FilePath $FileResults -Append
    foreach ($item in $PackageFileList) {
        if ($item.Id -like $SearchPatternTp) {
            "TP.Version = $([IO.Path]::GetFileName($item.File))" | Out-File -FilePath $FileResults -Append
        }
    }

    # webProjects
    ("`n" * 2) | Out-File -FilePath $FileResults -Append
    "WebProject Files" | Out-File -FilePath $FileResults -Append
    ("-" * 80) | Out-File -FilePath $FileResults -Append
    foreach ($item in $PackageFileList) {
        if ($item.Id -like $SearchPatternWebProject) {
            "WebProjecy.Version = $([IO.Path]::GetFileName($item.File))" | Out-File -FilePath $FileResults -Append
        }
    }

    # config files by package type
    ("`n" * 2) | Out-File -FilePath $FileResults -Append
    "$($listItem.Id) Configuration Files" | Out-File -FilePath $FileResults -Append
    ("-" * 80) | Out-File -FilePath $FileResults -Append

    foreach ($child in Get-ChildItem -Path "$($listItem.File)\$ConfigDirectory\*" -Recurse) {
        if ($child.attributes -NotMatch 'Directory') {
            # $NVRAMDir = $child.FullName.ToString()
            #$NVRAMPath = $NVRAMDir.Substring($NVRAMDir.IndexOf('NVRAM'))
            # [IO.Path]::GetFileName($child) | Out-File -FilePath $FileRe1sults -Append
            #    $NVRAMPath | Out-File -FilePath $FileResults -Append
        }
    }
    ("`n" * 2) | Out-File -FilePath $FileResults -Append

    Return $FileResults.FullName
}

<#######################################################################################
.DESCRIPTION
Creates a puf file (TO DO)
#######################################################################################>
<#Function PufCreator
{
    Param(
        [Parameter(Mandatory=$false)][String] $P01Path,
        [Parameter(Mandatory=$false)][String] $P02Path,
        [Parameter(Mandatory=$false)][String] $P03Path
    )

    $PufIni = "[Package]\nName=$Name
    Description=$Description
    Version=$Version
    CreationDate=$CreationDate
    CreationAuthor=AUTO
    CreationOrganization=PepperDash

    [DeviceSet.Processor]
    Description=Processor
    DeviceSearchSpace=connected
    DeviceSelectionLogic=model_is_one_of,PRO3,CP3,DIN-AP3,RMC3,CP3N,AV3,MC3

    [Action.100_LoadConfigs]
    Description=Load Config Files
    Selectability=Optional
    DeviceSets=Processor
    ExecutionCondition=Always
    "
}#>

<#######################################################################################
# Code below will automatically run when the script is executed
#######################################################################################>

try {
    Clear-Host;
    # Parameter help description
    if ($PromptEveryTime) {
        $Global:PromptEveryTime = $true
    }

    # Set the path location
    # this overcomes a path issue when the script is ran
    Set-Location $PSScriptRoot
    $TempLocalPath = (Get-Item $PSScriptRoot)

    # get the parent directory of $PSScriptRoot
    $Global:SolutionDirectory = "$TempLocalPath\$SlnDirRelative"
    $Global:PackageDirectory = "$TempLocalPath\$PackageDirRelative"

    # we don't have version data, assumed test-path failed or we did not find a file
    Write-Host ('-' * 80)
    Write-Host "Before we being we need to gather some basic information."
    Write-Host ('-' * 80)
    Write-Host "`n"

    # get search pattern: apps
    if ([String]::IsNullOrEmpty($SearchPatternApp)) {
        $SearchPatternApp = Get-SearchPattern -Index 1
    }
    else {
        Write-Host "Using the following search pattern for programs: $SearchPatternApp"
    }
    if ([String]::IsNullOrEmpty($SearchPatternConfig)) {
        $SearchPatternConfig = Get-SearchPattern -Index 2;
    }
    else {
        Write-Host "Using the following search pattern for configs: $SearchPatternConfig"
    }
    if ([String]::IsNullOrEmpty($SearchPatternTp)) {
        $SearchPatternTp = Get-SearchPattern -Index 3;
    }
    else {
        Write-Host "Using the following search pattern for touchpanels: $SearchPatternTp"
    }
    if ([String]::IsNullOrEmpty($SearchPatternWebProject)) {
        $SearchPatternWebProject = Get-SearchPattern -Index 4;
    }
    else {
        Write-Host "Using the following search pattern for webprojects: $SearchPatternWebProject"
    }
    if ([String]::IsNullOrEmpty($SearchPatternPlugins)) {
        $SearchPatternPlugins = Get-SearchPattern -Index 5;
    }
    else {
        Write-Host "Using the following search pattern for plugins: $SearchPatternPlugins"
    }

    # build list of directories to search
    Get-Directories | Out-Null;

    # build list of files found in the directories searched
    Get-Files | Out-Null;
    if ($Debug) {
        Write-Host ('$' * 80)
    }
    else {
        Clear-Host;
    }


    # check if a package directory exists
    Write-Host "Searching for package directory..."
    if (Test-Path -Path $Global:PackageDirectory) {
        Write-Host "Package directory found.`n";
        # search for manifest file
        if ($Global:ManifestFiles = Get-ChildItem -Path $Global:PackageDirectory -Include "*.zip" -Name) {
            Write-Host "Getting Versions..."
            foreach ($Manifest in $Global:ManifestFiles ) {
                # $Name = $Manifest.Split("_");
                # $Global:Solution = $Name[0];
                # $Version = $Name[1].Split(".");
                # $Global:VersionMajor = $Version[0];
                # $Global:VersionMinor = $Version[1];
                # $Global:VersionCompile = $Version[2];

                # use regex to pull out the solution and version information
                if($Manifest -match '(?<Solution>.*)_(?<Version>\d{3}.\d{3}.\d{3})')
                {
                    # store the matches to the regex pattern, MUST USE $Matches to access the regex data
                    $Global:Solution = $Matches.Solution;
                    $Version = $Matches.Version.Split(".");
                    $Global:VersionMajor = $Version[0];
                    $Global:VersionMinor = $Version[1];
                    $Global:VersionCompile = $Version[2];
                    
                    # if($Debug)
                    # {
                    #     Write-Host "Solution: $($Matches.Solution)";
                    #     Write-Host "Version: $($Matches.Version)";
                    #     Write-Host "Global:Solution: $($Global:Solution)";
                    #     Write-Host "Global:VersionMajor: $($Global:VersionMajor)";
                    #     Write-Host "Global:VersionMinor: $($Global:VersionMinor)";
                    #     Write-Host "Global:VersionCompile: $($Global:VersionCompile)";
                    # }
                }

                break;
            }


            # process user input
            Write-Host "$Global:Solution v$Version"
            $UseThisVersion = ""
            if($Headless)
            { $UseThisVersion = "y"}
            else
            {
                do {

                    $UseThisVersion = (Read-Host "Use this version number? [y or n]").Split(",")
                }
                until($UseThisVersion -eq "y" -or $UseThisVersion -eq "n")
            }

            if ($UseThisVersion -eq "y") {
                $Global:VersionCompile = [int]$Global:VersionCompile + 1
                $Global:Solution = ("$($Global:Solution)_{0:000}.{1:000}.{2:000}" -f $Global:VersionMajor, $Global:VersionMinor, $Global:VersionCompile)
            }
            elseif ($UseThisVersion -eq "n") {
                $Global:VersionMajor = Read-Host("Enter major version number")
                $Global:VersionMinor = Read-Host("Enter minor version number")
                $Global:VersionCompile = Read-Host("Enter compile version number")
                $Global:Solution = ("$($Global:Solution)_{0:000}.{1:000}.{2:000}" -f $Global:VersionMajor, $Global:VersionMinor, $Global:VersionCompile)
                if ($Debug) {
                    Write-Host ('$' * 80)
                }
                else {
                    Clear-Host;
                }
            }

        }
        else {
            Write-Host "Manifest file not found, creating a package...`n"
        }
    }
    else {
        Write-Host "Package directory not found, creating a package...`n"
    }

    # have a manifest file...
    if (!([String]::IsNullOrEmpty($Global:ManifestFiles))) {
        # we have version data from a file
        Write-Host "Removing old manifest file and packages..."
        # Remove-Item -Path "$Global:PackageDirectory\*.*" -Recurse -Force
    }
    # no package exists, get package info
    else {

        Write-Host "$("-" * 80)`n";
        $Global:Solution = (Read-Host("Enter a solution name")).ToUpper()
        $Global:VersionMajor = Read-Host("Enter major version number")
        $Global:VersionMinor = Read-Host("Enter minor version number")
        $Global:VersionCompile = 0;
        $Global:Solution = ("$($Global:Solution)_{0:000}.{1:000}.{2:000}" -f $Global:VersionMajor, $Global:VersionMinor, $Global:VersionCompile)
        if ($Debug) {
            Write-Host ('$' * 80)
        }
        else {
            Clear-Host;
        }
    }


    ######################
    # Config prompts
    ######################
    Add-PackageList -MenuTitle "config" -SearchId $SearchPatternConfig -InputObject $FilesFoundList | Out-Null
    if ($Debug) {
        Write-Host ('$' * 80)
    }
    else {
        Clear-Host;
    }

    # build the package
    if ($FilesFoundList.Count -gt 0) {
        ######################
        # Program prompts
        ######################
        for ($i = 1; $i -le 10; $i++) {
            Add-PackageList -MenuTitle "program $i" -SearchId ("$SearchPatternApp{0:00}" -f $i) -InputObject $FilesFoundList | Out-Null
            if ($Debug) {
                Write-Host ('$' * 80)
            }
            else {
                Clear-Host;
            }
        }


        ######################
        # Plugins prompts
        ######################
        Add-PackageList -MenuTitle "plugin" -SearchId $SearchPatternPlugins -InputObject $FilesFoundList | Out-Null
        if ($Debug) {
            Write-Host ('$' * 80)
        }
        else {
            Clear-Host;
        }

        ######################
        # Touchpanel prompts
        ######################
        Add-PackageList -MenuTitle "touchpanel" -SearchId $SearchPatternTp -InputObject $FilesFoundList | Out-Null
        if ($Debug) {
            Write-Host ('$' * 80)
        }
        else {
            Clear-Host;
        }

        ######################
        # WebProject prompts
        ######################
        Add-PackageList -MenuTitle "web project" -SearchId $SearchPatternWebProject -InputObject $FilesFoundList | Out-Null
        if ($Debug) {
            Write-Host ('$' * 80)
        }
        else {
            Clear-Host;
        }

        Write-Host "Building package with the following files:`n"
        foreach ($item in $add) {
            Write-Host "$($item.File)"
        }
        Write-Host "`n"
        Add-Package | Out-Null;
        Write-Host "Process complete!"
    }
    else {
        Write-Host "No matching files found in the following directories.  Verify you directory paths and try again."
        $DirectorySearchList;
        Write-Host "`n"
    }
}
catch {
    # output the last error message
    "Error: $($Error[0].Exception.Message)"
}