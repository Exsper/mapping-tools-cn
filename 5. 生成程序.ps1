Set-Location .\Mapping_Tools_CN
dotnet restore Mapping_Tools
dotnet build Mapping_Tools -c Release /p:Platform=AnyCPU --no-restore
Set-Location .\Mapping_Tools\bin\Release\net5.0-windows
Start-Process "Mapping Tools.exe"
pause
