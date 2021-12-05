Set-Location $PSScriptRoot

$Env:BuildTargetName = "ui-desktop"
dotnet run --project ../src/Omnius.Lxna.Ui.Desktop/ -- --config "./$Env:BuildTargetName/config.yml" --storage "./$Env:BuildTargetName/storage" --logs "./$Env:BuildTargetName/logs" -v
