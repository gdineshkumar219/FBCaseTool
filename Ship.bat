@echo off
setlocal

set Project=Src\FBCaseTool\FBCaseTool.csproj
set PubDir=Publish\

rem Locate the Inno Setup compiler (machine-wide or per-user install).
set "ISCC="
for %%P in (
   "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
   "%ProgramFiles%\Inno Setup 6\ISCC.exe"
   "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
) do if not defined ISCC if exist "%%~P" set "ISCC=%%~P"

if not defined ISCC (
   echo ERROR: Inno Setup 6 ISCC.exe not found. Install Inno Setup 6 or add its path to Ship.bat.
   goto :error
)

if exist %PubDir% (rd %PubDir% /s /q)
md %PubDir%

echo Publishing FBCaseTool
dotnet publish %Project% -c Release -o %PubDir%
if errorlevel 1 goto :error

echo Injecting version into the installer script
dotnet run --project Tools\Installer\VersionInjector
if errorlevel 1 goto :error

echo Running Inno Setup Compiler
"%ISCC%" Tools\Installer\FBCaseTool.iss
if errorlevel 1 goto :error

echo Cleaning up
del Tools\Installer\FBCaseTool.iss
rd %PubDir% /s /q

echo Done.
goto :eof

:error
echo.
echo Ship FAILED.
exit /b 1
