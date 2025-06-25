# 🐍 Python Backend Service - Beginner's Guide

## 📁 Project Structure (What each folder does)

```
python_backend/
├── 📄 start.py                    # ⭐ MAIN FILE - Starts the web server
├── 📄 setup.bat                   # 🔧 Installation script (runs once)
├── 📄 .env                        # ⚙️ Configuration file (API keys, database settings)
├── 📁 venv/                       # 🏠 Virtual environment (Python packages live here)
├── 📁 app/                        # 📦 Main application code
│   ├── 📄 main.py                 # 🌐 Web server setup (FastAPI app)
│   ├── 📁 config/                 # ⚙️ Configuration management
│   │   ├── 📄 settings.py         # 📋 App settings (reads from .env)
│   │   └── 📄 database.py         # 🗄️ Database connection setup
│   ├── 📁 controllers/            # 🎮 API endpoints (what users can call)
│   │   └── 📄 natural_language_sql_controller.py  # 🗣️ Main API endpoint
│   ├── 📁 services/               # 🧠 Business logic (the smart stuff)
│   │   └── 📁 natural_sql_rag/    # 🤖 AI and database services
│   │       ├── 📄 orchestrator.py      # 🎭 Coordinates everything
│   │       ├── 📄 rag_service.py       # 📚 Knowledge base (RAG)
│   │       ├── 📄 gemini_service.py    # 🤖 AI service (Gemini)
│   │       └── 📄 mock_rag_service.py  # 🎪 Fake data for testing
│   ├── 📁 models/                 # 📝 Data structures
│   │   └── 📁 dto/                # 📤 Request/Response formats
│   └── 📁 utils/                  # 🛠️ Helper functions
```

## 🔄 Flow of Execution (Step by Step)

### **Step 1: User Makes a Request** 🗣️
```
User types: "Show me campaigns from last month"
↓
Sends HTTP POST request to: /api/natural-language-sql-query/sql-query
```

### **Step 2: Controller Receives Request** 🎮
```python
# File: controllers/natural_language_sql_controller.py
# This is like a "receptionist" that receives all requests

@router.post("/sql-query")
async def post_sql_query(request: NaturalLanguageSqlQueryRequestDto):
    # 1. Validates the request (is it valid?)
    # 2. Calls the orchestrator to do the work
    # 3. Returns the result to the user
```

### **Step 3: Orchestrator Coordinates Everything** 🎭
```python
# File: services/natural_sql_rag/orchestrator.py
# This is like a "project manager" that coordinates all the work

class NaturalLanguageSqlQueryOrchestrator:
    async def run_async(self, user_query, page_number, page_size):
        # Step 3a: Get context from RAG service
        # Step 3b: Send to Gemini AI to generate SQL
        # Step 3c: Execute SQL against database
        # Step 3d: Format and return results
```

### **Step 4: RAG Service Finds Relevant Context** 📚
```python
# File: services/natural_sql_rag/rag_service.py
# This is like a "librarian" that finds relevant information

class SemanticNaturalSqlRagService:
    async def get_context_for_sql_async(self, user_query):
        # 1. Takes user question
        # 2. Searches knowledge base for relevant info
        # 3. Returns context like: "Use DateCreated for time filters"
```

### **Step 5: Gemini AI Generates SQL** 🤖
```python
# File: services/natural_sql_rag/gemini_service.py
# This is like a "translator" that converts English to SQL

class GeminiSqlGeneratorService:
    async def generate_sql_async(self, prompt):
        # 1. Takes user question + context
        # 2. Sends to Google Gemini AI
        # 3. Gets back SQL query
        # 4. Returns: "SELECT ... FROM EmailTrigger WHERE ..."
```

### **Step 6: Execute SQL and Return Results** 🗄️
```python
# Back in orchestrator.py
# 1. Run the SQL against your database
# 2. Get rows of data back
# 3. Format as JSON
# 4. Add pagination (page 1 of 5, etc.)
# 5. Send back to user
```

## 🔧 Key Python Concepts Used

### **1. FastAPI Framework** 🌐
```python
from fastapi import FastAPI

app = FastAPI()  # Creates a web server

@app.get("/health")  # When someone visits /health
async def health_check():
    return {"status": "healthy"}  # Return this JSON
```

### **2. Async/Await (Non-blocking code)** ⚡
```python
# OLD WAY (blocking - waits for each step)
def old_way():
    result1 = call_ai_service()      # Wait 2 seconds
    result2 = call_database()       # Wait 1 second
    return combine(result1, result2) # Total: 3 seconds

# NEW WAY (async - can do multiple things at once)
async def new_way():
    result1 = await call_ai_service()   # Can handle other requests while waiting
    result2 = await call_database()    # More efficient!
    return combine(result1, result2)
```

### **3. Dependency Injection** 🔌
```python
# Instead of creating objects inside functions:
def bad_way():
    rag_service = SemanticNaturalSqlRagService()  # Hard to test/change
    
# We "inject" them from outside:
def good_way(rag_service: SemanticNaturalSqlRagService = Depends(get_rag_service)):
    # rag_service is provided automatically by FastAPI
    # Easy to swap with mock versions for testing!
```

### **4. Environment Variables (.env file)** 🔐
```python
# Instead of hardcoding secrets in code:
api_key = "AIzaSyDBrIDXPfipjL74HTWJskMfcGIkdAB72Wg"  # BAD! Visible to everyone

# We store them in .env file:
GEMINI_API_KEY=AIzaSyDBrIDXPfipjL74HTWJskMfcGIkdAB72Wg

# And read them in code:
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    gemini_api_key: str  # Automatically reads from .env
```

## 🚀 How to Start the Service

### **Simple Version:**
```bash
1. Double-click setup.bat          # Install everything
2. Double-click start.bat          # Start the server
3. Go to http://localhost:8000/swagger  # Use the web interface
```

### **Manual Version:**
```bash
1. Open PowerShell
2. cd "D:\Dev\EmailCampaignReporting\python_backend"
3. venv\Scripts\activate           # Activate virtual environment
4. python start.py                 # Start the server
```

## 📝 Data Flow Example

Let's trace a real example:

### **Input:**
```json
{
  "query": "Show campaigns from last month",
  "page_number": 1,
  "page_size": 10
}
```

### **Step-by-step processing:**

**1. RAG Service finds context:**
```
"For 'last month': add AND eo.DateCreated >= DATEADD(month, -1, GETDATE())"
"Always use LEFT JOINs: et LEFT JOIN eo ON..."
```

**2. Gemini AI generates SQL:**
```sql
SELECT 
    et.Description AS StrategyName,
    COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
    SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount
FROM EmailTrigger et
LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
WHERE et.IsActive = 1 
    AND eo.DateCreated >= DATEADD(month, -1, GETDATE())
GROUP BY et.Description
ORDER BY et.Description
```

**3. Database returns data:**
```
StrategyName: "Newsletter Campaign"
TotalEmails: 1500
DeliveredCount: 1450
...
```

**4. Final JSON response:**
```json
{
  "items": [
    {
      "strategyName": "Newsletter Campaign",
      "totalEmails": 1500,
      "deliveredCount": 1450,
      "openedCount": 725,
      ...
    }
  ],
  "total_items": 5,
  "page_number": 1,
  "page_size": 10
}
```

## 🎯 Why This Architecture?

### **Separation of Concerns** 📦
- **Controllers**: Handle web requests (like a receptionist)
- **Services**: Do the business logic (like specialists)
- **Models**: Define data structures (like forms)
- **Utils**: Helper functions (like tools)

### **Testability** 🧪
```python
# Easy to test individual parts:
def test_rag_service():
    service = MockNaturalSqlRagService()  # Use fake data
    result = service.get_context("test query")
    assert len(result) > 0
```

### **Scalability** 📈
- Can handle many requests at once (async)
- Easy to add new features
- Can swap components (use different AI models)

## 🛠️ Common Debugging Steps

### **If server won't start:**
```bash
1. Check virtual environment: venv\Scripts\activate
2. Check imports: python -c "from app.main import app; print('OK')"
3. Check .env file: Make sure API keys are set
4. Check port: netstat -ano | findstr :8000
```

### **If API returns errors:**
```bash
1. Check logs in terminal
2. Test with simple query: "Show all campaigns"
3. Check database connection
4. Verify Gemini API key is working
```

This architecture follows Python best practices and makes the code easy to understand, test, and maintain! 🎉
