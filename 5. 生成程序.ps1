Set-Location .\Mapping_Tools_CN
dotnet restore Mapping_Tools
dotnet publish Mapping_Tools -c Release -r win-x64 --no-restore --self-contained false
Set-Location .\Mapping_Tools\bin\Release\net5.0-windows\win-x64
Start-Process "Mapping Tools.exe"
pause
