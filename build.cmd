@echo off

echo Restoring NuGet packages
nuget restore src\crypto-rtd.sln
@if ERRORLEVEL 1 pause

echo Building release
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release  /nologo src\CryptoRtd\CryptoRtd.csproj
@if ERRORLEVEL 1 pause