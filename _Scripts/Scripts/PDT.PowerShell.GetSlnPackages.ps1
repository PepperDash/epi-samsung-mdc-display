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
    [Parameter(Mandatory = $false)][Bool]$RemoveExsisting = $true,
    [Parameter(Mandatory = $false)][Bool]$PromptForDelete = $true,
    [Parameter(Mandatory = $false)][Bool]$AddToGitIgnore = $true,
    [Parameter(Mandatory = $false)][String]$SlnDirRelative = "..\..\"
)


<#######################################################################################
.DESCRIPTION
variables
#######################################################################################>


# Set the path location
# this overcomes a path issue when the script is ran
Set-Location $PSScriptRoot
$TempLocalPath = (Get-Item $PSScriptRoot)
$SolutionDirectory = "$TempLocalPath\$SlnDirRelative"

$PackageConfigs = Get-ChildItem -File -Include packages.config -Path $SolutionDirectory -Recurse


if (!$PackageConfigs.Exists) {
    Write-Output "No packages.config fieles found in $PackageDirectory."
    return 
}
 
foreach ($config in $PackageConfigs) 
{
    Write-Output ($config.DirectoryName)
    $DirName = $config.Directory.Name
    if($AddToGitIgnore -and @( Get-Content "$SolutionDirectory\.gitignore" | Where-Object { $_.Contains("$DirName/*") } ).Count -eq 0 )
    {
        Add-Content "$SolutionDirectory\.gitignore" "$DirName/*"
        Add-Content "$SolutionDirectory\.gitignore" "!*/packages.config"
    }
    if($RemoveExsisting)
    {
        $folder = Get-ChildItem -Path $config.DirectoryName -Directory
        if($PromptForDelete)
        {
            $subfolders = Get-ChildItem -Path $config.DirectoryName -Directory -recurse -depth 0
            Write-Output "-------Folders---------"
            foreach ($subfolder in $subfolders) {
                Write-Output ($subfolder.Name)
            }
            Write-Output "-----------------------"
            $DeleteQuestion = $null
            do {
                $DeleteQuestion = (Read-Host -Prompt 'Delete these folders y/n?')
                }
            until($DeleteQuestion -eq "y" -or $DeleteQuestion -eq "n")
        }
        if($DeleteQuestion -eq "y" -or !$PromptForDelete)
        {
            Remove-Item -Path $folder.FullName -Recurse   
        }
        
    }
    nuget install $config.FullName -OutputDirectory $config.DirectoryName -excludeVersion 
    
}
Write-Output "---------------------"`n


