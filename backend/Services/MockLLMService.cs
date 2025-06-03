using EmailCampaignReporting.API.Models.DTOs;
using System.Diagnostics;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Mock LLM service for development and testing without requiring actual LLM model
    /// </summary>
    public class MockLLMService : ILLMService
    {
        private readonly ILogger<MockLLMService> _logger;

        public MockLLMService(ILogger<MockLLMService> logger)
        {
            _logger = logger;
        }

        public async Task<NaturalLanguageQueryResponseDto> ProcessQueryAsync(
            string query, 
            string? context = null, 
            bool includeDebugInfo = false)
        {
            var stopwatch = Stopwatch.StartNew();
            await Task.Delay(500); // Simulate processing time

            var response = new NaturalLanguageQueryResponseDto
            {
                OriginalQuery = query,
                Success = true
            };

            // Mock intent extraction and SQL generation based on common patterns
            var intent = await ExtractIntentAsync(query);
            response.Intent = intent.QueryType;
            response.Parameters = intent.Entities;
            response.GeneratedSql = await GenerateSqlAsync(intent);

            if (includeDebugInfo)
            {
                response.DebugInfo = new QueryDebugInfo
                {
                    LlmResponse = $"[MOCK] Processed query: {query}",
                    TokensUsed = query.Split(' ').Length * 2, // Rough estimation
                    LlmProcessingTimeMs = 300,
                    SqlExecutionTimeMs = 50,
                    ConfidenceScore = 0.85f,
                    Warnings = new List<string> { "Using mock LLM service - results are simulated" }
                };
            }

            stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return response;
        }

        public async Task<QueryIntent> ExtractIntentAsync(string query)
        {
            await Task.Delay(100); // Simulate processing

            var lowerQuery = query.ToLowerInvariant();
            var intent = new QueryIntent();

            // Extract query type
            if (lowerQuery.Contains("campaign"))
                intent.QueryType = "campaigns";
            else if (lowerQuery.Contains("recipient") || lowerQuery.Contains("customer") || lowerQuery.Contains("user"))
                intent.QueryType = "recipients";
            else if (lowerQuery.Contains("event") || lowerQuery.Contains("click") || lowerQuery.Contains("open") || lowerQuery.Contains("bounce"))
                intent.QueryType = "events";
            else if (lowerQuery.Contains("list"))
                intent.QueryType = "lists";
            else if (lowerQuery.Contains("metric") || lowerQuery.Contains("rate") || lowerQuery.Contains("performance"))
                intent.QueryType = "metrics";
            else
                intent.QueryType = "campaigns"; // Default

            // Extract action
            if (lowerQuery.Contains("count") || lowerQuery.Contains("how many"))
                intent.Action = "count";
            else if (lowerQuery.Contains("show") || lowerQuery.Contains("get") || lowerQuery.Contains("find"))
                intent.Action = "get";
            else if (lowerQuery.Contains("filter") || lowerQuery.Contains("where"))
                intent.Action = "filter";
            else
                intent.Action = "get"; // Default

            // Extract entities and filters
            ExtractEntitiesAndFilters(lowerQuery, intent);

            return intent;
        }

        public async Task<string> GenerateSqlAsync(QueryIntent intent)
        {
            await Task.Delay(100); // Simulate processing

            return intent.QueryType switch
            {
                "campaigns" => GenerateCampaignsSql(intent),
                "recipients" => GenerateRecipientsSql(intent),
                "events" => GenerateEventsSql(intent),
                "lists" => GenerateListsSql(intent),
                "metrics" => GenerateMetricsSql(intent),
                _ => GenerateCampaignsSql(intent)
            };
        }

        public async Task<bool> IsAvailableAsync()
        {
            await Task.Delay(10);
            return true; // Mock service is always available
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            await Task.Delay(10);

            return new Dictionary<string, object>
            {
                ["ModelType"] = "Mock LLM Service",
                ["Version"] = "1.0.0",
                ["IsInitialized"] = true,
                ["Capabilities"] = new[] { "Intent Extraction", "SQL Generation", "Natural Language Processing" },
                ["MockMode"] = true
            };
        }

        private void ExtractEntitiesAndFilters(string query, QueryIntent intent)
        {
            // Extract date ranges
            if (query.Contains("march") || query.Contains("in march"))
            {
                intent.Entities["dateRange"] = "March 2024";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "DateCreated",
                    Operator = "between",
                    Value = "2024-03-01 AND 2024-03-31"
                });
            }
            else if (query.Contains("last month"))
            {
                intent.Entities["dateRange"] = "Last Month";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "DateCreated",
                    Operator = "greater_than",
                    Value = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-01")
                });
            }

            // Extract campaign names
            if (query.Contains("xyz campaign") || query.Contains("xyz"))
            {
                intent.Entities["campaignName"] = "XYZ Campaign";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "StrategyName",
                    Operator = "contains",
                    Value = "XYZ"
                });
            }
            else if (query.Contains("black friday"))
            {
                intent.Entities["campaignName"] = "Black Friday";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "StrategyName",
                    Operator = "contains",
                    Value = "Black Friday"
                });
            }

            // Extract actions/events
            if (query.Contains("clicked") || query.Contains("click"))
            {
                intent.Entities["eventType"] = "clicked";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "Status",
                    Operator = "equals",
                    Value = "clicked"
                });
            }
            else if (query.Contains("opened") || query.Contains("open"))
            {
                intent.Entities["eventType"] = "opened";
                intent.Filters.Add(new QueryFilter
                {
                    Field = "Status",
                    Operator = "equals",
                    Value = "opened"
                });
            }

            // Set default limit
            if (intent.Limit == null)
            {
                intent.Limit = query.Contains("all") ? 1000 : 50;
            }
        }

        private string GenerateCampaignsSql(QueryIntent intent)
        {
            var sql = @"
SELECT 
    o.StrategyId as campaign_id,
    o.StrategyName as name,
    o.EmailType as type,
    o.Status as status,
    MIN(o.DateCreated) as created_at,
    COUNT(DISTINCT o.EmailOutboxIdentifier) as total_recipients,
    COUNT(CASE WHEN o.StatusId = 1 THEN 1 END) as sent_count,
    COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END) as delivered_count,
    COUNT(CASE WHEN s.Status = 'opened' THEN 1 END) as opened_count,
    COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END) as clicked_count
FROM EmailOutbox o
LEFT JOIN EmailStatus s ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier";

            var whereConditions = new List<string>();

            foreach (var filter in intent.Filters)
            {
                whereConditions.Add(BuildWhereCondition(filter));
            }

            if (whereConditions.Any())
            {
                sql += "\nWHERE " + string.Join(" AND ", whereConditions);
            }

            sql += "\nGROUP BY o.StrategyId, o.StrategyName, o.EmailType, o.Status";
            sql += "\nORDER BY MIN(o.DateCreated) DESC";
            
            if (intent.Limit.HasValue)
            {
                sql += $"\nLIMIT {intent.Limit.Value}";
            }

            return sql;
        }

        private string GenerateRecipientsSql(QueryIntent intent)
        {
            var sql = @"
SELECT 
    CAST(o.EmailCustomers_SID AS STRING) as recipient_id,
    o.EmailTo as email_address,
    o.FirstName as first_name,
    o.LastName as last_name,
    o.Status as status,
    o.DateCreated as created_at,
    COUNT(CASE WHEN s.Status = 'opened' THEN 1 END) as total_opens,
    COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END) as total_clicks
FROM EmailOutbox o
LEFT JOIN EmailStatus s ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier";

            var whereConditions = new List<string>();

            foreach (var filter in intent.Filters)
            {
                whereConditions.Add(BuildWhereCondition(filter));
            }

            if (whereConditions.Any())
            {
                sql += "\nWHERE " + string.Join(" AND ", whereConditions);
            }

            sql += "\nGROUP BY o.EmailCustomers_SID, o.EmailTo, o.FirstName, o.LastName, o.Status, o.DateCreated";
            sql += "\nORDER BY o.DateCreated DESC";
            
            if (intent.Limit.HasValue)
            {
                sql += $"\nLIMIT {intent.Limit.Value}";
            }

            return sql;
        }

        private string GenerateEventsSql(QueryIntent intent)
        {
            var sql = @"
SELECT 
    s.Id as event_id,
    o.StrategyId as campaign_id,
    o.StrategyName as campaign_name,
    CAST(o.EmailCustomers_SID AS STRING) as recipient_id,
    o.EmailTo as email_address,
    s.Status as event_type,
    s.DateCreated_UTC as timestamp,
    o.Subject as subject
FROM EmailOutbox o
JOIN EmailStatus s ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier";

            var whereConditions = new List<string>();

            foreach (var filter in intent.Filters)
            {
                whereConditions.Add(BuildWhereCondition(filter));
            }

            if (whereConditions.Any())
            {
                sql += "\nWHERE " + string.Join(" AND ", whereConditions);
            }

            sql += "\nORDER BY s.DateCreated_UTC DESC";
            
            if (intent.Limit.HasValue)
            {
                sql += $"\nLIMIT {intent.Limit.Value}";
            }

            return sql;
        }

        private string GenerateListsSql(QueryIntent intent)
        {
            var sql = @"
SELECT 
    CAST(o.EmailTemplateId AS STRING) as list_id,
    o.TemplateName as name,
    COUNT(DISTINCT o.EmailOutboxIdentifier) as total_recipients,
    COUNT(CASE WHEN o.StatusId = 2 THEN 1 END) as active_recipients
FROM EmailOutbox o";

            var whereConditions = new List<string>();

            foreach (var filter in intent.Filters)
            {
                whereConditions.Add(BuildWhereCondition(filter));
            }

            if (whereConditions.Any())
            {
                sql += "\nWHERE " + string.Join(" AND ", whereConditions);
            }

            sql += "\nGROUP BY o.EmailTemplateId, o.TemplateName";
            sql += "\nORDER BY COUNT(DISTINCT o.EmailOutboxIdentifier) DESC";
            
            if (intent.Limit.HasValue)
            {
                sql += $"\nLIMIT {intent.Limit.Value}";
            }

            return sql;
        }

        private string GenerateMetricsSql(QueryIntent intent)
        {
            return @"
SELECT 
    COUNT(DISTINCT o.StrategyId) as total_campaigns,
    COUNT(DISTINCT o.EmailOutboxIdentifier) as total_emails_sent,
    COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END) as delivered_count,
    COUNT(CASE WHEN s.Status = 'opened' THEN 1 END) as opened_count,
    COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END) as clicked_count,
    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END), COUNT(DISTINCT o.EmailOutboxIdentifier)) * 100 as delivery_rate,
    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'opened' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as open_rate,
    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as click_rate
FROM EmailOutbox o
LEFT JOIN EmailStatus s ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier";
        }

        private string BuildWhereCondition(QueryFilter filter)
        {
            return filter.Operator.ToLowerInvariant() switch
            {
                "equals" => $"{filter.Field} = '{filter.Value}'",
                "contains" => $"{filter.Field} LIKE '%{filter.Value}%'",
                "greater_than" => $"{filter.Field} > '{filter.Value}'",
                "less_than" => $"{filter.Field} < '{filter.Value}'",
                "between" => $"{filter.Field} BETWEEN {filter.Value}",
                _ => $"{filter.Field} = '{filter.Value}'"
            };
        }
    }
}
