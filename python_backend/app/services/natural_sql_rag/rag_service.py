import logging
import numpy as np
from typing import List, Dict, Any

try:
    from sentence_transformers import SentenceTransformer
    from sklearn.metrics.pairwise import cosine_similarity
    HAS_AI_LIBRARIES = True
except ImportError:
    HAS_AI_LIBRARIES = False
    # Create dummy classes to prevent import errors
    class SentenceTransformer:
        def __init__(self, *args, **kwargs):
            raise ImportError("sentence-transformers not installed")
    
    def cosine_similarity(*args, **kwargs):
        raise ImportError("scikit-learn not installed")

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
        # 🐛 WEB DEBUG: Service initialization
        print("🔍 DEBUG: SemanticNaturalSqlRagService.__init__() called")
        print(f"🔍 DEBUG: HAS_AI_LIBRARIES = {HAS_AI_LIBRARIES}")
        
        if not HAS_AI_LIBRARIES:
            print("❌ DEBUG: AI libraries not available, raising ImportError")
            raise ImportError("AI libraries (sentence-transformers, scikit-learn) not available. Use MockNaturalSqlRagService instead.")
        
        print("🔍 DEBUG: About to initialize SentenceTransformer model...")
        self.embedding_model = SentenceTransformer(settings.embedding_model_name)
        print(f"✅ DEBUG: SentenceTransformer initialized with model: {settings.embedding_model_name}")
        
        self.knowledge_base: List[KnowledgeItem] = []
        self.is_initialized = False
        
        print("🔍 DEBUG: About to initialize knowledge base...")
        self._initialize_knowledge_base()
        print(f"✅ DEBUG: Knowledge base initialized with {len(self.knowledge_base)} items")
    
    def _initialize_knowledge_base(self):
        """Initialize the knowledge base with domain-specific information"""
        knowledge_texts = [
            # Database schema information
            "EmailTrigger table: Description (campaign name), IsActive (1=active), CommunicationId (links to EmailOutbox)",
            "EmailOutbox_bak table: EmailOutboxId (unique), CommunicationId (links to EmailTrigger), DateCreated (email send time)",
            "WebhookLogs_bak table: EmailOutboxId (links to EmailOutbox), StatusId (links to EmailStatus)",
            "EmailStatus table: StatusId (unique), Status (delivered/bounced/failed/opened/clicked/complained/unsubscribed)",
            
            # Required output format - CRITICAL for all queries
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
            
            # Query structure requirements - CRITICAL for all queries
            "Always use LEFT JOINs: et LEFT JOIN eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN st ON es.StatusId = st.StatusId",
            "Always include WHERE et.IsActive = 1 to get only active campaigns",
            "Always GROUP BY et.Description for campaign-level aggregation",
            "Always include ORDER BY clause - required for pagination",
            
            # Date filtering patterns - CRITICAL for time-based queries
            "For 'last month' or 'past month': add AND eo.DateCreated >= DATEADD(month, -1, GETDATE())",
            "For 'last week' or 'past week': add AND eo.DateCreated >= DATEADD(week, -1, GETDATE())",
            "For 'last N days': add AND eo.DateCreated >= DATEADD(day, -N, GETDATE())",
            "For 'last N months': add AND eo.DateCreated >= DATEADD(month, -N, GETDATE())",
            "For 'this year': add AND eo.DateCreated >= DATEADD(year, 0, DATEADD(month, 1-MONTH(GETDATE()), DATEADD(day, 1-DAY(GETDATE()), GETDATE())))",
            "ALWAYS filter by eo.DateCreated when user mentions time periods like 'last month', 'recent', 'this week', etc.",
            "Recent campaigns, latest campaigns, newest campaigns all require date filtering with eo.DateCreated",
            
            # Sorting patterns - CRITICAL for performance queries
            "For 'high open rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'best performing' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'high click rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "For 'low bounce rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC",
            "For 'most emails sent' queries: ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC",
            "For alphabetical sorting: ORDER BY et.Description ASC",
            "Top performing campaigns, best campaigns, most successful campaigns should be ordered by open rate descending",
            "High engagement campaigns, effective campaigns should order by open rate or click rate",
            
            # Performance filtering patterns - CRITICAL for successful campaigns
            "For campaigns with 'high open rates': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'",
            "For campaigns with 'significant volume': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10'",
            "For 'successful campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND COUNT(DISTINCT eo.EmailOutboxId) >= 5'",
            "For 'most successful campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'",
            "For 'best campaigns': Add HAVING clause to filter out campaigns with 0 deliveries: 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'",
            "For 'top performing campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10 AND SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'",
            "For 'effective campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) > 0.1'",
            "ALWAYS exclude campaigns with 0 deliveries when user asks for 'successful', 'best', 'top', 'effective', or 'performing' campaigns",
            "High-performing campaigns, excellent campaigns, outstanding campaigns all need delivery and engagement filtering",
            
            # Problematic campaign filtering patterns - CRITICAL for identifying poor performers
            "For 'problematic campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status IN (''bounced'', ''failed'') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.05 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.5'",
            "For 'campaigns with issues': Add HAVING clause to identify inactive or poor performing: 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) < 0.95'",
            "For 'underperforming campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.6'",
            "For 'campaigns with delivery issues': Add HAVING clause like 'HAVING (CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) < 0.95 OR COUNT(DISTINCT eo.EmailOutboxId) = 0'",
            "For 'campaigns that need improvement': Add HAVING clause to find inactive or below-average performers: 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.8'",
            "For 'poor performing campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.5'",
            "ALWAYS filter for inactive campaigns (0 emails) and poor metrics when user asks for 'problematic', 'issues', 'underperforming', 'poor', or 'need improvement' campaigns",
            "Bad campaigns, failing campaigns, broken campaigns all need problematic filtering with low metrics",
            
            # Enhanced problematic campaign patterns with more synonyms and variations
            "Find poorly performing email campaigns: Use HAVING clause with high bounce rate (>5%) or low open rate (<50%) criteria",
            "Show campaigns that need attention: Filter for inactive campaigns (0 emails) or poor engagement metrics",
            "Identify underperforming email strategies: Add HAVING to find campaigns with open rates below 60%",
            "Campaigns with poor results: Include HAVING clause for high bounce rates or zero deliveries",
            "Problematic email campaigns needing fixes: Filter for campaigns with delivery issues or poor engagement",
            "Email campaigns that are failing: Use HAVING clause to identify campaigns with poor performance metrics",
            "Campaigns requiring improvement: Filter for inactive or below-average performing campaigns",
            "Show email campaigns with low performance: Add HAVING clause for poor open rates and high bounce rates",
            "Identify campaigns with delivery problems: Filter for campaigns with delivery rate below 95%",
            "Find email campaigns that aren't working: Use HAVING clause to identify poor performers and inactive campaigns",
            "Campaigns that need optimization: Include filtering for low engagement and high bounce rates",
            "Show struggling email campaigns: Filter for campaigns with poor metrics or zero activity",
            
            # Additional patterns for comprehensive coverage
            "Time-based filtering is CRITICAL: Always add date filters when users mention 'last month', 'recent', 'this week', 'past N days'",
            "Performance-based sorting is CRITICAL: Order by open rates, click rates, or bounce rates when users ask for 'best', 'worst', 'high', 'low' performance",
            "Problematic campaign identification is CRITICAL: Use HAVING clauses to filter for poor performers when users mention 'problematic', 'issues', 'underperforming'",
            "Always include ORDER BY clause - this is required for pagination and prevents SQL errors",
            "Use NULLIF functions to prevent division by zero errors in rate calculations",
            "CAST calculations to FLOAT for accurate percentage calculations in HAVING clauses",
            "Filter et.IsActive = 1 to exclude inactive email triggers from all results",
            "GROUP BY et.Description is required for campaign-level aggregation in all queries",
            
            # Performance metric calculation patterns
            "Open rate calculation: CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
            "Click rate calculation: CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
            "Bounce rate calculation: CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
            "Delivery rate calculation: CAST(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
            
            # Natural language variations that users commonly ask
            "Show me campaigns that aren't performing well: Use problematic campaign HAVING clause with poor metrics",
            "Which email campaigns have low engagement: Filter for campaigns with low open rates using HAVING clause", 
            "Find email campaigns with high bounce rates: Use HAVING clause with bounce rate threshold above 5%",
            "What campaigns need my attention: Identify inactive campaigns or those with poor performance metrics",
            "Show campaigns that are struggling: Filter for campaigns with delivery issues or low engagement",
            "Which email strategies are underperforming: Use HAVING clause for campaigns below average metrics",
            "Find campaigns with poor open rates: Filter for campaigns with open rates below 50% using HAVING clause",
            "Show me failing email campaigns: Identify campaigns with high bounces or zero deliveries",
            "What campaigns have delivery problems: Filter for campaigns with delivery rates below 95%",
            "Find campaigns that need optimization: Use HAVING clause for poor performers and inactive campaigns",
            
            # Example complete queries for reference
            "Example for 'campaigns with high open rates last month': Include date filter 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())' and order by open rate DESC",
            "Example for 'best performing campaigns this week': Include date filter 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())' and order by open rate DESC"
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
        # 🐛 BREAKPOINT LOCATION: Set VS Code breakpoint on the next line!
        # 🔥 Place your VS Code breakpoint on the line below this comment:
        print("🚀 DEBUG: get_context_for_sql_async() called")
        print(f"🔍 DEBUG: user_query = '{user_query}'")
        print(f"🔍 DEBUG: max_context_items = {max_context_items}")
        print(f"🔍 DEBUG: is_initialized = {self.is_initialized}")
        print(f"🔍 DEBUG: knowledge_base size = {len(self.knowledge_base)}")
        
        if not self.is_initialized:
            print("❌ DEBUG: Knowledge base not initialized, returning empty context")
            logger.warning("Knowledge base not initialized, returning empty context")
            return []
        
        try:
            # 🔥 Another good breakpoint location - set breakpoint on next line:
            print("🔍 DEBUG: About to generate embedding for user query...")
            # Generate embedding for user query
            query_embedding = self.embedding_model.encode([user_query], convert_to_numpy=True)
            print(f"✅ DEBUG: Generated query embedding with shape: {query_embedding.shape}")
            
            # 🔥 Breakpoint location for similarity calculation:
            print("🔍 DEBUG: Calculating cosine similarities...")
            # Calculate cosine similarities
            similarities = []
            for i, item in enumerate(self.knowledge_base):
                if item.embedding is not None:
                    similarity = cosine_similarity(query_embedding, item.embedding.reshape(1, -1))[0][0]
                    similarities.append((similarity, i))
            
            print(f"✅ DEBUG: Calculated {len(similarities)} similarities")
            
            # Sort by similarity and get top items
            similarities.sort(key=lambda x: x[0], reverse=True)
            top_indices = [idx for _, idx in similarities[:max_context_items]]
            
            print(f"🔍 DEBUG: Top {len(top_indices)} similarity indices: {top_indices}")
            
            # Return top context items
            context_items = [self.knowledge_base[idx].text for idx in top_indices]
            
            print(f"✅ DEBUG: Returning {len(context_items)} context items")
            for i, item in enumerate(context_items):
                print(f"🔍 DEBUG: Context {i+1}: {item[:100]}...")
            
            logger.debug(f"Retrieved {len(context_items)} context items for query: '{user_query[:50]}...'")
            return context_items
            
        except Exception as e:
            print(f"💥 DEBUG: Exception in get_context_for_sql_async: {e}")
            print(f"🔍 DEBUG: Exception type: {type(e).__name__}")
            import traceback
            print(f"🔍 DEBUG: Full traceback:")
            traceback.print_exc()
            logger.error(f"Error retrieving context for query '{user_query}': {e}")
            return []
