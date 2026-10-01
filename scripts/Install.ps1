#Requires -Version 5.1
[CmdletBinding()]
param([switch]$EnableStartup, [switch]$Launch)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $repo 'artifacts\publish\win-x64'
$install = Join-Path $env:LOCALAPPDATA 'Programs\SystemTrayProcessManager'
$exe = Join-Path $install 'SystemTrayProcessManager.exe'
if (Get-Process SystemTrayProcessManager -ErrorAction SilentlyContinue) {
    throw 'Exit Process Manager from its tray menu before installing or updating.'
}
dotnet publish (Join-Path $repo 'src\SystemTrayProcessManager.UI\SystemTrayProcessManager.csproj') -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed; installation was not changed.' }
New-Item -ItemType Directory -Path $install -Force | Out-Null
Copy-Item -Path (Join-Path $publish '*') -Destination $install -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall.ps1') -Destination $install -Force
$shell = New-Object -ComObject WScript.Shell
try {
    $shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'SystemTray Process Manager.lnk'))
    $shortcut.TargetPath = $exe
    $shortcut.Arguments = '--show'
    $shortcut.WorkingDirectory = $install
    $shortcut.IconLocation = "$exe,0"
    $shortcut.Save()
} finally {
    if ($shortcut) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
}
$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$existingStartupValue = $null
if (Test-Path -LiteralPath $run) {
    # Get-ItemPropertyValue reports a missing registry value as an error when
    # $ErrorActionPreference is Stop. Inspect the property's presence instead.
    $runProperties = Get-ItemProperty -LiteralPath $run
    $existingStartupValue = $runProperties.PSObject.Properties['SystemTrayProcessManager']
}
if ($EnableStartup -or $null -ne $existingStartupValue) {
    New-Item -Path $run -Force | Out-Null
    New-ItemProperty -Path $run -Name SystemTrayProcessManager -Value ('"' + $exe + '" --minimized') -PropertyType String -Force | Out-Null
}
$uninstall = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SystemTrayProcessManager'
New-Item -Path $uninstall -Force | Out-Null
$properties = @{
    DisplayName = 'SystemTray Process Manager'; DisplayVersion = '1.0.0'; Publisher = 'DontDoThat21'
    InstallLocation = $install; DisplayIcon = $exe
    UninstallString = ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $install 'Uninstall.ps1') + '"')
}
foreach ($name in $properties.Keys) { New-ItemProperty -Path $uninstall -Name $name -Value $properties[$name] -PropertyType String -Force | Out-Null }
Write-Host "Installed to $install. Open SystemTray Process Manager from Start."
if ($Launch) { Start-Process -FilePath $exe -ArgumentList '--show' -WindowStyle Hidden }
