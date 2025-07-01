from fastapi import APIRouter, HTTPException, Depends, status
from typing import List
import logging
import json
from ..models.dto.request_dto import NaturalLanguageSqlQueryRequestDto, DebugContextRequestDto
from ..models.dto.response_dto import PaginatedResponse, EmailTriggerReportDto, DebugContextResponseDto, ErrorResponseDto
from ..services.natural_sql_rag.orchestrator import NaturalLanguageSqlQueryOrchestrator
from ..services.natural_sql_rag.rag_service import SemanticNaturalSqlRagService
from ..services.natural_sql_rag.mock_rag_service import MockNaturalSqlRagService
from ..services.natural_sql_rag.gemini_service import GeminiSqlGeneratorService
from ..services.natural_sql_rag.mock_gemini_test_service import MockGeminiTestService
from ..config.settings import settings
from ..utils.exceptions import ValidationException, AIServiceException, DatabaseException

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/natural-language-sql-query", tags=["Natural Language SQL Query"])

# Dependency injection - in production, use proper DI container
def get_rag_service():
    # 🐛 DEBUG: Service creation
    print("🔍 Creating RAG service...")
    try:
        # Use the real SemanticNaturalSqlRagService for debugging
        print("🔍 DEBUG: Using SemanticNaturalSqlRagService for full debugging...")
        service = SemanticNaturalSqlRagService()
        print(f"✅ RAG service created: {type(service).__name__}")
        return service
    except ImportError as e:
        print(f"⚠️ Falling back to mock RAG service due to import error: {e}")
        logger.warning(f"AI libraries not available, using mock RAG service: {e}")
        return MockNaturalSqlRagService()

def get_gemini_service():
    # 🐛 DEBUG: Gemini service creation
    print("🔍 Creating Gemini service...")
    if not settings.gemini_api_key:
        print("🔍 DEBUG: No Gemini API key found, using MockGeminiTestService for pattern matching...")
        service = MockGeminiTestService()
    else:
        print("🔍 DEBUG: Using real GeminiSqlGeneratorService with API key...")
        service = GeminiSqlGeneratorService()
    print(f"✅ Gemini service created: {type(service).__name__}")
    return service

def get_orchestrator(
    rag_service: SemanticNaturalSqlRagService = Depends(get_rag_service),
    gemini_service = Depends(get_gemini_service)
) -> NaturalLanguageSqlQueryOrchestrator:
    # 🐛 DEBUG: Orchestrator creation
    print("🔍 Creating orchestrator...")
    orchestrator = NaturalLanguageSqlQueryOrchestrator(rag_service, gemini_service)
    print(f"✅ Orchestrator created: {type(orchestrator).__name__}")
    return orchestrator

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
    # � BREAKPOINT: Set VS Code breakpoint on the next line!
    # �🐛 DEBUG: Request received
    print(f"🚀 API Request received: '{request.query}' (Page: {request.page_number}, Size: {request.page_size})")
    
    try:
        if not request.query or not request.query.strip():
            print("❌ Validation failed: empty query")
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Query cannot be empty"
            )
        
        logger.info(f"Processing natural language query: {request.query}")
        
        # 🔥 BREAKPOINT: Another good spot for a breakpoint!
        # 🐛 DEBUG: Before orchestrator call
        print(f"🤖 Calling orchestrator with query: '{request.query}'")
        
        result = await orchestrator.run_async(
            request.query, 
            request.page_number, 
            request.page_size
        )
        
        # 🔥 BREAKPOINT: Check the result here!
        # 🐛 DEBUG: After orchestrator call
        print(f"✅ Orchestrator completed successfully")
        print(f"📊 Result type: {type(result).__name__}")
        if hasattr(result, 'items'):
            print(f"📊 Items returned: {len(result.items)}")
        
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
    """    # 🐛 DEBUG: Debug context request received
    # 🔥 BREAKPOINT: Set VS Code breakpoint on the next line!
    print(f"🔍 DEBUG CONTEXT Request: '{request.query}' (Max items: {request.max_items})")
    
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
