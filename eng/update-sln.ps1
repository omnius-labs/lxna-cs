dotnet new sln --force -n lxna
dotnet sln add (ls -r ./refs/core-cs/src/**/*.csproj)
dotnet sln add (ls -r ./refs/core-cs/test/**/*.csproj)
dotnet sln add (ls -r ./src/**/*.csproj)
dotnet sln add (ls -r ./test/**/*.csproj)
