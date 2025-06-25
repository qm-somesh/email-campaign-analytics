@echo off
setlocal enabledelayedexpansion

echo.
echo =====================================================
echo  EmailCampaignReporting Python Backend Setup
echo =====================================================
echo.

cd /d "%~dp0"

echo 🔍 Step 1: Checking Python installation...
echo.

REM Try different Python commands
python --version >nul 2>&1
if not errorlevel 1 (
    echo ✅ Python found via 'python' command
    set PYTHON_CMD=python
    goto :python_found
)

python3 --version >nul 2>&1
if not errorlevel 1 (
    echo ✅ Python found via 'python3' command
    set PYTHON_CMD=python3
    goto :python_found
)

py --version >nul 2>&1
if not errorlevel 1 (
    echo ✅ Python found via 'py' command
    set PYTHON_CMD=py
    goto :python_found
)

REM Python not found
echo.
echo ❌ Python not found!
echo.
echo 📋 Please install Python first:
echo    1. Go to https://python.org/downloads/
echo    2. Download Python 3.9 or higher
echo    3. During installation, CHECK "Add Python to PATH"
echo    4. Restart this script after installation
echo.
echo 🌐 Opening Python download page...
start https://python.org/downloads/
echo.
pause
exit /b 1

:python_found
echo.
echo 🐍 Python version:
%PYTHON_CMD% --version
echo.

echo 🔍 Step 2: Checking virtual environment...
if not exist "venv" (
    echo 📦 Creating virtual environment...
    %PYTHON_CMD% -m venv venv
    if errorlevel 1 (
        echo ❌ Failed to create virtual environment
        echo.
        echo Try running: %PYTHON_CMD% -m pip install --upgrade pip
        pause
        exit /b 1
    )
    echo ✅ Virtual environment created
) else (
    echo ✅ Virtual environment already exists
)

echo.
echo 🔧 Step 3: Activating virtual environment...
if exist "venv\Scripts\activate.bat" (
    call venv\Scripts\activate.bat
    echo ✅ Virtual environment activated
) else (
    echo ❌ Virtual environment activation script not found
    pause
    exit /b 1
)

echo.
echo 📦 Step 4: Installing dependencies...
echo    This may take a few minutes...
echo.

pip install --upgrade pip
if errorlevel 1 (
    echo ⚠️  Warning: Failed to upgrade pip, continuing...
)

pip install -r requirements.txt
if errorlevel 1 (
    echo ❌ Failed to install dependencies
    echo.
    echo 🔍 Trying alternative installation methods...
    echo.
    
    REM Try installing core dependencies individually
    echo Installing core FastAPI dependencies...
    pip install fastapi uvicorn pydantic pydantic-settings
    
    echo Installing database dependencies...
    pip install sqlalchemy pyodbc
    
    echo Installing AI/ML dependencies...
    pip install sentence-transformers scikit-learn numpy httpx
    
    echo Installing development dependencies...
    pip install python-dotenv python-multipart
    
    if errorlevel 1 (
        echo ❌ Alternative installation also failed
        echo.
        echo 💡 Try these manual steps:
        echo    1. pip install --upgrade pip
        echo    2. pip install fastapi uvicorn
        echo    3. pip install sentence-transformers
        pause
        exit /b 1
    )
)

echo ✅ Dependencies installed successfully
echo.

echo 🔧 Step 5: Setting up configuration...
if not exist ".env" (
    echo 📝 Creating .env configuration file...
    copy .env.example .env >nul 2>&1
    if not errorlevel 1 (
        echo ✅ Configuration file created
    ) else (
        echo 📝 Creating minimal .env file...
        (
            echo # Database Configuration ^(optional for testing^)
            echo DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server
            echo.
            echo # Gemini AI Configuration ^(optional for testing^)
            echo GEMINI_API_KEY=
            echo.
            echo # API Configuration
            echo LOG_LEVEL=INFO
        ) > .env
        echo ✅ Basic configuration file created
    )
) else (
    echo ✅ Configuration file already exists
)

echo.
echo 🧪 Step 6: Testing the setup...
echo.

REM Test basic Python imports
%PYTHON_CMD% -c "import fastapi; print('✅ FastAPI imported successfully')" 2>nul
if errorlevel 1 (
    echo ❌ FastAPI import failed
    echo.
    echo Trying to install FastAPI...
    pip install fastapi
)

%PYTHON_CMD% -c "import uvicorn; print('✅ Uvicorn imported successfully')" 2>nul
if errorlevel 1 (
    echo ❌ Uvicorn import failed
    echo.
    echo Trying to install Uvicorn...
    pip install uvicorn
)

REM Test if the main app can be imported (catches syntax errors early)
echo Testing main application import...
%PYTHON_CMD% -c "from app.main import app; print('✅ Main application imported successfully')" 2>temp_error.log
if errorlevel 1 (
    echo ❌ Main application import failed
    echo.
    echo 🔍 Error details:
    type temp_error.log
    del temp_error.log >nul 2>&1
    echo.
    echo 🔍 There might be a syntax error in the application code.
    echo    Attempting to start server anyway (it may show more detailed error info)...
    echo.
) else (
    del temp_error.log >nul 2>&1
)

echo.
echo 🚀 Step 7: Starting the Python backend server...
echo.
echo    🌐 API will be available at: http://localhost:8000
echo    📚 Documentation at: http://localhost:8000/swagger
echo    ❤️  Health check at: http://localhost:8000/health
echo.
echo    Starting server... (this may take a moment)

REM Start the server and capture the process ID
start /B %PYTHON_CMD% start.py

REM Wait for server to start (check multiple times)
echo    Waiting for server to start...
set SERVER_READY=0
for /L %%i in (1,1,10) do (
    timeout /t 2 /nobreak >nul
    powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:8000/health' -TimeoutSec 2 -UseBasicParsing; exit 0 } catch { exit 1 }" >nul 2>&1
    if not errorlevel 1 (
        set SERVER_READY=1
        goto :server_ready
    )
    echo    Attempt %%i/10: Server not ready yet...
)

:server_ready
if %SERVER_READY%==1 (
    echo.
    echo ✅ Server started successfully!
    echo    🌐 Opening health check in browser...
    start http://localhost:8000/health
    echo.
    echo 🎉 Installation complete!
    echo    Your Python backend is now running at: http://localhost:8000
    echo    API Documentation: http://localhost:8000/swagger
    echo.
    echo    Press Ctrl+C in the server window to stop the server
) else (
    echo.
    echo ❌ Server failed to start within 20 seconds
    echo.
    echo 🔍 Troubleshooting steps:
    echo    1. Check if there are any error messages above
    echo    2. Try running manually: %PYTHON_CMD% start.py
    echo    3. Check if port 8000 is already in use
    echo    4. Verify all dependencies installed correctly
    echo.
    echo 💡 You can still try to access the server manually:
    echo    - Wait a few more seconds and try: http://localhost:8000/health
    echo    - Or run the test script: %PYTHON_CMD% test_api.py
)
echo.
pause
