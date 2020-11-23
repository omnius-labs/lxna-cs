#!/bin/bash

DOTNET_CLI_TELEMETRY_OPTOUT=1

BIN_DIR="$PWD/bin/tools/linux"
TOOL_PATH="$BIN_DIR/Omnius.Core.RocketPack.DefinitionCompiler/Omnius.Core.RocketPack.DefinitionCompiler"
INCLUDE_1="$PWD/refs/core/fmt/**/*.rpf"
INCLUDE_2="$PWD/fmt/**/*.rpf"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Components/Omnius.Lxna.Components.Models.rpf" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/src/Omnius.Lxna.Components/Models/_RocketPack/_Generated.cs"

"$TOOL_PATH" compile -s "$PWD/fmt/Omnius.Lxna.Components.Implementations/Omnius.Lxna.Components.Internal.Models.rpf" -i "$INCLUDE_1" -i "$INCLUDE_2" -o "$PWD/src/Omnius.Lxna.Components.Implementations/Internal/_RocketPack/_Generated.cs"
