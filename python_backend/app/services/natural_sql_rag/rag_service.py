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
            
            # HIGH PERFORMANCE PATTERNS - POSITIVE CAMPAIGN QUERIES
            "BEST PERFORMING CAMPAIGNS: Use ORDER BY open rate descending: (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "TOP PERFORMING CAMPAIGNS: Order by highest open rate and add HAVING clause for minimum deliveries: HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0",
            "MOST SUCCESSFUL CAMPAIGNS: Order by open rate descending with engagement filter: HAVING SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) > 0",
            "HIGH PERFORMING CAMPAIGNS: Order by open rate descending and require minimum volume: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10",
            "EXCELLENT CAMPAIGNS: Sort by highest engagement metrics with delivery requirement: HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0",
            "OUTSTANDING CAMPAIGNS: Use high open rate sorting with significant volume filter: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 5",
            "EFFECTIVE CAMPAIGNS: Order by open rate descending with engagement threshold: HAVING (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) > 0.1",
            "SUCCESSFUL CAMPAIGNS: Sort by performance metrics with delivery filter: HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 AND COUNT(DISTINCT eo.EmailOutboxId) >= 5",
            
            # HIGH ENGAGEMENT PATTERNS - POSITIVE ENGAGEMENT QUERIES
            "HIGH OPEN RATES: Order by open rate calculation descending: (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "HIGH CLICK RATES: Order by click rate descending: (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "HIGH ENGAGEMENT: Sort by combined open and click metrics with minimum delivery requirement",
            "BEST ENGAGEMENT: Order by highest open rates with click activity filter: HAVING SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) > 0",
            "STRONG ENGAGEMENT: Use open rate sorting with engagement threshold above 20%",
            "GOOD ENGAGEMENT: Order by open rate with minimum engagement filter: HAVING SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) > 0",
            
            # VOLUME-BASED POSITIVE PATTERNS
            "HIGH VOLUME CAMPAIGNS: Order by total email count descending: ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC",
            "MOST EMAILS SENT: Sort by volume with threshold: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 1000",
            "SIGNIFICANT VOLUME: Filter for campaigns with substantial reach: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 100",
            "LARGE SCALE CAMPAIGNS: Order by email volume with high threshold: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 500",
            
            # LOW BOUNCE RATE PATTERNS - POSITIVE DELIVERY QUERIES  
            "LOW BOUNCE RATES: Order by lowest bounce rate ascending: (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC",
            "BEST DELIVERY: Sort by highest delivery rate with minimum volume: HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10",
            "GOOD DELIVERY RATES: Order by delivery success with low bounce threshold: bounce rate below 5%",
            "RELIABLE DELIVERY: Filter for campaigns with delivery rate above 95%",
            
            # POOR PERFORMANCE PATTERNS - NEGATIVE CAMPAIGN QUERIES
            "POOR PERFORMING CAMPAIGNS: Use HAVING clause for low metrics: (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) < 0.5",
            "UNDERPERFORMING CAMPAIGNS: Filter for below-average metrics: HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR open rate < 60%",
            "STRUGGLING CAMPAIGNS: Identify campaigns with delivery issues or low engagement using HAVING clause",
            "FAILING CAMPAIGNS: Use HAVING clause for high bounce rates or zero deliveries",
            "BAD CAMPAIGNS: Filter for campaigns with poor metrics: high bounces, low opens, zero activity",
            "PROBLEMATIC CAMPAIGNS: HAVING clause for multiple issues: high bounce rate (>5%) OR low open rate (<50%) OR zero deliveries",
            "CAMPAIGNS NEEDING IMPROVEMENT: Filter for inactive or below-average performers with HAVING clause",
            "CAMPAIGNS WITH ISSUES: Identify poor delivery rates or low engagement with HAVING filters",
            "BROKEN CAMPAIGNS: Find campaigns with delivery problems or zero activity using HAVING clause",
            "CAMPAIGNS NEEDING ATTENTION: Filter for poor performance indicators with HAVING clause",
            "CAMPAIGNS REQUIRING FIXES: Use HAVING clause for high bounce rates or poor engagement",
            "CAMPAIGNS THAT NEED OPTIMIZATION: Filter for low performance metrics with HAVING clause",
            
            # LOW ENGAGEMENT PATTERNS - NEGATIVE ENGAGEMENT QUERIES
            "LOW OPEN RATES: Filter for campaigns with poor open rates: HAVING open rate < 50%",
            "LOW CLICK RATES: Use HAVING clause for poor click performance: click rate < 5%",
            "LOW ENGAGEMENT: Filter for campaigns with minimal interaction using HAVING clause",
            "POOR ENGAGEMENT: HAVING clause for campaigns with low open and click rates",
            "WEAK ENGAGEMENT: Filter for below-average engagement metrics with HAVING clause",
            "MINIMAL ENGAGEMENT: Use HAVING clause for campaigns with very low interaction rates",
            
            # HIGH BOUNCE RATE PATTERNS - NEGATIVE DELIVERY QUERIES
            "HIGH BOUNCE RATES: Filter for campaigns with delivery issues: HAVING bounce rate > 5%",
            "DELIVERY PROBLEMS: Use HAVING clause for poor delivery rates: delivery rate < 95%",
            "DELIVERY ISSUES: Filter for campaigns with high bounce rates using HAVING clause",
            "POOR DELIVERY: HAVING clause for campaigns with delivery problems",
            "DELIVERY FAILURES: Filter for campaigns with high failure rates using HAVING clause",
            
            # ALPHABETICAL AND NEUTRAL SORTING
            "ALPHABETICAL SORTING: ORDER BY et.Description ASC for campaign names in alphabetical order",
            "CAMPAIGN OVERVIEW: ORDER BY et.Description ASC for comprehensive campaign listing",
            "ALL CAMPAIGNS: Basic ORDER BY et.Description ASC without performance filtering",
            
            # Performance metric calculation patterns
            "Open rate calculation: CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
            "Click rate calculation: CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
            "Bounce rate calculation: CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
            "Delivery rate calculation: CAST(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
            
            # Additional patterns for comprehensive coverage
            "Time-based filtering is CRITICAL: Always add date filters when users mention 'last month', 'recent', 'this week', 'past N days'",
            "Performance-based sorting is CRITICAL: Order by open rates, click rates, or bounce rates when users ask for 'best', 'worst', 'high', 'low' performance",
            "Always include ORDER BY clause - this is required for pagination and prevents SQL errors",
            "Use NULLIF functions to prevent division by zero errors in rate calculations",
            "CAST calculations to FLOAT for accurate percentage calculations in HAVING clauses",
            "Filter et.IsActive = 1 to exclude inactive email triggers from all results",
            "GROUP BY et.Description is required for campaign-level aggregation in all queries",
            
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
