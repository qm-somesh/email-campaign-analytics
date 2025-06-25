# Email Campaign Reporting - Python Backend

A modern Python backend service for email campaign analytics with natural language SQL query processing, built with FastAPI and semantic search capabilities.

## Features

- **Natural Language SQL Processing**: Convert natural language queries to SQL using RAG and Gemini AI
- **Semantic Search**: Enhanced context retrieval using sentence transformers and embeddings
- **FastAPI**: Modern, fast web framework with automatic API documentation
- **Database Integration**: SQL Server connectivity with SQLAlchemy ORM
- **Production Ready**: Comprehensive error handling, logging, and monitoring

## Technology Stack

- **FastAPI**: Web framework and API
- **SQLAlchemy**: Database ORM and connectivity
- **Sentence Transformers**: Embedding generation for semantic search
- **Google Generative AI**: Gemini AI integration for SQL generation
- **Pydantic**: Data validation and serialization
- **Uvicorn**: ASGI server

## Quick Start

### Prerequisites

- Python 3.9 or higher
- SQL Server with ODBC Driver 17 (optional for basic testing)
- Gemini AI API key (optional for basic testing)

### Installation

#### Option 1: Using the Batch Script (Windows - Recommended)
```bash
# Double-click start.bat or run in command prompt:
start.bat
```

#### Option 2: Manual Setup
```bash
cd D:\Dev\EmailCampaignReporting\python_backend
python -m venv venv
venv\Scripts\activate  # Windows
pip install -r requirements.txt
python start.py
```

### Configuration

The application will create a `.env` file automatically on first run. Edit it with your settings:

```env
# Database (optional for basic testing)
DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server

# Gemini AI (optional for basic testing)
GEMINI_API_KEY=your_api_key_here
```

### Testing

The API will be available at:
- **API**: http://localhost:8000
- **Documentation**: http://localhost:8000/swagger
- **Health Check**: http://localhost:8000/health

Run the test script:
```bash
python test_api.py
```

## API Endpoints

### Natural Language SQL Query
**POST** `/api/natural-language-sql-query/sql-query`

Convert natural language to SQL and execute:

```json
{
    "query": "Show me campaigns with high open rates from last month",
    "page_number": 1,
    "page_size": 50
}
```

### Debug Context
**POST** `/api/natural-language-sql-query/debug-context`

Get RAG context for debugging:

```json
{
    "query": "problematic campaigns with high bounce rates",
    "max_items": 10
}
```

## Configuration

Edit `.env` file with your settings:

```env
# Database
DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server

# Gemini AI
GEMINI_API_KEY=your_api_key_here
GEMINI_MODEL_NAME=gemini-1.5-flash

# Embedding Model
EMBEDDING_MODEL_NAME=all-MiniLM-L6-v2
```

## Development

### Run in development mode:
```bash
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

### Run tests:
```bash
pytest
```

### Format code:
```bash
black app/
flake8 app/
```

## Architecture

```
app/
├── config/           # Configuration and database setup
├── controllers/      # API endpoints and request handling
├── services/         # Business logic and external service integration
│   └── natural_sql_rag/  # RAG, Gemini AI, and orchestration services
├── models/           # Data models and DTOs
│   └── dto/          # Request/response data transfer objects
└── utils/            # Utilities and custom exceptions
```

## Features

### Semantic RAG
- **Enhanced Knowledge Base**: Comprehensive patterns for email campaign queries
- **Embedding-based Search**: True semantic matching using sentence transformers
- **Context Optimization**: Specialized knowledge for problematic and high-performing campaigns

### Natural Language Processing
- **Time-based Queries**: "last month", "recent campaigns", "this week"
- **Performance Queries**: "high open rates", "best performing", "top campaigns"
- **Problematic Campaign Detection**: "underperforming", "issues", "need attention"

### Database Integration
- **Robust SQL Generation**: Proper joins, aggregations, and filtering
- **Pagination Support**: Built-in pagination with configurable page sizes
- **Error Handling**: Comprehensive database error management

## Production Deployment

The application is production-ready with:
- Environment-based configuration
- Comprehensive logging
- Health check endpoints
- CORS support
- Error handling and monitoring

For production deployment, consider:
- Using PostgreSQL or production SQL Server
- Implementing authentication and authorization
- Adding rate limiting and caching
- Setting up monitoring and alerting
- Containerizing with Docker

## License

This project is part of the EmailCampaignReporting system.
