@echo off
echo Testing Swagger Documentation Issue
echo ====================================

cd /d "%~dp0"

REM Activate virtual environment
echo Activating virtual environment...
call venv\Scripts\activate.bat

REM Check Python
echo Testing Python...
python --version
if errorlevel 1 (
    echo Python not found in virtual environment
    pause
    exit /b 1
)

REM Test basic imports
echo Testing FastAPI import...
python -c "import fastapi; print('FastAPI OK')"
if errorlevel 1 (
    echo FastAPI import failed
    pause
    exit /b 1
)

REM Test main app import
echo Testing main app import...
python -c "from app.main import app; print('Main app import OK')"
if errorlevel 1 (
    echo Main app import failed - there are syntax errors
    pause
    exit /b 1
)

echo All imports successful!
echo.
echo Starting server...
echo Access these URLs after server starts:
echo - Health: http://localhost:8000/health
echo - Swagger: http://localhost:8000/swagger  
echo - Docs: http://localhost:8000/docs
echo - Root: http://localhost:8000/
echo.

python start.py
