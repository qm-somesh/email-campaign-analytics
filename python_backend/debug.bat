@echo off
setlocal enabledelayedexpansion

echo.
echo =====================================================
echo  🐛 FastAPI Debugging Helper
echo =====================================================
echo.

cd /d "%~dp0"

echo Choose your debugging option:
echo.
echo 1. 🔍 Quick Debug (check imports and config)
echo 2. 🚀 Start Server in Debug Mode
echo 3. 🧪 Run Debug Tools (interactive menu)
echo 4. 🌐 Test Server Health
echo 5. 📡 Test API Endpoints
echo 6. 🔄 Run Full Diagnostics
echo 7. 📝 Show Debug Guide
echo.

set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" goto quick_debug
if "%choice%"=="2" goto start_debug
if "%choice%"=="3" goto debug_tools
if "%choice%"=="4" goto test_health
if "%choice%"=="5" goto test_endpoints
if "%choice%"=="6" goto full_diagnostics
if "%choice%"=="7" goto show_guide

echo Invalid choice. Please select 1-7.
pause
goto :eof

:quick_debug
echo.
echo 🔍 Running Quick Debug Check...
echo.
python debug_tools.py imports
echo.
python debug_tools.py config
echo.
pause
goto :eof

:start_debug
echo.
echo 🚀 Starting FastAPI in Debug Mode...
echo    - Enhanced logging enabled
echo    - Detailed error messages
echo    - Auto-reload on file changes
echo.
python start_debug.py
pause
goto :eof

:debug_tools
echo.
echo 🧪 Opening Interactive Debug Tools...
echo.
python debug_tools.py
pause
goto :eof

:test_health
echo.
echo 🌐 Testing Server Health...
echo.
python debug_tools.py health
pause
goto :eof

:test_endpoints
echo.
echo 📡 Testing API Endpoints...
echo.
python debug_tools.py endpoints
pause
goto :eof

:full_diagnostics
echo.
echo 🔄 Running Full Diagnostic Suite...
echo.
python debug_tools.py full
pause
goto :eof

:show_guide
echo.
echo 📝 Opening Debug Guide...
echo.
if exist "DEBUGGING_GUIDE.md" (
    start DEBUGGING_GUIDE.md
) else (
    echo DEBUGGING_GUIDE.md not found in current directory.
)
pause
goto :eof
