@echo off
taskkill /F /IM DS4Windows.exe /T
TIMEOUT /T 2
xcopy "DS4Windows\bin\x64\DS4Windows\*" "C:\Program Files\DS4Windows\" /E /Y /I

TIMEOUT /T 2


                                   