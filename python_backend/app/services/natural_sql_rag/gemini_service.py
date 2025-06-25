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
            logger.warning("Gemini API key not provided - service will return mock responses")
    
    async def generate_sql_async(self, prompt: str) -> str:
        """Generate SQL query from prompt using Gemini AI"""
        
        # If no API key provided, return a basic mock SQL query for testing
        if not self.api_key:
            logger.warning("Using mock SQL response - no Gemini API key provided")
            return """SELECT et.Description AS StrategyName, COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails, 
SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount, 
SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount, 
SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount, 
SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount, 
SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount, 
SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount, 
MIN(eo.DateCreated) AS FirstEmailSent, MAX(eo.DateCreated) AS LastEmailSent 
FROM EmailTrigger et 
LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId 
LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId 
LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId 
WHERE et.IsActive = 1 
GROUP BY et.Description 
ORDER BY et.Description"""
        
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
