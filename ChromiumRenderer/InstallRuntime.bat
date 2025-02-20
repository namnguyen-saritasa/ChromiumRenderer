@echo off
REM Exit with code 1 if system is 32-bit
if /I "%PROCESSOR_ARCHITECTURE%"=="x86" (
    if not defined PROCESSOR_ARCHITEW6432 (
        echo 32-bit system detected.
        exit /b 1
    )
)

echo "runtimes-cache" > .gitignore
echo "runtimes-cache/" > .dockerignore

REM Set up temporary download folder and target extraction folder
set "tempDir=%TEMP%\chrome-headless-shell-win64-download"
if not exist "%tempDir%" mkdir "%tempDir%"
set "zipFile=%tempDir%\chrome-headless-shell-win64.zip"
set "parentTargetDir=runtimes-cache\win-x64"
set "targetDir=runtimes-cache\win-x64\native"
set "checkFile=runtimes-cache\win-x64\native\chrome-headless-shell.exe"
set "zippedFolderDir=runtimes-cache\win-x64\chrome-headless-shell-win64"
if exist "%checkFile%" exit /b 0
if not exist "%parentTargetDir%" mkdir "%parentTargetDir%"
if exist "%targetDir%" rmdir /s /q "%targetDir%"

REM Download the zip file using certutil
echo Downloading chromedriver...
curl "https://storage.googleapis.com/chrome-for-testing-public/133.0.6943.126/win64/chrome-headless-shell-win64.zip" --output "%zipFile%"
if errorlevel 1 (
    echo Download failed.
    exit /b 1
)

REM Extract the zip file using tar (available on Windows 10+)
echo Extracting files...
tar -xf "%zipFile%" -C "%parentTargetDir%"
if errorlevel 1 (
    echo Extraction failed.
    exit /b 1
)

move "%zippedFolderDir%" "%targetDir%"

REM Delete the downloaded zip file and remove temporary folder if empty
del /f "%zipFile%"
rd "%tempDir%" 2>nul

echo Done.
