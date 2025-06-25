# 🐍 Python Patterns Cheat Sheet for Email Campaign Service

## 🎯 Common Patterns You'll See

### 1. **Class Definition** (Blueprint for objects)
```python
class EmailCampaignService:
    """This is a docstring - explains what the class does"""
    
    def __init__(self):
        """Constructor - runs when you create a new object"""
        self.api_key = "your-key-here"  # Instance variable
    
    def process_query(self, query: str) -> str:
        """Method - function that belongs to this class"""
        return f"Processing: {query}"

# Usage:
service = EmailCampaignService()  # Create an object
result = service.process_query("show campaigns")  # Call a method
```

### 2. **Async/Await** (Non-blocking code)
```python
import asyncio

# Regular function (blocking)
def slow_function():
    time.sleep(2)  # Blocks everything for 2 seconds
    return "Done"

# Async function (non-blocking)
async def fast_function():
    await asyncio.sleep(2)  # Other code can run during this wait
    return "Done"

# How to call async functions:
async def main():
    result = await fast_function()  # Wait for result
    print(result)

# Run async code:
asyncio.run(main())
```

### 3. **Type Hints** (Tell Python what types to expect)
```python
from typing import List, Dict, Optional

# Without type hints (old way):
def process_data(items, count, config):
    # What types are these? We don't know!
    pass

# With type hints (new way):
def process_data(
    items: List[str],           # List of strings
    count: int,                 # Integer number
    config: Dict[str, str],     # Dictionary with string keys and values
    optional_param: Optional[str] = None  # String or None, defaults to None
) -> bool:                      # Returns a boolean
    # Now we know exactly what types to expect!
    return True
```

### 4. **Dependency Injection** (Providing objects from outside)
```python
from fastapi import Depends

# Old way (hard to test):
def bad_endpoint():
    service = EmailService()  # Created inside function
    return service.get_data()

# New way (easy to test and swap):
def get_email_service():
    """Factory function - creates the service"""
    return EmailService()

def good_endpoint(
    service: EmailService = Depends(get_email_service)  # Injected automatically
):
    return service.get_data()

# For testing, you can easily provide a mock:
def get_mock_service():
    return MockEmailService()
```

### 5. **Environment Variables** (Configuration)
```python
import os
from pydantic_settings import BaseSettings

# Reading environment variables:
api_key = os.getenv("GEMINI_API_KEY", "default-value")

# Better way with Pydantic:
class Settings(BaseSettings):
    gemini_api_key: str = "default-key"
    database_url: str = "default-connection"
    
    class Config:
        env_file = ".env"  # Reads from .env file

settings = Settings()  # Automatically loads from .env
```

### 6. **Error Handling** (Try/Except)
```python
from fastapi import HTTPException

async def process_query(query: str):
    try:
        # Try to do something that might fail
        result = await call_ai_service(query)
        return result
        
    except ValueError as e:
        # Handle specific error type
        print(f"Invalid input: {e}")
        raise HTTPException(status_code=400, detail="Bad request")
        
    except Exception as e:
        # Handle any other error
        print(f"Unexpected error: {e}")
        raise HTTPException(status_code=500, detail="Internal server error")
        
    finally:
        # This always runs (optional)
        print("Cleanup if needed")
```

### 7. **List Comprehensions** (Elegant way to create lists)
```python
# Old way:
results = []
for item in data:
    if item.status == "active":
        results.append(item.name.upper())

# New way (list comprehension):
results = [item.name.upper() for item in data if item.status == "active"]

# Dictionary comprehension:
status_counts = {status: len(items) for status, items in grouped_data.items()}
```

### 8. **F-strings** (String formatting)
```python
name = "John"
age = 30

# Old ways:
message1 = "Hello " + name + ", you are " + str(age) + " years old"
message2 = "Hello {}, you are {} years old".format(name, age)

# New way (f-strings):
message3 = f"Hello {name}, you are {age} years old"

# With expressions:
message4 = f"Hello {name.upper()}, next year you'll be {age + 1}"
```

### 9. **Decorators** (Modify function behavior)
```python
from functools import wraps

# Simple decorator:
def log_calls(func):
    @wraps(func)
    def wrapper(*args, **kwargs):
        print(f"Calling {func.__name__}")
        result = func(*args, **kwargs)
        print(f"Finished {func.__name__}")
        return result
    return wrapper

# Usage:
@log_calls
def process_data(data):
    return len(data)

# FastAPI uses decorators for routes:
@app.get("/health")
async def health_check():
    return {"status": "healthy"}
```

### 10. **Context Managers** (Automatic cleanup)
```python
# Automatically close files:
with open("data.txt", "r") as file:
    content = file.read()
# File is automatically closed here

# Database connections:
with get_db_connection() as db:
    result = db.execute("SELECT * FROM campaigns")
# Connection is automatically closed
```

## 🔍 How These Patterns Are Used in Our Service

### **In Controllers:**
```python
@router.post("/sql-query")  # Decorator
async def post_sql_query(  # Async function
    request: NaturalLanguageSqlQueryRequestDto,  # Type hint
    orchestrator: NaturalLanguageSqlQueryOrchestrator = Depends(get_orchestrator)  # Dependency injection
):
    try:  # Error handling
        result = await orchestrator.run_async(request.query, request.page_number, request.page_size)
        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error: {e}")  # F-string
```

### **In Services:**
```python
class SemanticNaturalSqlRagService:
    def __init__(self):
        self.knowledge_base: List[KnowledgeItem] = []  # Type hint
        
    async def get_context_for_sql_async(self, user_query: str) -> List[str]:  # Async + type hints
        # List comprehension:
        relevant_items = [item.text for item in self.knowledge_base if self._is_relevant(item, user_query)]
        return relevant_items
```

### **In Configuration:**
```python
class Settings(BaseSettings):  # Inherits from BaseSettings
    gemini_api_key: str  # Type hint
    database_url: str
    
    class Config:
        env_file = ".env"  # Environment variables
```

## 🚀 Quick Commands for Development

```bash
# Activate virtual environment
venv\Scripts\activate

# Install packages
pip install package-name

# Run the server
python start.py

# Run tests
python -m pytest

# Check code style
python -m flake8 app/

# Format code
python -m black app/

# Show installed packages
pip list

# Create requirements file
pip freeze > requirements.txt
```

## 🎯 Debugging Tips

```python
# Add print statements for debugging:
print(f"Debug: user_query = {user_query}")
print(f"Debug: context items = {len(context_items)}")

# Use logging instead of print for production:
import logging
logger = logging.getLogger(__name__)
logger.info(f"Processing query: {user_query}")
logger.error(f"Error occurred: {error}")

# Test individual functions:
if __name__ == "__main__":
    # Test code here
    service = EmailService()
    result = service.test_function()
    print(result)
```

## 🔗 Useful Resources

- **FastAPI Docs**: https://fastapi.tiangolo.com/
- **Python Type Hints**: https://docs.python.org/3/library/typing.html
- **Async Programming**: https://docs.python.org/3/library/asyncio.html
- **Pydantic**: https://pydantic.dev/
- **SQLAlchemy**: https://sqlalchemy.org/

Remember: Don't try to learn everything at once! Start with the basics and gradually understand more complex patterns as you work with the code. 🎓
