
Remove-Item .\*.nupkg
$path=(get-item ".\..\bin\Release\ClearScript.dll").FullName
$ass = [Reflection.Assembly]::Loadfile($path)
$ver = $ass.GetName().version.ToString()
(Get-Content ClearScript.Installer.targets) | Foreach-Object {$_ -replace "ClearScript.Installer.[^\\]*", "ClearScript.Installer.$ver"} | Set-Content ClearScript.Installer.targets
nuget pack ClearScript.Installer.nuspec -Properties version=$ver;
(Get-Content ClearScript.Installer.targets) | Foreach-Object {$_ -replace "ClearScript.Installer.[^\\]*", "ClearScript.Installer.version"} | Set-Content ClearScript.Installer.targets