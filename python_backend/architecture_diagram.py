"""
Visual Architecture Diagram (Text-based)
This shows how all the components work together
"""

ARCHITECTURE_DIAGRAM = """
🌐 USER INTERFACE (Swagger UI / Client App)
    │
    │ HTTP POST Request
    │ {"query": "Show campaigns from last month"}
    ▼
┌─────────────────────────────────────────────────────────────┐
│  🎮 CONTROLLER (natural_language_sql_controller.py)        │
│  • Receives HTTP requests                                   │
│  • Validates input                                          │
│  • Returns HTTP responses                                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      │ Calls orchestrator.run_async()
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  🎭 ORCHESTRATOR (orchestrator.py)                         │
│  • Coordinates the entire workflow                          │
│  • Manages the 4-step process                              │
│  • Handles errors and pagination                           │
└─────────┬───────────────────────────────────┬─────────────┘
          │                                   │
          │ 1. Get context                    │ 3. Execute SQL
          ▼                                   ▼
┌─────────────────────┐              ┌─────────────────────┐
│ 📚 RAG SERVICE      │              │ 🗄️  DATABASE        │
│ (rag_service.py)    │              │ (SQL Server)        │
│                     │              │                     │
│ • Knowledge base    │              │ • EmailTrigger      │
│ • Embeddings        │              │ • EmailOutbox       │
│ • Semantic search   │              │ • EmailStatus       │
│ • Context retrieval │              │ • WebhookLogs       │
└─────────────────────┘              └─────────────────────┘
          │
          │ 2. Generate SQL
          ▼
┌─────────────────────────────────────────────────────────────┐
│  🤖 GEMINI AI SERVICE (gemini_service.py)                  │
│  • Calls Google Gemini API                                 │
│  • Converts natural language + context → SQL               │
│  • Uses the API key: AIzaSyDBrIDXPfipjL74HTWJskMfcGIkdAB72Wg │
└─────────────────────────────────────────────────────────────┘

DATA FLOW:
═══════════
Input:  "Show campaigns from last month"
        ↓
Step 1: RAG finds context: "For last month use DateCreated >= DATEADD(month, -1, GETDATE())"
        ↓
Step 2: Gemini generates: "SELECT et.Description... WHERE eo.DateCreated >= DATEADD(month, -1, GETDATE())"
        ↓
Step 3: Database executes SQL and returns rows
        ↓
Step 4: Format as JSON with pagination
        ↓
Output: {"items": [...], "total_items": 25, "page_number": 1}

PYTHON CONCEPTS USED:
═══════════════════════
🔹 FastAPI        → Web framework (like Express.js for Node.js)
🔹 Async/Await    → Non-blocking code (can handle multiple users)
🔹 Pydantic       → Data validation (ensures correct input/output)
🔹 SQLAlchemy     → Database ORM (Object-Relational Mapping)
🔹 Type Hints     → def function(name: str) -> int: (helps with bugs)
🔹 Dependency Injection → get_service() provides objects automatically
🔹 Environment Variables → .env file stores secrets safely
🔹 Error Handling → try/except blocks catch and handle errors

FILE STRUCTURE EXPLAINED:
═══════════════════════════
📁 app/
├── main.py              🌐 Creates FastAPI app, sets up CORS
├── controllers/         🎮 Handle HTTP requests (like "routes" in other frameworks)
├── services/           🧠 Business logic (the actual work)
├── models/dto/         📝 Data Transfer Objects (request/response formats)
├── config/             ⚙️ Settings and database configuration
└── utils/              🛠️ Helper functions and custom exceptions

COMPARISON TO OTHER FRAMEWORKS:
═══════════════════════════════
If you know:           Python equivalent:
• Express.js routes → FastAPI @router.post()
• Spring Controllers → FastAPI controllers
• Entity Framework → SQLAlchemy
• appsettings.json → .env file
• Dependency Injection → FastAPI Depends()
• Async/await → Same in Python!
"""

def print_architecture():
    """Print the architecture diagram"""
    print(ARCHITECTURE_DIAGRAM)

if __name__ == "__main__":
    print_architecture()
