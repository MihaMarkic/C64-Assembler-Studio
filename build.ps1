<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
Expanse Windows bootstrap.

.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER ShowDescription
Shows description about tasks.

.LINK
https://cakebuild.net

#>

[CmdletBinding()]
Param(
	[ValidateSet("Default", "Publish", "Test", "SignWin")]
	[string]$Target,
	[ValidateSet("WinX64", "LinuxX64", "OSXArm64", "Dependent")]
	[string]$Architecture,
	[ValidateSet("Scoop", "Archive")]
	[string]$BuildType,
	[ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
	[string]$Verbosity,
	[switch]$ShowDescription
)

# Build Cake arguments
$cakeArguments = @();
if ($Target) { $cakeArguments += "--target=$Target" }
if ($Architecture) { $cakeArguments += "--architecture=$Architecture" }
if ($BuildType) { $cakeArguments += "--buildType=$BuildType" }
if ($Verbosity) { $cakeArguments += "--verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "--showdescription" }
if ($DryRun) { $cakeArguments += "--dryrun" }

dotnet run --project .\src\C64AssemblerStudioBuilder\Builder\Build.csproj -- $cakeArguments
exit $LASTEXITCODE;