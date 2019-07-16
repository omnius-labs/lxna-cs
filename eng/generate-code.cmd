setlocal

setx DOTNET_CLI_TELEMETRY_OPTOUT 1

set TOOL_PATH=%cd%\tools\win\Omnix.Serialization.RocketPack.CodeGenerator\Omnix.Serialization.RocketPack.CodeGenerator.exe

"%TOOL_PATH%" %cd%\formats\Lxna.Core.Contents.rpf %cd%\src\Lxna.Core\Contents\_RocketPack\Messages.generated.cs

"%TOOL_PATH%" %cd%\formats\Lxna.Messages.rpf %cd%\src\Lxna.Messages\_RocketPack\Messages.generated.cs

"%TOOL_PATH%" %cd%\formats\Lxna.Rpc.rpf %cd%\src\Lxna.Rpc\_RocketPack\Messages.generated.cs