#Run this as administrator
cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
InstallUtil.exe C:\nLIGHT_CIM\nLIGHT_PushSPCToFITsService\PushSPCToFITsService.exe
echo sc config nLIGHT_PushSPCToFITs start= auto
echo sc start nLIGHT_PushSPCToFITs
pause