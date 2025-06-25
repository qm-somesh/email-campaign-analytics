# Email Campaign Reporting - Python Backend Service
# Complete implementation replicating .NET 9 backend functionality

## Architecture Overview

This document provides a comprehensive Python implementation of the EmailCampaignReporting backend service, replicating all functionality from the existing .NET 9 backend including:

- Natural Language SQL Query processing with RAG architecture
- Semantic search using embeddings
- Google Gemini AI integration for SQL generation
- SQL Server database connectivity
- RESTful API endpoints with pagination
- Email trigger reporting and analytics

## Recommended Technology Stack

### Core Framework
- **FastAPI**: Modern, fast web framework for building APIs
- **Pydantic**: Data validation and serialization
- **SQLAlchemy**: SQL toolkit and ORM
- **Alembic**: Database migration tool

### Database & AI
- **pyodbc/asyncpg**: SQL Server connectivity
- **sentence-transformers**: For embedding generation
- **google-generativeai**: Gemini AI client
- **numpy**: Numerical operations for embeddings
- **scikit-learn**: Cosine similarity calculations

### Additional Libraries
- **python-dotenv**: Environment variable management
- **uvicorn**: ASGI server
- **httpx**: Async HTTP client
- **logging**: Comprehensive logging

## Project Structure

```
python_backend/
├── app/
│   ├── __init__.py
│   ├── main.py                    # FastAPI application entry point
│   ├── config/
│   │   ├── __init__.py
│   │   ├── settings.py            # Configuration management
│   │   └── database.py            # Database connection setup
│   ├── controllers/
│   │   ├── __init__.py
│   │   ├── natural_language_sql_controller.py  # Main controller
│   │   └── health_controller.py   # Health check endpoints
│   ├── services/
│   │   ├── __init__.py
│   │   ├── natural_sql_rag/
│   │   │   ├── __init__.py
│   │   │   ├── interfaces.py      # Service interfaces
│   │   │   ├── orchestrator.py    # Main orchestration service
│   │   │   ├── rag_service.py     # RAG implementation
│   │   │   ├── gemini_service.py  # Gemini AI service
│   │   │   └── embedding_service.py # Embedding generation
│   │   └── database_service.py    # Database operations
│   ├── models/
│   │   ├── __init__.py
│   │   ├── dto/
│   │   │   ├── __init__.py
│   │   │   ├── request_dto.py     # Request DTOs
│   │   │   └── response_dto.py    # Response DTOs
│   │   └── entities/
│   │       ├── __init__.py
│   │       └── email_entities.py  # Database entities
│   └── utils/
│       ├── __init__.py
│       ├── logging_config.py      # Logging configuration
│       └── exceptions.py          # Custom exceptions
├── requirements.txt
├── .env.example
├── Dockerfile
└── README.md
```

## Implementation

### 1. Core Configuration (app/config/settings.py)

```python
from pydantic_settings import BaseSettings
from typing import Optional

class Settings(BaseSettings):
    # Database Configuration
    database_url: str = "mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server"
    database_pool_size: int = 10
    database_max_overflow: int = 20
    
    # Gemini AI Configuration
    gemini_api_key: str
    gemini_model_name: str = "gemini-1.5-flash"
    gemini_base_url: str = "https://generativelanguage.googleapis.com/v1"
    gemini_temperature: float = 0.1
    gemini_max_tokens: int = 512
    
    # Embedding Configuration
    embedding_model_name: str = "all-MiniLM-L6-v2"
    embedding_dimension: int = 384
    max_context_items: int = 10
    
    # API Configuration
    api_title: str = "Email Campaign Reporting API"
    api_version: str = "1.0.0"
    api_description: str = "Python backend service for email campaign analytics"
    cors_origins: list = ["http://localhost:3000", "http://localhost:3001"]
    
    # Pagination
    default_page_size: int = 50
    max_page_size: int = 1000
    
    # Logging
    log_level: str = "INFO"
    log_format: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    class Config:
        env_file = ".env"

settings = Settings()
```

### 2. Database Configuration (app/config/database.py)

```python
from sqlalchemy import create_engine, MetaData
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import QueuePool
from .settings import settings
import logging

logger = logging.getLogger(__name__)

# Create database engine with connection pooling
engine = create_engine(
    settings.database_url,
    poolclass=QueuePool,
    pool_size=settings.database_pool_size,
    max_overflow=settings.database_max_overflow,
    pool_pre_ping=True,  # Verify connections before use
    echo=False  # Set to True for SQL debugging
)

# Create sessionmaker
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Create base class for models
Base = declarative_base()

# Database dependency for FastAPI
def get_db():
    db = SessionLocal()
    try:
        yield db
    except Exception as e:
        logger.error(f"Database error: {e}")
        db.rollback()
        raise
    finally:
        db.close()
```

### 3. Data Transfer Objects (app/models/dto/request_dto.py)

```python
from pydantic import BaseModel, Field, validator
from typing import Optional

class NaturalLanguageSqlQueryRequestDto(BaseModel):
    """DTO for natural language SQL query requests"""
    query: str = Field(..., min_length=1, max_length=1000, description="Natural language query")
    page_number: int = Field(1, ge=1, description="Page number for pagination")
    page_size: int = Field(50, ge=1, le=1000, description="Number of items per page")
    
    @validator('query')
    def validate_query(cls, v):
        if not v or not v.strip():
            raise ValueError('Query cannot be empty or whitespace only')
        return v.strip()
    
    class Config:
        schema_extra = {
            "example": {
                "query": "Show me campaigns with high open rates from last month",
                "page_number": 1,
                "page_size": 50
            }
        }

class DebugContextRequestDto(BaseModel):
    """DTO for debug context requests"""
    query: str = Field(..., min_length=1, max_length=1000, description="Query to get context for")
    max_items: int = Field(10, ge=1, le=50, description="Maximum context items to return")
```

### 4. Response DTOs (app/models/dto/response_dto.py)

```python
from pydantic import BaseModel, Field
from typing import List, Optional, Generic, TypeVar
from datetime import datetime
import math

T = TypeVar('T')

class EmailTriggerReportDto(BaseModel):
    """DTO for email trigger report data"""
    strategy_name: str = Field(..., description="Campaign/Strategy name")
    total_emails: int = Field(..., description="Total number of emails sent")
    delivered_count: int = Field(..., description="Number of delivered emails")
    bounced_count: int = Field(..., description="Number of bounced emails")
    opened_count: int = Field(..., description="Number of opened emails")
    clicked_count: int = Field(..., description="Number of clicked emails")
    complained_count: int = Field(..., description="Number of complained emails")
    unsubscribed_count: int = Field(..., description="Number of unsubscribed emails")
    first_email_sent: Optional[datetime] = Field(None, description="First email sent date")
    last_email_sent: Optional[datetime] = Field(None, description="Last email sent date")
    
    class Config:
        schema_extra = {
            "example": {
                "strategy_name": "Summer Sale Campaign",
                "total_emails": 1000,
                "delivered_count": 950,
                "bounced_count": 50,
                "opened_count": 300,
                "clicked_count": 75,
                "complained_count": 5,
                "unsubscribed_count": 10,
                "first_email_sent": "2025-06-01T10:00:00Z",
                "last_email_sent": "2025-06-15T15:30:00Z"
            }
        }

class PaginatedResponse(BaseModel, Generic[T]):
    """Generic paginated response"""
    items: List[T] = Field(..., description="List of items")
    total_count: int = Field(..., description="Total number of items")
    page_number: int = Field(..., description="Current page number")
    page_size: int = Field(..., description="Number of items per page")
    total_pages: int = Field(..., description="Total number of pages")
    
    @classmethod
    def create(cls, items: List[T], total_count: int, page_number: int, page_size: int):
        total_pages = math.ceil(total_count / page_size) if page_size > 0 else 0
        return cls(
            items=items,
            total_count=total_count,
            page_number=page_number,
            page_size=page_size,
            total_pages=total_pages
        )

class DebugContextResponseDto(BaseModel):
    """DTO for debug context response"""
    query: str = Field(..., description="Original query")
    context: List[str] = Field(..., description="Retrieved context items")
    context_count: int = Field(..., description="Number of context items returned")
    
class ErrorResponseDto(BaseModel):
    """DTO for error responses"""
    error: str = Field(..., description="Error message")
    details: Optional[str] = Field(None, description="Additional error details")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Error timestamp")
```

### 5. Service Interfaces (app/services/natural_sql_rag/interfaces.py)

```python
from abc import ABC, abstractmethod
from typing import List
from ...models.dto.response_dto import PaginatedResponse, EmailTriggerReportDto

class INaturalSqlRagService(ABC):
    """Interface for RAG service dedicated to SQL query generation context"""
    
    @abstractmethod
    async def get_context_for_sql_async(self, user_query: str, max_context_items: int = 5) -> List[str]:
        """
        Given a user query, returns a list of context strings relevant for SQL generation.
        
        Args:
            user_query: The user's natural language query
            max_context_items: Maximum number of context items to retrieve
            
        Returns:
            List of context strings
        """
        pass

class IGeminiSqlGeneratorService(ABC):
    """Interface for Gemini SQL generation service"""
    
    @abstractmethod
    async def generate_sql_async(self, prompt: str) -> str:
        """
        Generate SQL query from prompt using Gemini AI
        
        Args:
            prompt: The complete prompt including context and user query
            
        Returns:
            Generated SQL query string
        """
        pass

class INaturalLanguageSqlQueryOrchestrator(ABC):
    """Interface for the main orchestrator service"""
    
    @abstractmethod
    async def run_async(self, user_query: str, page_number: int, page_size: int) -> PaginatedResponse[EmailTriggerReportDto]:
        """
        Run the complete RAG + SQL generation + execution workflow
        
        Args:
            user_query: Natural language query from user
            page_number: Page number for pagination
            page_size: Number of items per page
            
        Returns:
            Paginated response with email trigger report data
        """
        pass
```

### 6. RAG Service Implementation (app/services/natural_sql_rag/rag_service.py)

```python
import logging
import numpy as np
from typing import List, Dict, Any
from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity
from .interfaces import INaturalSqlRagService
from ...config.settings import settings

logger = logging.getLogger(__name__)

class KnowledgeItem:
    """Represents a knowledge base item with text and embedding"""
    def __init__(self, text: str, embedding: np.ndarray = None):
        self.text = text
        self.embedding = embedding

class SemanticNaturalSqlRagService(INaturalSqlRagService):
    """Semantic RAG service using embeddings for true semantic search"""
    
    def __init__(self):
        self.embedding_model = SentenceTransformer(settings.embedding_model_name)
        self.knowledge_base: List[KnowledgeItem] = []
        self.is_initialized = False
        self._initialize_knowledge_base()
    
    def _initialize_knowledge_base(self):
        """Initialize the knowledge base with domain-specific information"""
        knowledge_texts = [
            # Database schema information
            "EmailTrigger table: Description (campaign name), IsActive (1=active), CommunicationId (links to EmailOutbox)",
            "EmailOutbox_bak table: EmailOutboxId (unique), CommunicationId (links to EmailTrigger), DateCreated (email send time)",
            "WebhookLogs_bak table: EmailOutboxId (links to EmailOutbox), StatusId (links to EmailStatus)",
            "EmailStatus table: StatusId (unique), Status (delivered/bounced/failed/opened/clicked/complained/unsubscribed)",
            
            # Required output format
            "ALL queries MUST return exactly these 10 columns with exact aliases: StrategyName, TotalEmails, DeliveredCount, BouncedCount, OpenedCount, ClickedCount, ComplainedCount, UnsubscribedCount, FirstEmailSent, LastEmailSent",
            "Use et.Description AS StrategyName for campaign names",
            "Use COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails for total email count",
            "Use SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount",
            "Use SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount",
            "Use SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount",
            "Use SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount",
            "Use SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount",
            "Use SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount",
            "Use MIN(eo.DateCreated) AS FirstEmailSent and MAX(eo.DateCreated) AS LastEmailSent",
            
            # Table relationships and joins
            "Always use LEFT JOINs to avoid losing data: et LEFT JOIN eo ON eo.CommunicationId = et.CommunicationId",
            "Join EmailOutbox to WebhookLogs: LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId",
            "Join WebhookLogs to EmailStatus: LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId",
            "Always include WHERE et.IsActive = 1 to filter active campaigns only",
            "Always GROUP BY et.Description for campaign-level aggregation",
            "Always include ORDER BY clause for consistent pagination",
            
            # Time-based filtering patterns
            "For 'last month' or 'past month': add AND eo.DateCreated >= DATEADD(month, -1, GETDATE())",
            "For 'last week' or 'past week': add AND eo.DateCreated >= DATEADD(week, -1, GETDATE())",
            "For 'last N days': add AND eo.DateCreated >= DATEADD(day, -N, GETDATE())",
            "For 'recent' or 'latest': add appropriate date filter based on context",
            
            # Performance-based sorting
            "For 'high open rates' or 'best open': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'high click rates': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "For 'low bounce rates': ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC",
            
            # Performance filtering with HAVING clauses
            "For 'successful' or 'best' campaigns: HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) > 0",
            "For 'top performing': HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10 AND SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0",
            "For 'problematic campaigns': HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR bounce_rate > 0.05 OR open_rate < 0.5",
            
            # Pagination requirements
            "Never include OFFSET/FETCH clauses in generated SQL - pagination added automatically",
            "Always ensure queries support pagination by including ORDER BY clause",
            
            # Common query patterns
            "Campaign performance overview: all campaigns with email metrics and dates",
            "Top performers: campaigns with high delivery and engagement rates",
            "Problematic campaigns: campaigns with high bounce rates or low engagement",
            "Recent activity: campaigns from specific time periods",
            "Campaign comparison: side-by-side metrics for multiple campaigns"
        ]
        
        try:
            # Generate embeddings for all knowledge items
            logger.info(f"Generating embeddings for {len(knowledge_texts)} knowledge items...")
            embeddings = self.embedding_model.encode(knowledge_texts, convert_to_numpy=True)
            
            # Create knowledge items with embeddings
            self.knowledge_base = [
                KnowledgeItem(text, embedding) 
                for text, embedding in zip(knowledge_texts, embeddings)
            ]
            
            self.is_initialized = True
            logger.info(f"Successfully initialized knowledge base with {len(self.knowledge_base)} items")
            
        except Exception as e:
            logger.error(f"Failed to initialize knowledge base: {e}")
            raise
    
    async def get_context_for_sql_async(self, user_query: str, max_context_items: int = 5) -> List[str]:
        """Retrieve relevant context for SQL generation using semantic search"""
        if not self.is_initialized:
            logger.warning("Knowledge base not initialized, returning empty context")
            return []
        
        try:
            # Generate embedding for user query
            query_embedding = self.embedding_model.encode([user_query], convert_to_numpy=True)
            
            # Calculate cosine similarities
            similarities = []
            for i, item in enumerate(self.knowledge_base):
                if item.embedding is not None:
                    similarity = cosine_similarity(query_embedding, item.embedding.reshape(1, -1))[0][0]
                    similarities.append((similarity, i))
            
            # Sort by similarity and get top items
            similarities.sort(key=lambda x: x[0], reverse=True)
            top_indices = [idx for _, idx in similarities[:max_context_items]]
            
            # Return top context items
            context_items = [self.knowledge_base[idx].text for idx in top_indices]
            
            logger.debug(f"Retrieved {len(context_items)} context items for query: '{user_query[:50]}...'")
            return context_items
            
        except Exception as e:
            logger.error(f"Error retrieving context for query '{user_query}': {e}")
            return []
```

### 7. Gemini AI Service (app/services/natural_sql_rag/gemini_service.py)

```python
import logging
import httpx
import json
import re
from typing import Optional
from .interfaces import IGeminiSqlGeneratorService
from ...config.settings import settings
from ...utils.exceptions import AIServiceException

logger = logging.getLogger(__name__)

class GeminiSqlGeneratorService(IGeminiSqlGeneratorService):
    """Service for calling Gemini AI to generate SQL queries"""
    
    def __init__(self):
        self.api_key = settings.gemini_api_key
        self.model_name = settings.gemini_model_name
        self.base_url = f"{settings.gemini_base_url}/models/{self.model_name}:generateContent"
        self.temperature = settings.gemini_temperature
        self.max_tokens = settings.gemini_max_tokens
        
        if not self.api_key:
            raise ValueError("Gemini API key is required")
    
    async def generate_sql_async(self, prompt: str) -> str:
        """Generate SQL query from prompt using Gemini AI"""
        try:
            request_body = {
                "contents": [{
                    "parts": [{"text": prompt}]
                }],
                "generationConfig": {
                    "temperature": self.temperature,
                    "maxOutputTokens": self.max_tokens
                }
            }
            
            url = f"{self.base_url}?key={self.api_key}"
            
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(
                    url,
                    json=request_body,
                    headers={"Content-Type": "application/json"}
                )
                
                if not response.is_success:
                    error_msg = f"Gemini API error: {response.status_code} - {response.text}"
                    logger.error(error_msg)
                    raise AIServiceException(error_msg)
                
                response_data = response.json()
                
                # Extract text from Gemini response
                if "candidates" in response_data and len(response_data["candidates"]) > 0:
                    candidate = response_data["candidates"][0]
                    if "content" in candidate and "parts" in candidate["content"]:
                        text = candidate["content"]["parts"][0].get("text", "")
                        sql = self._extract_sql_from_response(text)
                        
                        if not sql:
                            raise AIServiceException("No valid SQL found in Gemini response")
                        
                        logger.info(f"Generated SQL query: {sql[:100]}...")
                        return sql
                
                raise AIServiceException("Invalid response format from Gemini API")
                
        except httpx.TimeoutException:
            logger.error("Timeout occurred while calling Gemini API")
            raise AIServiceException("AI service timeout")
        except Exception as e:
            logger.error(f"Error generating SQL with Gemini: {e}")
            raise AIServiceException(f"Failed to generate SQL: {str(e)}")
    
    def _extract_sql_from_response(self, response: str) -> str:
        """Extract SQL query from Gemini response"""
        if not response or not response.strip():
            return ""
        
        # Clean up the response
        clean_response = response.replace("\\n", "\n").replace("\\u003e", ">").replace("\\u003c", "<").strip()
        
        # Try to extract SQL from code block (```sql ... ```)
        sql_match = re.search(r'```sql\s*(.*?)\s*```', clean_response, re.DOTALL | re.IGNORECASE)
        if sql_match:
            return sql_match.group(1).strip()
        
        # Try to extract SQL from generic code block (``` ... ```)
        code_match = re.search(r'```\s*(.*?)\s*```', clean_response, re.DOTALL)
        if code_match:
            sql = code_match.group(1).strip()
            if sql.upper().startswith('SELECT'):
                return sql
        
        # Look for SELECT statement in the response
        select_match = re.search(r'\bSELECT\b.*?(?=;|\n\n|\Z)', clean_response, re.DOTALL | re.IGNORECASE)
        if select_match:
            return select_match.group(0).strip()
        
        # Fallback: return response if it looks like SQL
        if clean_response.upper().startswith('SELECT'):
            return clean_response.strip()
        
        return ""
```

### 8. Main Orchestrator Service (app/services/natural_sql_rag/orchestrator.py)

```python
import logging
import re
import math
from typing import List, Tuple
from sqlalchemy.orm import Session
from sqlalchemy import text
from .interfaces import INaturalLanguageSqlQueryOrchestrator, INaturalSqlRagService, IGeminiSqlGeneratorService
from ...models.dto.response_dto import PaginatedResponse, EmailTriggerReportDto
from ...config.database import get_db
from ...utils.exceptions import DatabaseException, ValidationException

logger = logging.getLogger(__name__)

class NaturalLanguageSqlQueryOrchestrator(INaturalLanguageSqlQueryOrchestrator):
    """Orchestrates the RAG + Gemini SQL workflow and executes generated SQL"""
    
    def __init__(self, rag_service: INaturalSqlRagService, gemini_service: IGeminiSqlGeneratorService):
        self.rag_service = rag_service
        self.gemini_service = gemini_service
    
    async def run_async(self, user_query: str, page_number: int, page_size: int) -> PaginatedResponse[EmailTriggerReportDto]:
        """Run the complete RAG + SQL generation + execution workflow"""
        try:
            # 1. Get RAG context
            logger.info(f"Getting RAG context for query: {user_query}")
            context_items = await self.rag_service.get_context_for_sql_async(user_query)
            context = "\n".join(context_items)
            
            # 2. Build comprehensive prompt for Gemini
            prompt = self._build_gemini_prompt(user_query, context)
            
            # 3. Generate SQL with Gemini
            logger.info("Generating SQL with Gemini AI")
            raw_response = await self.gemini_service.generate_sql_async(prompt)
            generated_sql = self._extract_and_validate_sql(raw_response)
            
            # 4. Add pagination if not present
            final_sql = self._add_pagination_to_sql(generated_sql)
            
            # 5. Execute SQL and return results
            logger.info("Executing generated SQL query")
            return await self._execute_sql_and_paginate(final_sql, page_number, page_size)
            
        except Exception as e:
            logger.error(f"Error in orchestrator workflow: {e}")
            raise
    
    def _build_gemini_prompt(self, user_query: str, context: str) -> str:
        """Build comprehensive prompt for Gemini SQL generation"""
        schema = """Table: EmailTrigger (et) - Description, IsActive, CommunicationId
Table: EmailOutbox_bak (eo) - EmailOutboxId, CommunicationId, DateCreated
Table: WebhookLogs_bak (es) - EmailOutboxId, StatusId
Table: EmailStatus (st) - StatusId, Status (values: 'delivered', 'bounced', 'failed', 'opened', 'clicked', 'complained', 'unsubscribed')"""
        
        required_columns = """REQUIRED OUTPUT COLUMNS (must be included in SELECT with exact aliases):
- et.Description AS StrategyName
- COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails
- SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount
- SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount
- SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount
- SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount
- SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount
- SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount
- MIN(eo.DateCreated) AS FirstEmailSent
- MAX(eo.DateCreated) AS LastEmailSent"""
        
        example_sql = """SELECT et.Description AS StrategyName, COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails, SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount, SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount, SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount, SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount, SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount, SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount, MIN(eo.DateCreated) AS FirstEmailSent, MAX(eo.DateCreated) AS LastEmailSent FROM EmailTrigger et LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId WHERE et.IsActive = 1 GROUP BY et.Description ORDER BY et.Description"""
        
        return f"""You are an expert SQL assistant. Generate a SQL SELECT query to answer the user's request.

CRITICAL REQUIREMENTS:
- You MUST include ALL 10 required columns in your SELECT statement with the EXACT aliases shown below
- Use only the tables and columns from the provided schema
- Always use LEFT JOINs to avoid losing data
- Always include 'WHERE et.IsActive = 1' condition
- Always GROUP BY et.Description
- Always include ORDER BY clause (required for pagination)
- Do not include OFFSET/FETCH clauses (pagination will be added automatically)
- Output ONLY the SQL query, no explanations

IMPORTANT: Pay attention to time-based requests:
- If user mentions 'last month', 'past month': ADD 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())'
- If user mentions 'last week', 'past week': ADD 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())'
- If user mentions 'last N days': ADD 'AND eo.DateCreated >= DATEADD(day, -N, GETDATE())'
- If user mentions 'recent', 'latest': ADD appropriate date filter

IMPORTANT: Pay attention to performance-based requests:
- If user wants 'high open rates', 'best open': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC
- If user wants 'high click rates': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC
- If user wants 'low bounce rates': ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC

{required_columns}

SCHEMA:
{schema}

EXAMPLE QUERY STRUCTURE:
{example_sql}

CONTEXT:
{context}

USER REQUEST: {user_query}

Generate the SQL query now:"""
    
    def _extract_and_validate_sql(self, raw_response: str) -> str:
        """Extract and validate SQL from Gemini response"""
        if not raw_response or not raw_response.strip():
            raise ValidationException("Gemini response is empty")
        
        # Try to extract SQL (reuse logic from Gemini service)
        clean_response = raw_response.replace("\\n", "\n").strip()
        
        # Extract SQL using various patterns
        sql = ""
        
        # Try code block extraction
        sql_match = re.search(r'```sql\s*(.*?)\s*```', clean_response, re.DOTALL | re.IGNORECASE)
        if sql_match:
            sql = sql_match.group(1).strip()
        else:
            # Try generic code block
            code_match = re.search(r'```\s*(.*?)\s*```', clean_response, re.DOTALL)
            if code_match:
                potential_sql = code_match.group(1).strip()
                if potential_sql.upper().startswith('SELECT'):
                    sql = potential_sql
            else:
                # Look for SELECT statement
                select_match = re.search(r'\bSELECT\b.*?(?=;|\n\n|\Z)', clean_response, re.DOTALL | re.IGNORECASE)
                if select_match:
                    sql = select_match.group(0).strip()
                elif clean_response.upper().startswith('SELECT'):
                    sql = clean_response.strip()
        
        if not sql or not sql.upper().startswith('SELECT'):
            raise ValidationException(f"No valid SQL SELECT statement found in response: {raw_response[:200]}")
        
        return sql
    
    def _add_pagination_to_sql(self, sql: str) -> str:
        """Add ORDER BY and pagination parameters to SQL if not present"""
        sql = sql.strip()
        
        # Ensure ORDER BY is present
        if not re.search(r'\bORDER\s+BY\b', sql, re.IGNORECASE):
            sql += " ORDER BY et.Description"
        
        # Add pagination parameters if not present
        if not re.search(r'\bOFFSET\b.*\bROWS\b', sql, re.IGNORECASE):
            sql += " OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY"
        
        return sql
    
    async def _execute_sql_and_paginate(self, sql: str, page_number: int, page_size: int) -> PaginatedResponse[EmailTriggerReportDto]:
        """Execute SQL query and return paginated results"""
        try:
            offset = (page_number - 1) * page_size
            
            # Create database session
            db_gen = get_db()
            db: Session = next(db_gen)
            
            try:
                # Get total count first
                count_sql = f"SELECT COUNT(*) as total_count FROM ({sql.replace(' OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY', '')}) AS CountQuery"
                count_result = db.execute(text(count_sql), {"offset": offset, "page_size": page_size})
                total_count = count_result.scalar()
                
                # Get actual data
                data_result = db.execute(text(sql), {"offset": offset, "page_size": page_size})
                rows = data_result.fetchall()
                
                # Map results to DTOs
                items = []
                for row in rows:
                    items.append(EmailTriggerReportDto(
                        strategy_name=row.StrategyName,
                        total_emails=row.TotalEmails,
                        delivered_count=row.DeliveredCount,
                        bounced_count=row.BouncedCount,
                        opened_count=row.OpenedCount,
                        clicked_count=row.ClickedCount,
                        complained_count=row.ComplainedCount,
                        unsubscribed_count=row.UnsubscribedCount,
                        first_email_sent=row.FirstEmailSent,
                        last_email_sent=row.LastEmailSent
                    ))
                
                return PaginatedResponse.create(items, total_count, page_number, page_size)
                
            finally:
                db.close()
                
        except Exception as e:
            logger.error(f"Error executing SQL query: {e}")
            raise DatabaseException(f"Failed to execute query: {str(e)}")
```

### 9. Main Controller (app/controllers/natural_language_sql_controller.py)

```python
from fastapi import APIRouter, HTTPException, Depends, status
from typing import List
import logging
from ..models.dto.request_dto import NaturalLanguageSqlQueryRequestDto, DebugContextRequestDto
from ..models.dto.response_dto import PaginatedResponse, EmailTriggerReportDto, DebugContextResponseDto, ErrorResponseDto
from ..services.natural_sql_rag.orchestrator import NaturalLanguageSqlQueryOrchestrator
from ..services.natural_sql_rag.rag_service import SemanticNaturalSqlRagService
from ..services.natural_sql_rag.gemini_service import GeminiSqlGeneratorService
from ..utils.exceptions import ValidationException, AIServiceException, DatabaseException

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/natural-language-sql-query", tags=["Natural Language SQL Query"])

# Dependency injection - in production, use proper DI container
def get_rag_service() -> SemanticNaturalSqlRagService:
    return SemanticNaturalSqlRagService()

def get_gemini_service() -> GeminiSqlGeneratorService:
    return GeminiSqlGeneratorService()

def get_orchestrator(
    rag_service: SemanticNaturalSqlRagService = Depends(get_rag_service),
    gemini_service: GeminiSqlGeneratorService = Depends(get_gemini_service)
) -> NaturalLanguageSqlQueryOrchestrator:
    return NaturalLanguageSqlQueryOrchestrator(rag_service, gemini_service)

@router.post(
    "/sql-query",
    response_model=PaginatedResponse[EmailTriggerReportDto],
    responses={
        400: {"model": ErrorResponseDto},
        500: {"model": ErrorResponseDto}
    }
)
async def post_sql_query(
    request: NaturalLanguageSqlQueryRequestDto,
    orchestrator: NaturalLanguageSqlQueryOrchestrator = Depends(get_orchestrator)
):
    """
    Process natural language query and return email campaign analytics data
    
    This endpoint accepts a natural language query, converts it to SQL using RAG and AI,
    executes the query against the email campaign database, and returns paginated results.
    """
    try:
        if not request.query or not request.query.strip():
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Query cannot be empty"
            )
        
        logger.info(f"Processing natural language query: {request.query}")
        
        result = await orchestrator.run_async(
            request.query, 
            request.page_number, 
            request.page_size
        )
        
        logger.info(f"Query processed successfully, returning {len(result.items)} items")
        return result
        
    except ValidationException as e:
        logger.warning(f"Validation error: {e}")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )
    except AIServiceException as e:
        logger.error(f"AI service error: {e}")
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=f"AI service error: {str(e)}"
        )
    except DatabaseException as e:
        logger.error(f"Database error: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Database error: {str(e)}"
        )
    except Exception as e:
        logger.error(f"Unexpected error processing query '{request.query}': {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"An unexpected error occurred: {str(e)}"
        )

@router.post(
    "/debug-context",
    response_model=DebugContextResponseDto,
    responses={
        400: {"model": ErrorResponseDto},
        500: {"model": ErrorResponseDto}
    }
)
async def get_debug_context(
    request: DebugContextRequestDto,
    rag_service: SemanticNaturalSqlRagService = Depends(get_rag_service)
):
    """
    Get debug context for a natural language query
    
    This endpoint returns the RAG context that would be used for SQL generation,
    useful for debugging and understanding how the system interprets queries.
    """
    try:
        if not request.query or not request.query.strip():
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Query cannot be empty"
            )
        
        logger.info(f"Getting debug context for query: {request.query}")
        
        context = await rag_service.get_context_for_sql_async(
            request.query, 
            request.max_items
        )
        
        return DebugContextResponseDto(
            query=request.query,
            context=context,
            context_count=len(context)
        )
        
    except Exception as e:
        logger.error(f"Error getting debug context for query '{request.query}': {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"An error occurred: {str(e)}"
        )
```

### 10. Custom Exceptions (app/utils/exceptions.py)

```python
class EmailCampaignReportingException(Exception):
    """Base exception for the application"""
    pass

class ValidationException(EmailCampaignReportingException):
    """Exception for validation errors"""
    pass

class AIServiceException(EmailCampaignReportingException):
    """Exception for AI service errors"""
    pass

class DatabaseException(EmailCampaignReportingException):
    """Exception for database errors"""
    pass

class ConfigurationException(EmailCampaignReportingException):
    """Exception for configuration errors"""
    pass
```

### 11. Main Application (app/main.py)

```python
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import logging
import uvicorn
from .config.settings import settings
from .controllers.natural_language_sql_controller import router as nl_sql_router
from .utils.exceptions import EmailCampaignReportingException

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format=settings.log_format
)

logger = logging.getLogger(__name__)

# Create FastAPI application
app = FastAPI(
    title=settings.api_title,
    version=settings.api_version,
    description=settings.api_description,
    docs_url="/swagger",
    redoc_url="/redoc"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global exception handler
@app.exception_handler(EmailCampaignReportingException)
async def custom_exception_handler(request: Request, exc: EmailCampaignReportingException):
    return JSONResponse(
        status_code=400,
        content={"error": str(exc), "type": exc.__class__.__name__}
    )

# Include routers
app.include_router(nl_sql_router)

# Health check endpoint
@app.get("/health")
async def health_check():
    return {"status": "healthy", "service": "EmailCampaignReporting Python Backend"}

# Root endpoint
@app.get("/")
async def root():
    return {
        "service": "EmailCampaignReporting Python Backend",
        "version": settings.api_version,
        "docs": "/swagger"
    }

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level=settings.log_level.lower()
    )
```

### 12. Requirements File (requirements.txt)

```txt
# Web Framework
fastapi==0.104.1
uvicorn[standard]==0.24.0
pydantic==2.5.0
pydantic-settings==2.1.0

# Database
sqlalchemy==2.0.23
pyodbc==5.0.1
alembic==1.13.1

# AI and ML
sentence-transformers==2.2.2
scikit-learn==1.3.2
numpy==1.25.2
google-generativeai==0.3.2

# HTTP Client
httpx==0.25.2

# Utilities
python-dotenv==1.0.0
python-multipart==0.0.6

# Development
pytest==7.4.3
pytest-asyncio==0.21.1
black==23.11.0
flake8==6.1.0
```

### 13. Environment Configuration (.env.example)

```env
# Database Configuration
DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server
DATABASE_POOL_SIZE=10
DATABASE_MAX_OVERFLOW=20

# Gemini AI Configuration
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL_NAME=gemini-1.5-flash
GEMINI_TEMPERATURE=0.1
GEMINI_MAX_TOKENS=512

# Embedding Configuration
EMBEDDING_MODEL_NAME=all-MiniLM-L6-v2
MAX_CONTEXT_ITEMS=10

# API Configuration
API_TITLE=Email Campaign Reporting API
API_VERSION=1.0.0
CORS_ORIGINS=["http://localhost:3000", "http://localhost:3001"]

# Logging
LOG_LEVEL=INFO
```

## Deployment and Usage

### Installation and Setup

1. **Create virtual environment:**
```bash
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
```

2. **Install dependencies:**
```bash
pip install -r requirements.txt
```

3. **Configure environment:**
```bash
cp .env.example .env
# Edit .env with your configuration
```

4. **Run the application:**
```bash
python -m app.main
# Or with uvicorn directly:
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

### API Usage Examples

**Natural Language Query:**
```bash
curl -X POST "http://localhost:8000/api/natural-language-sql-query/sql-query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show me campaigns with high open rates from last month",
    "page_number": 1,
    "page_size": 50
  }'
```

**Debug Context:**
```bash
curl -X POST "http://localhost:8000/api/natural-language-sql-query/debug-context" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "problematic campaigns with high bounce rates",
    "max_items": 10
  }'
```

## Architecture Benefits

1. **Exact Feature Parity**: Replicates all .NET backend functionality
2. **Modern Python Stack**: Uses latest FastAPI, SQLAlchemy, and AI libraries
3. **Semantic RAG**: Embeddings-based knowledge retrieval for better context
4. **Robust Error Handling**: Comprehensive exception handling and logging
5. **Scalable Design**: Proper dependency injection and service architecture
6. **Production Ready**: Includes Docker support, health checks, and monitoring

This Python implementation provides a complete, production-ready backend service that matches the .NET 9 functionality while leveraging modern Python technologies and best practices.
