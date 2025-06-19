using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{    /// <summary>
    /// Semantic RAG service that uses embeddings for true semantic search over knowledge base.
    /// </summary>
    public class SemanticNaturalSqlRagService : INaturalSqlRagService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
        private readonly ILogger<SemanticNaturalSqlRagService> _logger;
        private readonly List<KnowledgeItem> _knowledgeBase;
        private bool _isInitialized = false;

        public SemanticNaturalSqlRagService(
            IEmbeddingGenerator<string, Embedding<float>> embeddingService,
            ILogger<SemanticNaturalSqlRagService> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
            _knowledgeBase = new List<KnowledgeItem>();
            
            InitializeKnowledgeBase();
        }

        private void InitializeKnowledgeBase()
        {
            var knowledgeTexts = new List<string>
            {
                // Database schema information
                "EmailTrigger table: Description (campaign name), IsActive (1=active), CommunicationId (links to EmailOutbox)",
                "EmailOutbox_bak table: EmailOutboxId (unique), CommunicationId (links to EmailTrigger), DateCreated (email send time)",
                "WebhookLogs_bak table: EmailOutboxId (links to EmailOutbox), StatusId (links to EmailStatus)",
                "EmailStatus table: StatusId (unique), Status (delivered/bounced/failed/opened/clicked/complained/unsubscribed)",
                
                // Required output format
                "ALL queries MUST return exactly these 10 columns with exact aliases: StrategyName, TotalEmails, DeliveredCount, BouncedCount, OpenedCount, ClickedCount, ComplainedCount, UnsubscribedCount, FirstEmailSent, LastEmailSent",
                "Use et.Description AS StrategyName for campaign names",
                "Use COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails for total email count",
                "Use SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount",
                "Use SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount", 
                "Use SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount",
                "Use SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount",
                "Use SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount",
                "Use SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount",
                "Use MIN(eo.DateCreated) AS FirstEmailSent and MAX(eo.DateCreated) AS LastEmailSent for date ranges",
                
                // Query structure requirements
                "Always use LEFT JOINs: et LEFT JOIN eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN st ON es.StatusId = st.StatusId",
                "Always include WHERE et.IsActive = 1 to get only active campaigns",
                "Always GROUP BY et.Description for campaign-level aggregation",
                "Always include ORDER BY clause - required for pagination",
                
                // Date filtering patterns - CRITICAL for time-based queries
                "For 'last month' queries: Add 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())'",
                "For 'last week' queries: Add 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())'", 
                "For 'last N days' queries: Add 'AND eo.DateCreated >= DATEADD(day, -N, GETDATE())'",
                "For 'last N months' queries: Add 'AND eo.DateCreated >= DATEADD(month, -N, GETDATE())'",
                "For 'this year' queries: Add 'AND eo.DateCreated >= DATEADD(year, 0, DATEADD(month, 1-MONTH(GETDATE()), DATEADD(day, 1-DAY(GETDATE()), GETDATE())))'",
                "ALWAYS filter by eo.DateCreated when user mentions time periods like 'last month', 'recent', 'this week', etc.",
                "Recent campaigns, latest campaigns, newest campaigns all require date filtering with eo.DateCreated",
                
                // Sorting patterns - CRITICAL for performance queries  
                "For 'high open rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
                "For 'best performing' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
                "For 'high click rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
                "For 'low bounce rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC",
                "For 'most emails sent' queries: ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC",
                "For alphabetical sorting: ORDER BY et.Description ASC",
                "Top performing campaigns, best campaigns, most successful campaigns should be ordered by open rate descending",
                "High engagement campaigns, effective campaigns should order by open rate or click rate",
                
                // Performance filtering patterns
                "For campaigns with 'high open rates': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'",
                "For campaigns with 'significant volume': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10'",
                "For 'successful campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND COUNT(DISTINCT eo.EmailOutboxId) >= 5'",
                "For 'most successful campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'",
                "For 'best campaigns': Add HAVING clause to filter out campaigns with 0 deliveries: 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'",
                "For 'top performing campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10 AND SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0'",
                "For 'effective campaigns': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) > 0 AND (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) > 0.1'",
                "ALWAYS exclude campaigns with 0 deliveries when user asks for 'successful', 'best', 'top', 'effective', or 'performing' campaigns",
                "High-performing campaigns, excellent campaigns, outstanding campaigns all need delivery and engagement filtering",
                  // Problematic campaign filtering patterns - CRITICAL for identifying poor performers
                "For 'problematic campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status IN (''bounced'', ''failed'') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.05 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.5'",
                "For 'campaigns with issues': Add HAVING clause to identify inactive or poor performing: 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) < 0.95'",
                "For 'underperforming campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.6'",
                "For 'campaigns with delivery issues': Add HAVING clause like 'HAVING (CAST(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) < 0.95 OR COUNT(DISTINCT eo.EmailOutboxId) = 0'",
                "For 'campaigns that need improvement': Add HAVING clause to find inactive or below-average performers: 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.8'",
                "For 'poor performing campaigns': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = ''delivered'' THEN 1 ELSE 0 END), 0)) < 0.5'",
                "ALWAYS filter for inactive campaigns (0 emails) and poor metrics when user asks for 'problematic', 'issues', 'underperforming', 'poor', or 'need improvement' campaigns",
                "Bad campaigns, failing campaigns, broken campaigns all need problematic filtering with low metrics",
                
                // Enhanced problematic campaign patterns with more synonyms and variations
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
                // Additional patterns for comprehensive coverage - mirroring RunAsync method logic
                "Time-based filtering is CRITICAL: Always add date filters when users mention 'last month', 'recent', 'this week', 'past N days'",
                "Performance-based sorting is CRITICAL: Order by open rates, click rates, or bounce rates when users ask for 'best', 'worst', 'high', 'low' performance",
                "Problematic campaign identification is CRITICAL: Use HAVING clauses to filter for poor performers when users mention 'problematic', 'issues', 'underperforming'",
                "Always include ORDER BY clause - this is required for pagination and prevents SQL errors",
                "Use NULLIF functions to prevent division by zero errors in rate calculations",
                "CAST calculations to FLOAT for accurate percentage calculations in HAVING clauses",
                "Filter et.IsActive = 1 to exclude inactive email triggers from all results",
                "GROUP BY et.Description is required for campaign-level aggregation in all queries",
                
                // Performance metric calculation patterns
                "Open rate calculation: CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
                "Click rate calculation: CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)",
                "Bounce rate calculation: CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
                "Delivery rate calculation: CAST(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)",
                
                // Natural language variations that users commonly ask
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
                
                // Example complete queries for reference
                "Example for 'campaigns with high open rates last month': Include date filter 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())' and order by open rate DESC",
                "Example for 'best performing campaigns this week': Include date filter 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())' and order by open rate DESC"
            };

            foreach (var text in knowledgeTexts)
            {
                _knowledgeBase.Add(new KnowledgeItem
                {
                    Text = text,
                    Embedding = null // Will be populated during initialization
                });
            }
        }

        public async Task<List<string>> GetContextForSqlAsync(string userQuery, int maxContextItems = 5)
        {
            try
            {
                // Initialize embeddings if not done already
                if (!_isInitialized)
                {
                    await InitializeEmbeddingsAsync();
                }                // Get embedding for user query
                var queryEmbeddingResult = await _embeddingService.GenerateAsync([userQuery]);
                var queryEmbedding = queryEmbeddingResult[0].Vector;

                // Calculate similarity scores for all knowledge items
                var similarities = new List<(KnowledgeItem item, float similarity)>();
                  foreach (var item in _knowledgeBase)
                {
                    if (item.Embedding.HasValue)
                    {
                        var similarity = CosineSimilarity(queryEmbedding, item.Embedding.Value);
                        similarities.Add((item, similarity));
                    }
                }

                // Sort by similarity score (highest first) and take top items
                var topItems = similarities
                    .OrderByDescending(x => x.similarity)
                    .Take(maxContextItems)
                    .Select(x => x.item.Text)
                    .ToList();

                _logger.LogInformation("Semantic RAG: Found {Count} relevant items for query: {Query}", 
                    topItems.Count, userQuery);

                return topItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in semantic RAG search for query: {Query}", userQuery);
                
                // Fallback to essential patterns if semantic search fails
                return GetFallbackContext();
            }
        }

        private async Task InitializeEmbeddingsAsync()
        {
            try
            {
                _logger.LogInformation("Initializing embeddings for {Count} knowledge base items", _knowledgeBase.Count);                var tasks = _knowledgeBase.Select(async item =>
                {
                    try
                    {
                        var embeddingResult = await _embeddingService.GenerateAsync([item.Text]);
                        item.Embedding = embeddingResult[0].Vector;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate embedding for: {Text}", item.Text.Substring(0, Math.Min(50, item.Text.Length)));
                    }
                });

                await Task.WhenAll(tasks);
                
                var successCount = _knowledgeBase.Count(x => x.Embedding.HasValue);
                _logger.LogInformation("Successfully initialized {SuccessCount}/{TotalCount} embeddings", 
                    successCount, _knowledgeBase.Count);

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize embeddings");
                throw;
            }
        }

        private static float CosineSimilarity(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
        {
            var span1 = embedding1.Span;
            var span2 = embedding2.Span;

            if (span1.Length != span2.Length)
                throw new ArgumentException("Embeddings must have the same length");

            float dotProduct = 0;
            float norm1 = 0;
            float norm2 = 0;

            for (int i = 0; i < span1.Length; i++)
            {
                dotProduct += span1[i] * span2[i];
                norm1 += span1[i] * span1[i];
                norm2 += span2[i] * span2[i];
            }

            return dotProduct / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
        }

        private List<string> GetFallbackContext()
        {
            // Return essential patterns as fallback
            return new List<string>
            {
                "ALL queries MUST return exactly these 10 columns with exact aliases: StrategyName, TotalEmails, DeliveredCount, BouncedCount, OpenedCount, ClickedCount, ComplainedCount, UnsubscribedCount, FirstEmailSent, LastEmailSent",
                "Always use LEFT JOINs: et LEFT JOIN eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN st ON es.StatusId = st.StatusId",
                "Always include WHERE et.IsActive = 1 to get only active campaigns",
                "Always GROUP BY et.Description for campaign-level aggregation",
                "Always include ORDER BY clause - required for pagination"
            };
        }
    }

    public class KnowledgeItem
    {
        public string Text { get; set; } = string.Empty;
        public ReadOnlyMemory<float>? Embedding { get; set; }
    }
}
