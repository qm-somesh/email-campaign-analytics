# 🐛 Debugging Guide for Your FastAPI Service

## Table of Contents
1. [Types of Debugging](#types-of-debugging)
2. [Setting Up Your Debug Environment](#setting-up-debug-environment)
3. [Python Print Debugging](#print-debugging)
4. [Using Python Debugger (pdb)](#python-debugger)
5. [VS Code Debugging](#vscode-debugging)
6. [FastAPI Specific Debugging](#fastapi-debugging)
7. [Common Issues & Solutions](#common-issues)
8. [Debug Tools & Scripts](#debug-tools)

---

## Types of Debugging

### 1. 🖨️ **Print Debugging** (Beginner Friendly)
- Add `print()` statements to see what's happening
- Simple but effective for basic issues
- **Best for**: Understanding program flow, checking variable values

### 2. 🔍 **Interactive Debugger (pdb)**
- Step through code line by line
- Inspect variables at any point
- **Best for**: Complex logic issues, understanding execution flow

### 3. 🎯 **IDE Debugging (VS Code)**
- Visual debugging with breakpoints
- See all variables in a nice interface
- **Best for**: Professional development, complex debugging

### 4. 📊 **Logging**
- Record what happens when your app runs
- Can be turned on/off without changing code
- **Best for**: Production debugging, long-term monitoring

---

## Setting Up Debug Environment

### 1. Enable Debug Mode in FastAPI

Your `start.py` already has `reload=True` which helps with development:

```python
uvicorn.run(
    "app.main:app",
    host="0.0.0.0",
    port=8000,
    reload=True,  # ✅ This automatically restarts when you change code
    log_level="info"
)
```

### 2. Add Debug Configuration

Create a debug version of your startup script:

```python
# start_debug.py
uvicorn.run(
    "app.main:app",
    host="0.0.0.0",
    port=8000,
    reload=True,
    log_level="debug",  # More detailed logging
    debug=True          # Enable debug mode
)
```

---

## Print Debugging (Start Here!)

### Basic Print Debugging

Add `print()` statements to see what's happening:

```python
def process_query(query: str):
    print(f"🔍 DEBUG: Received query: {query}")
    
    result = some_complex_function(query)
    print(f"🔍 DEBUG: Function returned: {result}")
    
    if result is None:
        print("⚠️ DEBUG: Result is None - this might be a problem!")
    
    return result
```

### Better Print Debugging with Rich Details

```python
import json

def debug_print(label, data):
    """Pretty print debug information"""
    print(f"\n🔍 DEBUG: {label}")
    print("-" * 50)
    if isinstance(data, (dict, list)):
        print(json.dumps(data, indent=2, default=str))
    else:
        print(f"Type: {type(data)}")
        print(f"Value: {data}")
    print("-" * 50)

# Usage:
def your_function(request_data):
    debug_print("Input received", request_data)
    
    processed = process_data(request_data)
    debug_print("After processing", processed)
    
    return processed
```

---

## Python Debugger (pdb)

### Adding Breakpoints

Insert this line where you want to stop and inspect:

```python
import pdb; pdb.set_trace()
```

**Example in your controller:**

```python
# app/controllers/natural_language_sql_controller.py
async def process_natural_language_query(request: QueryRequest):
    import pdb; pdb.set_trace()  # 🛑 Execution will stop here
    
    # You can now inspect variables in the terminal
    print(f"Request: {request}")
    
    result = await orchestrator.process_query(request.query)
    return result
```

### pdb Commands

When the debugger stops, you can use these commands:

- `l` (list) - Show current code
- `n` (next) - Execute next line
- `s` (step) - Step into function calls
- `c` (continue) - Continue execution
- `p variable_name` - Print variable value
- `pp variable_name` - Pretty print variable
- `h` (help) - Show all commands
- `q` (quit) - Stop debugging

### Better Debugger: ipdb

Install enhanced debugger:
```bash
pip install ipdb
```

Use instead of pdb:
```python
import ipdb; ipdb.set_trace()
```

---

## VS Code Debugging

### 1. Create Debug Configuration

Create `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Debug FastAPI",
            "type": "python",
            "request": "launch",
            "program": "${workspaceFolder}/start.py",
            "console": "integratedTerminal",
            "env": {
                "PYTHONPATH": "${workspaceFolder}"
            },
            "args": []
        },
        {
            "name": "Debug FastAPI (Advanced)",
            "type": "python",
            "request": "launch",
            "module": "uvicorn",
            "args": [
                "app.main:app",
                "--host", "0.0.0.0",
                "--port", "8000",
                "--reload"
            ],
            "console": "integratedTerminal",
            "env": {
                "PYTHONPATH": "${workspaceFolder}"
            }
        }
    ]
}
```

### 2. Using VS Code Debugger

1. **Set Breakpoints**: Click left of line numbers (red dots)
2. **Start Debugging**: Press F5 or use Debug menu
3. **Debug Controls**:
   - F10 - Step over (next line)
   - F11 - Step into (enter function)
   - Shift+F11 - Step out (exit function)
   - F5 - Continue

---

## FastAPI Specific Debugging

### 1. Debug API Endpoints

Add debug logging to your endpoints:

```python
# In your controller
import logging
logger = logging.getLogger(__name__)

@router.post("/query")
async def query_endpoint(request: QueryRequest):
    logger.info(f"🔍 Received request: {request.query}")
    
    try:
        result = await process_query(request)
        logger.info(f"✅ Query successful: {result}")
        return result
    except Exception as e:
        logger.error(f"❌ Query failed: {str(e)}")
        logger.exception("Full error details:")  # This shows the full stack trace
        raise
```

### 2. Debug Request/Response

```python
from fastapi import Request
import json

@app.middleware("http")
async def debug_requests(request: Request, call_next):
    # Log incoming request
    print(f"🔍 Incoming: {request.method} {request.url}")
    
    # Log request body (for POST requests)
    if request.method == "POST":
        body = await request.body()
        print(f"🔍 Request body: {body.decode()}")
    
    response = await call_next(request)
    
    # Log response
    print(f"🔍 Response status: {response.status_code}")
    
    return response
```

### 3. Debug Database Connections

```python
# In your database.py or service files
def test_db_connection():
    try:
        # Test your database connection
        print("🔍 Testing database connection...")
        # Your DB connection code here
        print("✅ Database connection successful")
    except Exception as e:
        print(f"❌ Database connection failed: {e}")
        print("🔍 Full error:")
        import traceback
        traceback.print_exc()
```

---

## Common Issues & Solutions

### 1. **Server Won't Start**

```python
# Add to start.py
def diagnose_startup_issues():
    print("🔍 Diagnosing startup issues...")
    
    # Check Python version
    import sys
    print(f"Python version: {sys.version}")
    
    # Check if port is available
    import socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    result = sock.connect_ex(('localhost', 8000))
    if result == 0:
        print("❌ Port 8000 is already in use!")
    else:
        print("✅ Port 8000 is available")
    sock.close()
    
    # Check environment
    import os
    print(f"Current directory: {os.getcwd()}")
    print(f".env file exists: {os.path.exists('.env')}")
    
    # Test imports
    try:
        from app.main import app
        print("✅ Main app imports successfully")
    except Exception as e:
        print(f"❌ Import error: {e}")

# Call this if server fails to start
if __name__ == "__main__":
    diagnose_startup_issues()
```

### 2. **Import Errors**

```python
# Debug import issues
def debug_imports():
    import sys
    print("🔍 Python path:")
    for path in sys.path:
        print(f"  📁 {path}")
    
    print("\n🔍 Testing imports:")
    
    modules_to_test = [
        "fastapi",
        "uvicorn", 
        "app.main",
        "app.controllers.natural_language_sql_controller"
    ]
    
    for module in modules_to_test:
        try:
            __import__(module)
            print(f"✅ {module}")
        except Exception as e:
            print(f"❌ {module}: {e}")
```

### 3. **API Not Responding**

```python
# Test API endpoints
import requests
import json

def test_api_endpoints():
    base_url = "http://localhost:8000"
    
    # Test health endpoint
    try:
        response = requests.get(f"{base_url}/health", timeout=5)
        print(f"Health check: {response.status_code}")
        print(f"Response: {response.text}")
    except Exception as e:
        print(f"❌ Health check failed: {e}")
    
    # Test query endpoint
    try:
        test_data = {"query": "show me all campaigns"}
        response = requests.post(
            f"{base_url}/api/query",
            json=test_data,
            timeout=10
        )
        print(f"Query test: {response.status_code}")
        print(f"Response: {response.text}")
    except Exception as e:
        print(f"❌ Query test failed: {e}")
```

---

## Debug Tools & Scripts

### 1. Environment Checker

```python
# debug_environment.py
def check_environment():
    print("🔍 Environment Check")
    print("=" * 50)
    
    # Python info
    import sys
    print(f"Python version: {sys.version}")
    print(f"Python executable: {sys.executable}")
    
    # Package versions
    packages = ['fastapi', 'uvicorn', 'sqlalchemy', 'pydantic']
    for package in packages:
        try:
            module = __import__(package)
            version = getattr(module, '__version__', 'Unknown')
            print(f"{package}: {version}")
        except ImportError:
            print(f"{package}: ❌ Not installed")
    
    # Environment variables
    import os
    print(f"\nEnvironment variables:")
    for key in ['DATABASE_URL', 'GEMINI_API_KEY', 'LOG_LEVEL']:
        value = os.getenv(key, 'Not set')
        # Hide sensitive values
        if 'KEY' in key or 'PASSWORD' in key:
            value = "***" if value != 'Not set' else 'Not set'
        print(f"  {key}: {value}")
```

### 2. Step-by-Step Request Debugger

```python
# debug_request.py
class RequestDebugger:
    def __init__(self):
        self.steps = []
    
    def step(self, name, data=None):
        import time
        timestamp = time.strftime("%H:%M:%S")
        self.steps.append({
            'time': timestamp,
            'step': name,
            'data': data
        })
        print(f"🔍 [{timestamp}] {name}")
        if data:
            print(f"    Data: {data}")
    
    def summary(self):
        print("\n📊 Debug Summary:")
        print("=" * 50)
        for step in self.steps:
            print(f"[{step['time']}] {step['step']}")
        print("=" * 50)

# Usage in your code:
debugger = RequestDebugger()

async def process_query_with_debug(query):
    debugger.step("Received query", query)
    
    debugger.step("Validating input")
    if not query:
        debugger.step("Validation failed - empty query")
        return None
    
    debugger.step("Processing with RAG service")
    result = await rag_service.process(query)
    
    debugger.step("RAG processing complete", result)
    
    debugger.summary()
    return result
```

---

## Quick Debug Checklist ✅

When something goes wrong, check these in order:

1. **🔍 Check the terminal/console output** - What error messages do you see?
2. **📁 Verify file paths** - Are all files in the right locations?
3. **🐍 Test imports** - Can Python find all your modules?
4. **🌐 Check the network** - Is the server running? Is the port available?
5. **📋 Validate input data** - Is the request data in the correct format?
6. **🔧 Check configuration** - Are environment variables set correctly?
7. **📊 Review logs** - What do the server logs tell you?

## Next Steps

1. **Start with print debugging** - Add `print()` statements to see program flow
2. **Try the debug scripts** I'll create for you
3. **Practice with breakpoints** - Use pdb or VS Code debugger
4. **Learn to read error messages** - They usually tell you exactly what's wrong!

Remember: **Debugging is a skill that improves with practice!** Every bug you fix makes you a better developer. 🚀
