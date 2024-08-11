# Creates Start Menu entry
$ProgramsPath = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::Programs)
$ShortcutDirectory = [IO.Path]::Combine($ProgramsPath, "C64AssemblerStudio")
Write-Host $ShortcutDirectory
if (Test-Path -Path $ShortcutDirectory) {
	[IO.Directory]::Delete($ShortcutDirectory, $true)
	Write-Host("Directory deleted")
} else {
	Write-Host("Directory does not exist")
}