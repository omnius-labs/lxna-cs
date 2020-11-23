#!/bin/bash

dotnet new sln --force
dotnet sln lxna.sln add ./src/**/*.csproj
dotnet sln lxna.sln add ./test/**/*.csproj
dotnet sln lxna.sln add ./refs/core/src/**/*.csproj
dotnet sln lxna.sln add ./refs/core/test/**/*.csproj
