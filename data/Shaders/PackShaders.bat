@echo off

echo Compiling C# script...

cd\windows\Microsoft.NET\Framework\v3.5\
csc.exe/t:exe /out:%~dp0\PackShaders.exe %~dp0\GenerateHeader.cs
cd  "%~dp0"
PackShaders.exe

echo .exe generated and executed.
timeout 3