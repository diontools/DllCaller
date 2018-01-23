set /p apiKey=APIKey:
nuget push DllCaller.*.nupkg %apiKey% -source https://www.nuget.org/api/v2/package
pause