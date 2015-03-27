
Remove-Item *.nupkg
IEX "&'C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe' ..\clearscript.sln  /p:Configuration=Release /t:clean"
IEX "&'C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe' ..\clearscript.sln  /p:Configuration=Release"
$status =git status -s -uno
if( $status.Length -gt 0 )
{
	Write-Host "files not committed"
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	Exit
}
$sha = git rev-parse HEAD
$path=(get-item ".\..\bin\Release\ClearScript.dll").FullName
$ass = [Reflection.Assembly]::Loadfile($path)
$ver = $ass.GetName().version.ToString()
(Get-Content ClearScript.Installer.targets) | Foreach-Object {$_ -replace "ClearScript.Installer.[^\\]*", "ClearScript.Installer.$ver"} | Set-Content ClearScript.Installer.targets



nuget pack ClearScript.Installer.nuspec -Properties version=$ver`;sha=$sha
(Get-Content ClearScript.Installer.targets) | Foreach-Object {$_ -replace "ClearScript.Installer.[^\\]*", "ClearScript.Installer.version"} | Set-Content ClearScript.Installer.targets