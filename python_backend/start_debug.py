#!/usr/bin/env python3
"""
Debug startup script for EmailCampaignReporting Python Backend
This version includes extensive debugging and diagnostic information.
"""

import sys
import os
import uvicorn
import logging
import time
import socket
import traceback
from pathlib import Path

# Add the current directory to Python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def setup_debug_logging():
    """Set up detailed logging for debugging"""
    logging.basicConfig(
        level=logging.DEBUG,
        format='%(asctime)s - %(name)s - %(levelname)s - %(filename)s:%(lineno)d - %(message)s',
        handlers=[
            logging.StreamHandler(sys.stdout),
            logging.FileHandler('debug.log', mode='w')
        ]
    )
    
    # Also log to file
    logger = logging.getLogger(__name__)
    logger.info("=" * 60)
    logger.info("DEBUG MODE: EmailCampaignReporting Python Backend")
    logger.info("=" * 60)
    return logger

def check_python_environment():
    """Check Python environment and dependencies"""
    print("\n🔍 STEP 1: Python Environment Check")
    print("-" * 50)
    
    # Python version
    print(f"🐍 Python version: {sys.version}")
    print(f"🐍 Python executable: {sys.executable}")
    print(f"📁 Current working directory: {os.getcwd()}")
    print(f"📁 Script location: {__file__}")
    
    # Python path
    print(f"\n📂 Python path:")
    for i, path in enumerate(sys.path):
        print(f"  {i}: {path}")
    
    return True

def check_dependencies():
    """Check if required packages are installed"""
    print("\n🔍 STEP 2: Dependency Check")
    print("-" * 50)
    
    required_packages = [
        'fastapi',
        'uvicorn', 
        'pydantic',
        'sqlalchemy',
        'python-dotenv',
        'httpx'
    ]
    
    missing_packages = []
    
    for package in required_packages:
        try:
            module = __import__(package.replace('-', '_'))
            version = getattr(module, '__version__', 'Unknown')
            print(f"✅ {package}: {version}")
        except ImportError as e:
            print(f"❌ {package}: Not installed ({e})")
            missing_packages.append(package)
    
    if missing_packages:
        print(f"\n⚠️ Missing packages: {', '.join(missing_packages)}")
        print("💡 Install with: pip install " + " ".join(missing_packages))
        return False
    
    print("✅ All dependencies are installed!")
    return True

def check_project_structure():
    """Verify project files exist"""
    print("\n🔍 STEP 3: Project Structure Check")
    print("-" * 50)
    
    required_files = [
        'app/__init__.py',
        'app/main.py',
        'app/controllers/__init__.py',
        'app/controllers/natural_language_sql_controller.py',
        'app/services/__init__.py',
        'app/config/__init__.py',
        'app/config/settings.py'
    ]
    
    missing_files = []
    
    for file_path in required_files:
        if os.path.exists(file_path):
            size = os.path.getsize(file_path)
            print(f"✅ {file_path} ({size} bytes)")
        else:
            print(f"❌ {file_path} - Missing!")
            missing_files.append(file_path)
    
    if missing_files:
        print(f"\n⚠️ Missing files: {missing_files}")
        return False
    
    print("✅ All required files are present!")
    return True

def check_configuration():
    """Check configuration files and environment variables"""
    print("\n🔍 STEP 4: Configuration Check")
    print("-" * 50)
    
    # Check .env file
    env_file = '.env'
    if os.path.exists(env_file):
        print(f"✅ {env_file} exists")
        
        # Read and show environment variables (hiding sensitive data)
        from dotenv import load_dotenv
        load_dotenv()
        
        env_vars = {
            'DATABASE_URL': os.getenv('DATABASE_URL'),
            'GEMINI_API_KEY': os.getenv('GEMINI_API_KEY'),
            'LOG_LEVEL': os.getenv('LOG_LEVEL', 'INFO'),
            'CORS_ORIGINS': os.getenv('CORS_ORIGINS', '*')
        }
        
        for key, value in env_vars.items():
            if value:
                # Hide sensitive information
                if 'KEY' in key or 'PASSWORD' in key or 'URL' in key:
                    display_value = f"***{value[-4:]}" if len(value) > 4 else "***"
                else:
                    display_value = value
                print(f"  ✅ {key}: {display_value}")
            else:
                print(f"  ⚠️ {key}: Not set")
    else:
        print(f"❌ {env_file} does not exist")
        return False
    
    return True

def check_imports():
    """Test importing main application components"""
    print("\n🔍 STEP 5: Import Test")
    print("-" * 50)
    
    import_tests = [
        ('app', 'Main app package'),
        ('app.main', 'Main FastAPI app'),
        ('app.main:app', 'FastAPI app instance'),
        ('app.controllers.natural_language_sql_controller', 'Query controller'),
        ('app.config.settings', 'Settings configuration'),
    ]
    
    failed_imports = []
    
    for module_name, description in import_tests:
        try:
            if ':' in module_name:
                # Special case for app instance
                module_path, attr_name = module_name.split(':')
                module = __import__(module_path, fromlist=[attr_name])
                getattr(module, attr_name)
                print(f"✅ {description}: OK")
            else:
                __import__(module_name)
                print(f"✅ {description}: OK")
        except Exception as e:
            print(f"❌ {description}: {e}")
            print(f"   Full error: {traceback.format_exc()}")
            failed_imports.append((module_name, str(e)))
    
    if failed_imports:
        print(f"\n⚠️ Failed imports:")
        for module, error in failed_imports:
            print(f"  - {module}: {error}")
        return False
    
    print("✅ All imports successful!")
    return True

def check_port_availability(port=8000):
    """Check if the port is available"""
    print(f"\n🔍 STEP 6: Port {port} Availability Check")
    print("-" * 50)
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        result = sock.connect_ex(('localhost', port))
        if result == 0:
            print(f"❌ Port {port} is already in use!")
            print("💡 Try stopping any running servers or use a different port")
            return False
        else:
            print(f"✅ Port {port} is available")
            return True
    finally:
        sock.close()

def create_debug_env_if_missing():
    """Create .env file if it doesn't exist"""
    env_file = '.env'
    if not os.path.exists(env_file):
        print(f"\n📝 Creating debug .env file...")
        
        debug_env_content = """# Database Configuration (for development/testing)
DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server

# Gemini AI Configuration (optional for testing)
GEMINI_API_KEY=your_gemini_api_key_here

# API Configuration
LOG_LEVEL=DEBUG
CORS_ORIGINS=*

# Debug Configuration
DEBUG=true
ENVIRONMENT=development
"""
        
        with open(env_file, 'w') as f:
            f.write(debug_env_content)
        
        print(f"✅ Created {env_file} with debug settings")
        print("💡 Edit this file with your actual configuration")

def run_diagnostics():
    """Run all diagnostic checks"""
    print("🚀 Starting Debug Diagnostics...")
    print("=" * 60)
    
    checks = [
        ("Python Environment", check_python_environment),
        ("Dependencies", check_dependencies), 
        ("Project Structure", check_project_structure),
        ("Configuration", check_configuration),
        ("Imports", check_imports),
        ("Port Availability", check_port_availability)
    ]
    
    all_passed = True
    
    for check_name, check_func in checks:
        try:
            success = check_func()
            if not success:
                all_passed = False
        except Exception as e:
            print(f"❌ {check_name} check failed: {e}")
            traceback.print_exc()
            all_passed = False
        
        time.sleep(0.5)  # Brief pause between checks
    
    print("\n" + "=" * 60)
    if all_passed:
        print("🎉 ALL DIAGNOSTICS PASSED! Ready to start server.")
    else:
        print("⚠️ SOME DIAGNOSTICS FAILED! Check the issues above.")
        response = input("\nContinue anyway? (y/n): ").lower()
        if response not in ['y', 'yes']:
            print("👋 Exiting debug startup.")
            return False
    
    return True

def start_debug_server():
    """Start the FastAPI server with debug configuration"""
    print("\n🚀 STEP 7: Starting Debug Server")
    print("-" * 50)
    
    print("🔧 Debug server configuration:")
    print("  - Host: 0.0.0.0 (accessible from any network interface)")
    print("  - Port: 8000")
    print("  - Reload: True (auto-restart on file changes)")
    print("  - Log Level: debug")
    print("  - Debug Mode: True")
    print("  - Log File: debug.log")
    
    print("\n🌐 Server will be available at:")
    print("  - http://localhost:8000 (local access)")
    print("  - http://127.0.0.1:8000 (local access)")
    print("  - http://localhost:8000/swagger (API documentation)")
    print("  - http://localhost:8000/health (health check)")
    
    input("\nPress Enter to start the server...")
    
    try:
        # Import the FastAPI app to make sure it's ready
        print("📦 Loading FastAPI application...")
        from app.main import app
        print("✅ FastAPI application loaded successfully")
        
        print("🚀 Starting uvicorn server...")
        uvicorn.run(
            "app.main:app",
            host="0.0.0.0",
            port=8000,
            reload=True,
            log_level="debug",
            debug=True,
            access_log=True
        )
        
    except KeyboardInterrupt:
        print("\n\n👋 Server stopped by user (Ctrl+C)")
    except Exception as e:
        print(f"\n❌ Server failed to start: {e}")
        print("\n🔍 Full error traceback:")
        traceback.print_exc()
        
        print("\n💡 Troubleshooting suggestions:")
        print("  1. Check if all dependencies are installed: pip install -r requirements.txt")
        print("  2. Verify your .env configuration")
        print("  3. Check if port 8000 is already in use")
        print("  4. Look at the debug.log file for more details")
        
        return False
    
    return True

def main():
    """Main debug startup function"""
    
    # Set up debug logging
    logger = setup_debug_logging()
    
    try:
        # Create .env if missing
        create_debug_env_if_missing()
        
        # Run diagnostics
        if not run_diagnostics():
            return 1
        
        # Start the server
        if not start_debug_server():
            return 1
            
        return 0
        
    except KeyboardInterrupt:
        logger.info("Debug startup interrupted by user")
        return 0
    except Exception as e:
        logger.error(f"Unexpected error during debug startup: {e}")
        logger.exception("Full error details:")
        return 1

if __name__ == "__main__":
    exit_code = main()
    sys.exit(exit_code)
