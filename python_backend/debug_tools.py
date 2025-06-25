#!/usr/bin/env python3
"""
Debug Tools for FastAPI Service
Collection of utilities to help debug your email campaign backend.
"""

import sys
import os
import json
import time
import traceback
import requests
from datetime import datetime
from pathlib import Path

# Add the current directory to Python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

class DebugLogger:
    """Enhanced logging for debugging"""
    
    def __init__(self, name="DEBUG"):
        self.name = name
        self.start_time = time.time()
        self.steps = []
    
    def log(self, message, data=None, level="INFO"):
        """Log a debug message with optional data"""
        timestamp = datetime.now().strftime("%H:%M:%S.%f")[:-3]
        elapsed = time.time() - self.start_time
        
        step_info = {
            'timestamp': timestamp,
            'elapsed': f"{elapsed:.3f}s",
            'level': level,
            'message': message,
            'data': data
        }
        
        self.steps.append(step_info)
        
        # Console output
        prefix = {
            'INFO': '🔍',
            'SUCCESS': '✅', 
            'WARNING': '⚠️',
            'ERROR': '❌',
            'DEBUG': '🐛'
        }.get(level, 'ℹ️')
        
        print(f"{prefix} [{timestamp}] (+{elapsed:.3f}s) {message}")
        
        if data is not None:
            if isinstance(data, (dict, list)):
                print(f"    📊 Data: {json.dumps(data, indent=4, default=str)}")
            else:
                print(f"    📊 Data: {data} (type: {type(data).__name__})")
    
    def success(self, message, data=None):
        self.log(message, data, "SUCCESS")
    
    def warning(self, message, data=None):
        self.log(message, data, "WARNING")
    
    def error(self, message, data=None):
        self.log(message, data, "ERROR")
    
    def debug(self, message, data=None):
        self.log(message, data, "DEBUG")
    
    def summary(self):
        """Print a summary of all debug steps"""
        print("\n" + "="*60)
        print(f" 📊 DEBUG SUMMARY - {self.name}")
        print("="*60)
        
        for i, step in enumerate(self.steps, 1):
            status = {
                'SUCCESS': '✅',
                'ERROR': '❌', 
                'WARNING': '⚠️'
            }.get(step['level'], '🔍')
            
            print(f"{i:2d}. {status} [{step['timestamp']}] {step['message']}")
        
        total_time = time.time() - self.start_time
        print(f"\n⏱️ Total execution time: {total_time:.3f} seconds")
        print("="*60)

def test_server_health():
    """Test if the FastAPI server is running and responsive"""
    logger = DebugLogger("Server Health Check")
    
    base_url = "http://localhost:8000"
    
    try:
        logger.log("Testing server health endpoint...")
        
        # Test health endpoint
        health_response = requests.get(f"{base_url}/health", timeout=5)
        if health_response.status_code == 200:
            logger.success("Health endpoint responding", {
                "status_code": health_response.status_code,
                "response": health_response.text
            })
        else:
            logger.warning("Health endpoint returned non-200 status", {
                "status_code": health_response.status_code,
                "response": health_response.text
            })
        
        # Test root endpoint
        logger.log("Testing root endpoint...")
        root_response = requests.get(base_url, timeout=5)
        logger.success("Root endpoint responding", {
            "status_code": root_response.status_code
        })
        
        # Test OpenAPI docs
        logger.log("Testing OpenAPI documentation...")
        docs_response = requests.get(f"{base_url}/docs", timeout=5)
        if docs_response.status_code == 200:
            logger.success("API documentation is accessible")
        else:
            logger.warning("API documentation not accessible", {
                "status_code": docs_response.status_code
            })
        
        return True
        
    except requests.exceptions.ConnectionError:
        logger.error("Cannot connect to server", {
            "url": base_url,
            "suggestion": "Make sure the server is running with: python start.py"
        })
        return False
    except requests.exceptions.Timeout:
        logger.error("Server response timeout", {
            "timeout": "5 seconds",
            "suggestion": "Server might be overloaded or starting up"
        })
        return False
    except Exception as e:
        logger.error("Unexpected error testing server", {
            "error": str(e),
            "type": type(e).__name__
        })
        return False
    finally:
        logger.summary()

def test_api_endpoints():
    """Test the main API endpoints with sample data"""
    logger = DebugLogger("API Endpoints Test")
    
    base_url = "http://localhost:8000"
    
    try:
        # Test query endpoint
        logger.log("Testing natural language query endpoint...")
        
        test_queries = [
            {"query": "show me all campaigns"},
            {"query": "how many emails were sent last month?"},
            {"query": "what is the open rate for campaign ABC123?"}
        ]
        
        for i, test_data in enumerate(test_queries, 1):
            logger.log(f"Testing query {i}: {test_data['query']}")
            
            try:
                response = requests.post(
                    f"{base_url}/api/query",
                    json=test_data,
                    headers={"Content-Type": "application/json"},
                    timeout=30
                )
                
                if response.status_code == 200:
                    logger.success(f"Query {i} successful", {
                        "status_code": response.status_code,
                        "response_preview": response.text[:200] + "..." if len(response.text) > 200 else response.text
                    })
                else:
                    logger.warning(f"Query {i} returned non-200 status", {
                        "status_code": response.status_code,
                        "response": response.text
                    })
                    
            except requests.exceptions.Timeout:
                logger.warning(f"Query {i} timed out (30s)")
            except Exception as e:
                logger.error(f"Query {i} failed", {
                    "error": str(e),
                    "type": type(e).__name__
                })
        
        return True
        
    except Exception as e:
        logger.error("Unexpected error testing endpoints", {
            "error": str(e),
            "type": type(e).__name__
        })
        return False
    finally:
        logger.summary()

def debug_imports():
    """Debug import issues in the application"""
    logger = DebugLogger("Import Debug")
    
    # Test core Python imports
    core_imports = [
        "sys", "os", "json", "time", "logging"
    ]
    
    logger.log("Testing core Python imports...")
    for module in core_imports:
        try:
            __import__(module)
            logger.success(f"Core import: {module}")
        except Exception as e:
            logger.error(f"Core import failed: {module}", str(e))
    
    # Test third-party dependencies
    third_party_imports = [
        "fastapi", "uvicorn", "pydantic", "sqlalchemy", 
        "dotenv", "httpx", "sentence_transformers"
    ]
    
    logger.log("Testing third-party dependencies...")
    missing_deps = []
    
    for module in third_party_imports:
        try:
            imported = __import__(module.replace('-', '_'))
            version = getattr(imported, '__version__', 'Unknown')
            logger.success(f"Dependency: {module} (v{version})")
        except ImportError as e:
            logger.error(f"Missing dependency: {module}", str(e))
            missing_deps.append(module)
        except Exception as e:
            logger.warning(f"Import issue: {module}", str(e))
    
    # Test application imports
    app_imports = [
        "app",
        "app.main", 
        "app.controllers",
        "app.controllers.natural_language_sql_controller",
        "app.services",
        "app.services.natural_sql_rag",
        "app.config",
        "app.config.settings"
    ]
    
    logger.log("Testing application imports...")
    failed_app_imports = []
    
    for module in app_imports:
        try:
            __import__(module)
            logger.success(f"App import: {module}")
        except ImportError as e:
            logger.error(f"App import failed: {module}", str(e))
            failed_app_imports.append((module, str(e)))
        except Exception as e:
            logger.error(f"App import error: {module}", {
                "error": str(e),
                "type": type(e).__name__,
                "traceback": traceback.format_exc()
            })
            failed_app_imports.append((module, str(e)))
    
    # Summary and recommendations
    if missing_deps:
        logger.warning("Missing dependencies found", {
            "missing": missing_deps,
            "fix": f"pip install {' '.join(missing_deps)}"
        })
    
    if failed_app_imports:
        logger.error("Application import failures", {
            "failed_imports": failed_app_imports,
            "suggestion": "Check for syntax errors in the listed modules"
        })
    
    logger.summary()
    return len(missing_deps) == 0 and len(failed_app_imports) == 0

def debug_configuration():
    """Debug configuration and environment setup"""
    logger = DebugLogger("Configuration Debug")
    
    # Check environment files
    config_files = ['.env', '.env.example', 'requirements.txt', 'app/config/settings.py']
    
    logger.log("Checking configuration files...")
    for file_path in config_files:
        if os.path.exists(file_path):
            size = os.path.getsize(file_path)
            logger.success(f"Config file found: {file_path} ({size} bytes)")
        else:
            logger.warning(f"Config file missing: {file_path}")
    
    # Check environment variables
    logger.log("Checking environment variables...")
    
    try:
        from dotenv import load_dotenv
        load_dotenv()
        
        env_vars = {
            'DATABASE_URL': os.getenv('DATABASE_URL'),
            'GEMINI_API_KEY': os.getenv('GEMINI_API_KEY'),
            'LOG_LEVEL': os.getenv('LOG_LEVEL', 'INFO'),
            'CORS_ORIGINS': os.getenv('CORS_ORIGINS', '*'),
            'DEBUG': os.getenv('DEBUG', 'false')
        }
        
        for key, value in env_vars.items():
            if value:
                # Mask sensitive values
                if 'KEY' in key or 'PASSWORD' in key:
                    display_value = f"***{value[-4:]}" if len(value) > 4 else "***"
                elif 'URL' in key and len(value) > 20:
                    display_value = f"{value[:10]}***{value[-10:]}"
                else:
                    display_value = value
                
                logger.success(f"Environment variable: {key} = {display_value}")
            else:
                logger.warning(f"Environment variable not set: {key}")
    
    except ImportError:
        logger.error("python-dotenv not installed", {
            "fix": "pip install python-dotenv"
        })
    except Exception as e:
        logger.error("Error loading environment variables", str(e))
    
    # Test configuration loading
    logger.log("Testing configuration loading...")
    try:
        from app.config.settings import get_settings
        settings = get_settings()
        logger.success("Settings loaded successfully", {
            "log_level": getattr(settings, 'log_level', 'Unknown'),
            "debug_mode": getattr(settings, 'debug', 'Unknown')
        })
    except Exception as e:
        logger.error("Failed to load settings", {
            "error": str(e),
            "traceback": traceback.format_exc()
        })
    
    logger.summary()

def run_full_diagnostics():
    """Run complete diagnostic suite"""
    print("🚀 Starting Full Diagnostic Suite")
    print("=" * 70)
    
    tests = [
        ("Import Debug", debug_imports),
        ("Configuration Debug", debug_configuration),
        ("Server Health Check", test_server_health),
        ("API Endpoints Test", test_api_endpoints)
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        print(f"\n▶️ Running: {test_name}")
        print("-" * 50)
        
        try:
            result = test_func()
            results[test_name] = "PASSED" if result else "FAILED"
        except Exception as e:
            print(f"❌ Test crashed: {e}")
            results[test_name] = "CRASHED"
        
        time.sleep(1)  # Brief pause between tests
    
    # Final summary
    print("\n" + "=" * 70)
    print(" 📊 DIAGNOSTIC RESULTS SUMMARY")
    print("=" * 70)
    
    for test_name, result in results.items():
        status_icon = {
            "PASSED": "✅",
            "FAILED": "❌", 
            "CRASHED": "💥"
        }.get(result, "❓")
        
        print(f"{status_icon} {test_name}: {result}")
    
    passed_count = sum(1 for r in results.values() if r == "PASSED")
    total_count = len(results)
    
    print(f"\n📈 Score: {passed_count}/{total_count} tests passed")
    
    if passed_count == total_count:
        print("🎉 All diagnostics passed! Your service is healthy.")
    elif passed_count >= total_count * 0.75:
        print("⚠️ Most tests passed. Check failed tests for minor issues.")
    else:
        print("🔧 Several issues found. Address failed tests before proceeding.")
    
    print("=" * 70)

def interactive_debug_menu():
    """Interactive debugging menu"""
    while True:
        print("\n" + "="*50)
        print(" 🐛 FastAPI Debug Tools Menu")
        print("="*50)
        print("1. 🔍 Check imports")
        print("2. ⚙️ Debug configuration") 
        print("3. 🌐 Test server health")
        print("4. 📡 Test API endpoints")
        print("5. 🔄 Run full diagnostics")
        print("6. 📝 View debug guide")
        print("0. 👋 Exit")
        print("-"*50)
        
        choice = input("Select option (0-6): ").strip()
        
        if choice == "1":
            debug_imports()
        elif choice == "2":
            debug_configuration()
        elif choice == "3":
            test_server_health()
        elif choice == "4":
            test_api_endpoints()
        elif choice == "5":
            run_full_diagnostics()
        elif choice == "6":
            print("\n📖 Check DEBUGGING_GUIDE.md for detailed instructions!")
        elif choice == "0":
            print("👋 Happy debugging!")
            break
        else:
            print("❌ Invalid choice. Please select 0-6.")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        command = sys.argv[1].lower()
        if command == "imports":
            debug_imports()
        elif command == "config":
            debug_configuration()
        elif command == "health":
            test_server_health()
        elif command == "endpoints":
            test_api_endpoints()
        elif command == "full":
            run_full_diagnostics()
        else:
            print(f"Unknown command: {command}")
            print("Available commands: imports, config, health, endpoints, full")
    else:
        interactive_debug_menu()
