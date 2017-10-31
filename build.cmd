@echo off

echo Restoring NuGet packages
nuget\nuget.exe restore src\crypto-rtd.sln

echo Building release
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release /v:minimal /nologo src\crypto-rtd.sln