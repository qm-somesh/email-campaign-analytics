@echo off
echo.
echo ==========================================
echo EmailCampaignReporting Python Backend
echo ==========================================
echo.

cd /d "%~dp0"

echo 🔍 Checking Python installation...
python --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Python not found! Please install Python 3.9 or higher.
    pause
    exit /b 1
)

echo ✅ Python found
echo.

echo 🔍 Checking virtual environment...
if not exist "venv" (
    echo 📦 Creating virtual environment...
    python -m venv venv
    if errorlevel 1 (
        echo ❌ Failed to create virtual environment
        pause
        exit /b 1
    )
)

echo 🔧 Activating virtual environment...
call venv\Scripts\activate.bat

echo 📦 Installing/updating dependencies...
pip install -r requirements.txt
if errorlevel 1 (
    echo ❌ Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo 🚀 Starting Python Backend Server...
echo.
echo    🌐 API: http://localhost:8000
echo    📚 Documentation: http://localhost:8000/swagger
echo    ❤️  Health check: http://localhost:8000/health
echo.
echo    Press Ctrl+C to stop the server
echo    Starting in 3 seconds...
timeout /t 3 /nobreak >nul
echo.

python start.py

pause
