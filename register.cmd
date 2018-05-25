p:
cd P:\crypto-rtd
@echo Registering RTD Server
rem %SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe src\CryptoRtd\bin\Debug\CryptoRtd.dll /codebase
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe src\CryptoRtd\bin\Release\CryptoRtd.dll /codebase
pause
