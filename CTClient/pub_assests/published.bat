@echo off

cd /d %~dp0

echo.|dotNET_Reactor -project "CTClient.nrproj"

xcopy obfuscators\* publish /s /e /y

compil32 /cc "ct_client_installer.iss"
