#!/bin/bash

dotnet new sln --force -n lxna
dotnet sln add ./src/**/*.csproj
dotnet sln add ./test/**/*.csproj
dotnet sln add ./refs/core/src/**/*.csproj
dotnet sln add ./refs/core/test/**/*.csproj
