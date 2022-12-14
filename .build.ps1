<#
.Synopsis
	Build script invoked by Invoke-Build.

.Description
	TODO: Declare build parameters as standard script parameters. Parameters
	are specified directly for Invoke-Build if their names do not conflict.
	Otherwise or alternatively they are passed in as "-Parameters @{...}".
#>

# TODO: [CmdletBinding()] is optional but recommended for strict name checks.
[CmdletBinding()]
param(
  $source_dir = ".",
  $release_dir = "Release",

  $build_configuration = "Release"
)

# Synopsis: Clean all bin and obj folders.
task Clean { 
    Write-Host "Cleaning repository."
    $dirs = Get-ChildItem $source_dir -include bin,obj -Recurse -Directory
    $files = (Get-ChildItem $dirs -Recurse -File).FullName
    if ($dirs -and $files){
        Remove-Item $files -ErrorAction SilentlyContinue
        Write-Host "Cleaned $(($files | Measure-Object).Count) files."
    }
    $subDirs = (Get-ChildItem $dirs -Recurse -Directory).FullName
    if ($dirs -and $subDirs){
        Remove-Item $subDirs -ErrorAction SilentlyContinue -Recurse
        Write-Host "Cleaned $(($subDirs | Measure-Object).Count) dirs."
    }
    Write-Host "Repository is clean."
}

# Synopsis: Restore nuget packages.
task NugetRestore { 
    Write-Host "Restoring nuget packages."
    $projects = Get-ChildItem $source_dir -include *.csproj -Recurse -File
    if ($projects){
        foreach ($project in $projects){
            exec { dotnet restore $project }
        }
    }
    Write-Host "Restored nuget packages."
}
# Synopsis: Compile Solution.
task Compile NugetRestore, { 
    Write-Host "Compiling the projects."
    $projects = Get-ChildItem $source_dir -include *.csproj -Recurse -File
    if ($projects){
        foreach ($project in $projects){
            exec { dotnet build $project --configuration $build_configuration }
        }
    }
    Write-Host "Compiled the projects."
}

# Synopsis: Run all tests.
task Test { 
    Write-Host "Testing solution."
    $projects = Get-ChildItem $source_dir -include *Tests.csproj -Recurse -File
    if ($projects){
        foreach ($project in $projects){
            exec { dotnet test $project --configuration $build_configuration }
        }
    }
    Write-Host "Tested solution."
}

# Synopsis: Publish the packages.
task PublishPackages {
    Write-Host "Publishing packages."

    $appReleaseDir = "$release_dir\TopLoggerPlus"

    $releaseDirs = $appReleaseDir #, $swaggerUIReleaseDir
    foreach ($releaseDir in $releaseDirs)
    {
        if (Test-Path $releaseDir){
            Remove-Item $releaseDir -Recurse
        }
        New-Item $releaseDir -ItemType Directory
    }
    
    exec { dotnet publish -f:net7.0-android $source_dir\Services\App\TopLoggerPlus.App\TopLoggerPlus.App.csproj -c $build_configuration -o (Resolve-Path $appReleaseDir) }

    Write-Host "Published packages."
}

# Synopsis: The default task: clean/compile/test.
task . Clean, Compile, Test
task Publish ., PublishPackages