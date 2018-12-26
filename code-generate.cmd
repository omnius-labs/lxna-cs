setlocal

set BAT_DIR=%~dp0

if %PROCESSOR_ARCHITECTURE% == x86 (
    set TOOL_PATH=%BAT_DIR%tools\win-x86\Omnix.Serialization.RocketPack.CodeGenerator\Omnix.Serialization.RocketPack.CodeGenerator.exe
)

if %PROCESSOR_ARCHITECTURE% == AMD64 (
    set TOOL_PATH=%BAT_DIR%tools\win-x64\Omnix.Serialization.RocketPack.CodeGenerator\Omnix.Serialization.RocketPack.CodeGenerator.exe
)

"%TOOL_PATH%" %BAT_DIR%formats\Lxna.Rpc.rpf %BAT_DIR%src\Lxna.Rpc\_RocketPack\Messages.generated.cs
"%TOOL_PATH%" %BAT_DIR%formats\Lxna.Messages.rpf %BAT_DIR%src\Lxna.Messages\_RocketPack\Messages.generated.cs
"%TOOL_PATH%" %BAT_DIR%formats\Lxna.Core.Contents.rpf %BAT_DIR%src\Lxna.Core\Contents\_RocketPack\Messages.generated.cs
