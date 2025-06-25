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
