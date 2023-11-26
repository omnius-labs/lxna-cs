#!/bin/bash
set -euo pipefail

rm -rf ./pub/linux-x64/*

export NativeDepsPlatform=linux
export PlatformTarget=x64

dotnet publish ./src/Omnius.Lxna.Ui.Desktop -p:PublishSingleFile=true --runtime linux-x64 --configuration Release --self-contained true --output ./pub/linux-x64/bin/ui-desktop
