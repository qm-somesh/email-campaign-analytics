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
            
            # Debug logging for SQL generation
            logger.info(f"🔍 DEBUG: Full generated SQL: {generated_sql}")
            print(f"🔍 DEBUG: Full generated SQL: {generated_sql}")
            
            # 4. Add pagination if not present
            final_sql = self._add_pagination_to_sql(generated_sql)
            logger.info(f"🔍 DEBUG: Final SQL with pagination: {final_sql}")
            print(f"🔍 DEBUG: Final SQL with pagination: {final_sql}")
            
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
- If user mentions 'recent', 'latest': ADD 'AND eo.DateCreated >= DATEADD(day, -30, GETDATE())'
- If user mentions 'this week': ADD 'AND eo.DateCreated >= DATEADD(week, 0, GETDATE())'
- If user mentions 'this month': ADD 'AND eo.DateCreated >= DATEADD(month, 0, GETDATE())'
- If user mentions 'this year': ADD 'AND eo.DateCreated >= DATEADD(year, 0, GETDATE())'

IMPORTANT: Pay attention to performance-based requests:
- If user wants 'high open rates', 'best open rates', 'highest open rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0' AND ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC
- If user wants 'low open rates', 'lowest open rates', 'least open rates', 'worst open rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0' AND ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC
- If user wants 'high click rates', 'best click rates', 'highest click rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''clicked'' THEN 1 ELSE 0 END) > 0' AND ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC
- If user wants 'low click rates', 'lowest click rates', 'least click rates', 'worst click rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0' AND ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC
- If user wants 'low bounce rates', 'best delivery rates': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10' AND ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC
- If user wants 'click rates less than X%' or 'low click rates': ADD 'HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = ''clicked'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)), 0) < 0.05' (for 5%) AND ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC
- If user wants 'open rates less than X%' or 'low open rates': ADD 'HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)), 0) < 0.X' (replace X with user's percentage/100) AND ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC
- If user wants 'bounce rates greater than X%' or 'high bounce rates': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) > 0 AND (CAST(SUM(CASE WHEN st.Status IN (''bounced'', ''failed'') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.X'

IMPORTANT: Pay attention to filtering requirements:
- If user asks for 'successful', 'best', 'top', 'effective', or 'performing' campaigns: ADD HAVING clause to exclude poor performers
- If user asks for 'least', 'worst', 'lowest', 'bottom' performing campaigns: ORDER BY performance metrics ASC (ascending) to get worst first
- For 'most successful': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'
- For 'least successful' or 'worst performing': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0' AND ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC
- For 'best campaigns': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'
- For 'top performing': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10 AND SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'
- For 'highest click rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''clicked'' THEN 1 ELSE 0 END) > 0'
- For 'highest open rates': ADD 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'
- For 'best performing' or 'top campaigns': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 5 AND SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'
- For 'click rates less than 5%': ADD 'HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = ''clicked'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)), 0) < 0.05'
- For 'open rates less than X%': ADD 'HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)), 0) < 0.X' (replace X with percentage/100)
- For 'delivery rates less than X%': ADD 'HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)), 0) < 0.X'

IMPORTANT: Pay attention to problematic campaign requests:
- If user asks for 'problematic', 'issues', 'underperforming', 'poor', or 'need improvement' campaigns: ADD HAVING clause to identify poor performers
- For 'problematic campaigns': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status IN (''bounced'', ''failed'') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.05 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.5'
- For 'underperforming': ADD 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.6'
- For 'delivery issues': ADD 'HAVING (CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) < 0.95 OR COUNT(DISTINCT eo.EmailOutboxId) = 0'

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
                # Get total count first - remove ORDER BY and pagination for count query
                count_sql_base = sql.replace(' OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY', '')
                # Remove ORDER BY clause from count query as it's not allowed in subqueries in SQL Server
                # This regex matches ORDER BY and everything after it until the end of the string
                count_sql_base = re.sub(r'\s+ORDER\s+BY\s+.*$', '', count_sql_base, flags=re.IGNORECASE)
                count_sql = f"SELECT COUNT(*) as total_count FROM ({count_sql_base}) AS CountQuery"
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
