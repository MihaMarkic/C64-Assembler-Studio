# Creates Start Menu entry
$ProgramsPath = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::Programs)
$ShortcutDirectory = [IO.Path]::Combine($ProgramsPath, "C64AssemblerStudio")
Write-Host $ShortcutDirectory
if (-Not (Test-Path -Path $ShortcutDirectory)) {
	[IO.Directory]::CreateDirectory($ShortcutDirectory)
	Write-Host("Directory created")
} else {
	Write-Host("Directory already exists")
}
$WshShell = New-Object -comObject WScript.Shell
$AppLink = [IO.Path]::Combine($ShortcutDirectory, "C64 Assembler Studio.lnk")
$Shortcut = $WshShell::CreateShortcut($AppLink)
$Shortcut.TargetPath = [IO.Path]::Combine($PSScriptRoot, "C64AssemblerStudio.exe")
$Shortcut.Description = "An IDE for building C64 assembler programs using VICE emulator"
$Shortcut.Save()