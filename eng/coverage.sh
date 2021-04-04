#!/bin/bash

export ContinuousIntegrationBuild=true

output="../../tmp/test/linux/opencover.xml"
dotnet test --no-restore --filter "FullyQualifiedName~Omnius.Lxna" -p:CollectCoverage=true -p:CoverletOutputFormat=opencover -p:CoverletOutput="$output" -p:Exclude="[xunit*]*%2c[*.Tests]*%2c[Omnius.Core*]*"

ret=$?
if [ $ret -gt 0 ]; then
    exit $ret
fi

dotnet tool run reportgenerator "-reports:tmp/test/linux/opencover.xml" "-targetdir:pub/code-coverage/linux"
