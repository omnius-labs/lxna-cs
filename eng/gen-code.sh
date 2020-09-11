#!/bin/bash

DOTNET_CLI_TELEMETRY_OPTOUT=1

BIN_DIR="$PWD/bin/tools/linux"
TOOL_PATH="$BIN_DIR/Omnius.Core.RocketPack.DefinitionCompiler/Omnius.Core.RocketPack.DefinitionCompiler"
INCLUDE_1="$PWD/refs/core/fmt/**/*.rpd"
INCLUDE_2="$PWD/fmt/**/*.rpd"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Rpc/Omnius.Lxna.Rpc.rpd" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/src/Omnius.Lxna.Rpc/_RocketPack/_Generated.cs"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Components/Omnius.Lxna.Components.Models.rpd" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/src/Omnius.Lxna.Components/Models/_RocketPack/_Generated.cs"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Components.Impls/Omnius.Lxna.Components.Internal.rpd" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/src/Omnius.Lxna.Components.Impls/Internal/_RocketPack/_Generated.cs"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Components.Tests/Omnius.Lxna.Components.Internal.rpd" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/test/Omnius.Lxna.Components.Tests/Internal/_RocketPack/_Generated.cs"
