Remove-Item *.nupkg

$status =git status -s -uno
if( $status.Length -gt 0)
{
	Write-Host "files not committed"
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	Exit
}



IEX "&'C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe' ..\clearscript.sln  /p:Configuration=Release /t:clean"
IEX "&'C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe' ..\clearscript.sln  /p:Configuration=Release /p:PlatformToolset=v140"


$sha = git rev-parse HEAD
$path=(get-item ".\..\bin\Release\ClearScript.dll").FullName



$url = "http://nuget-external.corp.volusion.com:8201/api/odata/FindPackagesById()?id='ClearScript.Installer'";
$client = new-object System.Net.WebClient
$client.Headers.Add("Accept", "application/atom+xml,application/xml")
$client.Headers.Add("DataServiceVersion", "1.0;NetFx")
$client.Headers.Add("MaxDataServiceVersion", "2.0;NetFx")
$client.Headers.Add("Accept-Charset", "UTF-8")
$maj =5;
$min =4;
$rev =0;



[xml]$xml = $client.DownloadString($url)
foreach ( $entry in $xml.DocumentElement.entry)
{
    [string]$ver = $entry.properties.version
    if ( [string]::IsNullOrEmpty($ver))
    {
       continue;
    }
    $parts = $ver.Split(".");
    if ($parts.Length -le 2)
    {
        continue;
    }
    [int]$temp=0;
    if ( [int]::TryParse($parts[0],  [ref]$temp) -and $temp -ne $maj){
        continue;
    }
    if ( [int]::TryParse($parts[1],  [ref]$temp) -and $temp -ne $min){
        continue;
    }
    if ( [int]::TryParse($parts[2],  [ref]$temp) -and $temp -gt $rev){
        $rev = $temp
    }


}

write-host $rev
$rev= $rev+1;
write-host $rev

$ver = "5.4." +$rev


write-host $ver 

(Get-Content ClearScript.Installer.targets.template) | Foreach-Object {$_ -replace "TOKEN", $ver} | Set-Content ClearScript.Installer.targets

nuget pack ClearScript.Installer.nuspec -Properties version=5.4.$rev`;sha=$sha


$answer = Read-Host "copy to external feed [y/n]"

if ( $answer.ToLower().indexOf("y") -gt -1)
{
	Copy-Item ClearScript.Installer.5.4.$rev`.nupkg N:\Mozu\Nuget\nupkg\external
}








