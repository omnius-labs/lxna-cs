$Env:ContinuousIntegrationBuild = "true"

$output = "../../tmp/test/win/opencover.xml";
dotnet test --no-restore --filter "FullyQualifiedName~Lxna" /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput="$output" /p:Exclude="[xunit*]*%2c[*.Tests]*%2c[Core*]*";

if (!$?) {
    exit 1
}

dotnet tool run reportgenerator "-reports:tmp/test/win/opencover.xml" "-targetdir:pub/code-coverage/win"
