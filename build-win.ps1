Remove-Item ./pub/win-x64/* -Recurse

$Env:NativeDepsPlatform="Windows"
$Env:PlatformTarget="x64"

dotnet publish ./src/Omnius.Lxna.Ui.Desktop -p:PublishSingleFile=true --runtime win-x64 --configuration Release --self-contained true --output ./pub/win-x64/bin/
