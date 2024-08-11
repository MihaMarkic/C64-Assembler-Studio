$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$Home\Desktop\ColorPix.lnk")
$Shortcut.TargetPath = "C:\Program Files (x86)\ColorPix\ColorPix.exe"
$Shortcut.Save()