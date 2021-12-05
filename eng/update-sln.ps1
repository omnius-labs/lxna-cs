dotnet new sln --force -n lxna
dotnet sln add (ls -r ./refs/core/src/**/*.csproj)
dotnet sln add (ls -r ./refs/core/test/**/*.csproj)
dotnet sln add (ls -r ./src/**/*.csproj)
dotnet sln add (ls -r ./test/**/*.csproj)
