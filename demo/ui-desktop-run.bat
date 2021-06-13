cd /d %~dp0

dotnet run --project ../src/Omnius.Lxna.Ui.Desktop/ -- "./ui-desktop/state" "./ui-desktop/temp" "./ui-desktop/logs"
