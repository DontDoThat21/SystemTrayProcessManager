#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$install = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\SystemTrayProcessManager'))
if ([IO.Path]::GetFullPath($PSScriptRoot) -ne $install) { throw 'Run the installed uninstaller from Windows Installed apps.' }
if (Get-Process SystemTrayProcessManager -ErrorAction SilentlyContinue) { throw 'Exit Process Manager from its tray menu before uninstalling.' }
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name SystemTrayProcessManager -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run' -Name SystemTrayProcessManager -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'SystemTray Process Manager.lnk') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SystemTrayProcessManager' -Recurse -Force -ErrorAction SilentlyContinue
# The absolute target above is fixed and verified against the installed script directory.
Remove-Item -LiteralPath $install -Recurse -Force
Write-Host 'Uninstalled. Your settings and logs have been preserved.'
