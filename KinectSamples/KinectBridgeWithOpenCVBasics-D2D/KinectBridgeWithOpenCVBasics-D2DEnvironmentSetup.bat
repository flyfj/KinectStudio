@echo off

REM Check that we have administrator privileges
net session >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo "Error: This script needs to be Run as Adminstrator"
	goto :eof
)

REM 2nd command line parameter
if /i [%2] neq [] (
	set SearchDirectory=%2
) else (
	set SearchDirectory="C:\opencv"
)

REM Check if the directory provided by the user has the OpenCV README in it
REM If not, keep prompting the user for the actual directory.
:DirectoryCheck
echo.
echo Checking %SearchDirectory% for OpenCV..
if not exist %SearchDirectory%\README (
	echo OpenCV not found in %SearchDirectory%
	echo.

	set /p SearchDirectory=Enter the OpenCV directory: 

	goto :DirectoryCheck
)
echo Found OpenCV in %SearchDirectory%.
echo.

REM Check if user gave us OpenCV version
if /i "%1" neq "" (
	set Version="%1"
) else (
	echo No OpenCV version has been provided from command line.
	echo.

	set /p Version=Enter the OpenCV version: 
)
echo Using OpenCV version %Version%

echo.
echo Setting system environment variables...

REM Set OPENCV_DIR
echo.
echo Setting OPENCV_DIR = %SearchDirectory%
setx /m OPENCV_DIR %SearchDirectory%

REM Set OPENCV_VER
echo.
echo Setting OPENCV_VER = %Version%
setx /m OPENCV_VER %Version%

echo.
echo.
echo DONE
echo Please log out and log back in for the new systen envirionment variables to take effect!