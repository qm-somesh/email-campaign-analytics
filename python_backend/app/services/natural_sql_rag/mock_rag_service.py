import logging
from typing import List
from .interfaces import INaturalSqlRagService

logger = logging.getLogger(__name__)

class MockNaturalSqlRagService(INaturalSqlRagService):
    """Mock RAG service that doesn't require heavy AI dependencies"""
    
    def __init__(self):
        self.knowledge_base = [
            # Core patterns for problematic campaigns
            "For 'problematic campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status IN (''bounced'', ''failed'') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.05'",
            "For 'underperforming campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.6'",
            "For 'campaigns with issues': Add HAVING clause to identify inactive or poor performing",
            "For 'poor performing campaigns': Use HAVING clause with high bounce rate or low open rate criteria",
            "For 'campaigns that need attention': Filter for inactive campaigns (0 emails) or poor engagement metrics",
            
            # Core requirements
            "ALL queries MUST return exactly these 10 columns with exact aliases: StrategyName, TotalEmails, DeliveredCount, BouncedCount, OpenedCount, ClickedCount, ComplainedCount, UnsubscribedCount, FirstEmailSent, LastEmailSent",
            "Always use LEFT JOINs: et LEFT JOIN eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN st ON es.StatusId = st.StatusId",
            "Always include WHERE et.IsActive = 1 to get only active campaigns",
            "Always GROUP BY et.Description for campaign-level aggregation",
            "Always include ORDER BY clause - required for pagination",
            
            # Time-based patterns
            "For 'last month' queries: Add 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())'",
            "For 'last week' queries: Add 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())'",
            "For 'last N days' queries: Add 'AND eo.DateCreated >= DATEADD(day, -N, GETDATE())'",
            
            # Performance sorting
            "For 'high open rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'best performing' queries: ORDER BY open rate descending",
            "For 'high click rates' queries: ORDER BY click rate descending"
        ]
        logger.info(f"Mock RAG service initialized with {len(self.knowledge_base)} knowledge items")
    
    async def get_context_for_sql_async(self, user_query: str, max_context_items: int = 5) -> List[str]:
        """Simple keyword-based context retrieval"""
        try:
            query_lower = user_query.lower()
            relevant_items = []
            
            # Simple keyword matching for problematic campaigns
            problematic_keywords = ['problematic', 'problem', 'issue', 'underperform', 'poor', 'bad', 'failing', 'struggle', 'attention', 'improve']
            if any(keyword in query_lower for keyword in problematic_keywords):
                relevant_items.extend([item for item in self.knowledge_base if 'problematic' in item.lower() or 'underperform' in item.lower() or 'poor' in item.lower()])
            
            # Time-based keywords
            time_keywords = ['last', 'recent', 'month', 'week', 'day', 'time']
            if any(keyword in query_lower for keyword in time_keywords):
                relevant_items.extend([item for item in self.knowledge_base if 'DATEADD' in item or 'time' in item.lower()])
            
            # Performance keywords
            performance_keywords = ['high', 'best', 'top', 'good', 'excellent', 'perform']
            if any(keyword in query_lower for keyword in performance_keywords):
                relevant_items.extend([item for item in self.knowledge_base if 'ORDER BY' in item or 'performing' in item.lower()])
            
            # Always include core requirements
            core_items = [item for item in self.knowledge_base if 'ALL queries MUST' in item or 'Always' in item]
            relevant_items.extend(core_items)
            
            # Remove duplicates and limit
            unique_items = list(dict.fromkeys(relevant_items))  # Remove duplicates while preserving order
            result = unique_items[:max_context_items]
            
            logger.info(f"Mock RAG: Found {len(result)} relevant items for query: '{user_query[:50]}...'")
            return result
            
        except Exception as e:
            logger.error(f"Error in mock RAG search for query '{user_query}': {e}")
            # Return basic context as fallback
            return self.knowledge_base[:max_context_items]
