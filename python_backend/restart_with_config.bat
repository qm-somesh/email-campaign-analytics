@echo off
echo.
echo =====================================================
echo  Restarting Python Backend with Updated Config
echo =====================================================
echo.

cd /d "%~dp0"

echo 🔧 Activating virtual environment...
call venv\Scripts\activate.bat

echo 📋 Updated Configuration:
echo   • Gemini API Key: AIzaSyDBrIDXPfipjL74HTWJskMfcGIkdAB72Wg
echo   • Database: tvm.dev.db.internal.velocityadmin.com\SQL01,43201/TV_EmailService
echo   • CORS: Enabled for all origins (fixes Swagger CORS issue)
echo.

echo 🛑 Stopping any existing server on port 8000...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :8000') do (
    echo   Killing process %%a
    taskkill /PID %%a /F >nul 2>&1
)

echo.
echo 🚀 Starting server with updated configuration...
echo.
echo    🌐 API: http://localhost:8000
echo    📚 Swagger: http://localhost:8000/swagger
echo    ❤️  Health: http://localhost:8000/health
echo.
echo    The CORS issue should now be fixed!
echo    You can now use curl commands and Swagger UI.
echo.

python start.py
