using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{
    /// <summary>
    /// Simple in-memory RAG service for SQL query context (for demo/reference).
    /// </summary>
    public class InMemoryNaturalSqlRagService : INaturalSqlRagService
    {        private readonly List<string> _knowledgeBase = new()
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
            
            // Sorting patterns - CRITICAL for performance queries  
            "For 'high open rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'best performing' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC",
            "For 'high click rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC",
            "For 'low bounce rates' queries: ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC",
            "For 'most emails sent' queries: ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC",
            "For alphabetical sorting: ORDER BY et.Description ASC",
            
            // Performance filtering patterns
            "For campaigns with 'high open rates': Add HAVING clause like 'HAVING SUM(CASE WHEN st.Status = ''opened'' THEN 1 ELSE 0 END) > 0'",
            "For campaigns with 'significant volume': Add HAVING clause like 'HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10'",
            
            // Example complete queries for reference
            "Example for 'campaigns with high open rates last month': Include date filter 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())' and order by open rate DESC",
            "Example for 'best performing campaigns this week': Include date filter 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())' and order by open rate DESC"
        };        public Task<List<string>> GetContextForSqlAsync(string userQuery, int maxContextItems = 5)
        {
            var query = userQuery.ToLowerInvariant();
            var relevantContext = new List<string>();
            
            // Always include essential schema and structure
            relevantContext.AddRange(_knowledgeBase.Where(kb => 
                kb.Contains("ALL queries MUST return") || 
                kb.Contains("Always use LEFT JOINs") ||
                kb.Contains("Always include WHERE et.IsActive") ||
                kb.Contains("Always GROUP BY")));
            
            // Add date filtering patterns if query mentions time periods
            if (query.Contains("last month") || query.Contains("past month"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("last month")));
            }
            else if (query.Contains("last week") || query.Contains("past week"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("last week")));
            }
            else if (query.Contains("last") && (query.Contains("day") || query.Contains("days")))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("last N days")));
            }
            else if (query.Contains("this year") || query.Contains("current year"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("this year")));
            }
            else if (query.Contains("recent") || query.Contains("latest"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("ALWAYS filter by eo.DateCreated")));
            }
            
            // Add sorting patterns based on requested metrics
            if (query.Contains("high open") || query.Contains("best open") || query.Contains("top open"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("high open rates")));
            }
            else if (query.Contains("high click") || query.Contains("best click") || query.Contains("top click"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("high click rates")));
            }
            else if (query.Contains("best perform") || query.Contains("top perform") || query.Contains("high perform"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("best performing")));
            }
            else if (query.Contains("low bounce") || query.Contains("least bounce"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("low bounce rates")));
            }
            else if (query.Contains("most email") || query.Contains("highest volume"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("most emails sent")));
            }
            
            // Add performance filtering if needed
            if (query.Contains("high") || query.Contains("top") || query.Contains("best"))
            {
                relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("Performance filtering")));
            }
            
            // Add complete examples if available
            relevantContext.AddRange(_knowledgeBase.Where(kb => kb.Contains("Example for") && 
                (kb.ToLowerInvariant().Contains(query.Split(' ').FirstOrDefault() ?? "") ||
                 query.Split(' ').Any(word => kb.ToLowerInvariant().Contains(word)))));
            
            // Fill remaining slots with general patterns
            var remaining = maxContextItems - relevantContext.Count;
            if (remaining > 0)
            {
                var generalPatterns = _knowledgeBase.Except(relevantContext).Take(remaining);
                relevantContext.AddRange(generalPatterns);
            }
            
            return Task.FromResult(relevantContext.Distinct().Take(maxContextItems).ToList());
        }
    }
}
