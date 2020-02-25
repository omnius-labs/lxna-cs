#!/bin/bash

DOTNET_CLI_TELEMETRY_OPTOUT=1

BIN_DIR=$PWD/bin/tools/linux
TOOL_PATH=$BIN_DIR/Omnius.Core.Serialization.RocketPack.DefinitionCompiler/Omnius.Core.Serialization.RocketPack.DefinitionCompiler
INCLUDE="-i $PWD/fmt $PWD/refs/core/fmt"

"$TOOL_PATH" $PWD/fmt/Omnius.Lxna.Service/Omnius.Lxna.Service.rpd $INCLUDE -o $PWD/src/Omnius.Lxna.Service/_RocketPack/Messages.generated.cs
"$TOOL_PATH" $PWD/fmt/Omnius.Lxna.Service.Implements/Omnius.Lxna.Service.Internal.rpd $INCLUDE -o $PWD/src/Omnius.Lxna.Service.Implements/Internal/_RocketPack/Messages.generated.cs

"$TOOL_PATH" $PWD/fmt/Omnius.Lxna.Service.Tests/Omnius.Lxna.Service.Internal.rpd $INCLUDE -o $PWD/test/Omnius.Lxna.Service.Tests/Internal/_RocketPack/Messages.generated.cs
