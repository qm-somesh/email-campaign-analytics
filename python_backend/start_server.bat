@echo off
echo Starting Email Campaign API Server...
echo ====================================

cd /d "%~dp0"
call venv\Scripts\activate.bat

echo Virtual environment activated
echo Starting server on http://localhost:8000
echo.
echo API Documentation: http://localhost:8000/swagger
echo Health Check: http://localhost:8000/health
echo.

python start.py
