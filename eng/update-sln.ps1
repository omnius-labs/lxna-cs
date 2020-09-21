#!/bin/bash

Remove-Item -Path lxna.sln -Force
dotnet new sln
dotnet sln lxna.sln add (ls -r ./refs/core/src/**/*.csproj)
dotnet sln lxna.sln add (ls -r ./refs/core/test/**/*.csproj)
dotnet sln lxna.sln add (ls -r ./src/**/*.csproj)
dotnet sln lxna.sln add (ls -r ./test/**/*.csproj)
