@echo off
set config=%1
if "%config%" == "" (
   set config=Release
)

set version=2016.2.3
if not "%PackageVersion%" == "" (
   set version=%PackageVersion%
)

nuget restore src\Resharper.ConfigurationSense.sln

"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild" src\Resharper.ConfigurationSense.sln /t:Rebuild /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false

nuget pack resharper.nuspec -NoPackageAnalysis -Version %version% -Properties "Configuration=%config%;ReSharperDep=Wave;ReSharperVer=[6.0]"
