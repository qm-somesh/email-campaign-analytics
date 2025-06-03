namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Simple query request DTO for intent extraction
    /// </summary>
    public class QueryRequestDto
    {
        /// <summary>
        /// The natural language query
        /// </summary>
        public string Query { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for natural language queries
    /// </summary>
    public class NaturalLanguageQueryDto
    {
        /// <summary>
        /// The natural language query from the user
        /// </summary>
        public string Query { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional context about what type of query this is
        /// </summary>
        public string? Context { get; set; }
        
        /// <summary>
        /// Whether to include debugging information in the response
        /// </summary>
        public bool IncludeDebugInfo { get; set; } = false;
    }

    /// <summary>
    /// Response DTO for natural language queries
    /// </summary>
    public class NaturalLanguageQueryResponseDto
    {
        /// <summary>
        /// The original user query
        /// </summary>
        public string OriginalQuery { get; set; } = string.Empty;
        
        /// <summary>
        /// The interpreted intent of the query
        /// </summary>
        public string Intent { get; set; } = string.Empty;
        
        /// <summary>
        /// The generated SQL query
        /// </summary>
        public string? GeneratedSql { get; set; }
        
        /// <summary>
        /// The extracted parameters from the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// The query results
        /// </summary>
        public object? Results { get; set; }
        
        /// <summary>
        /// Whether the query was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if query failed
        /// </summary>
        public string? Error { get; set; }
        
        /// <summary>
        /// Debug information about the processing
        /// </summary>
        public QueryDebugInfo? DebugInfo { get; set; }
        
        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Debug information for query processing
    /// </summary>
    public class QueryDebugInfo
    {
        /// <summary>
        /// The raw LLM response
        /// </summary>
        public string? LlmResponse { get; set; }
        
        /// <summary>
        /// Tokens used by the LLM
        /// </summary>
        public int TokensUsed { get; set; }
        
        /// <summary>
        /// Time taken for LLM processing
        /// </summary>
        public long LlmProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Time taken for SQL execution
        /// </summary>
        public long SqlExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Confidence score of the intent detection
        /// </summary>
        public float ConfidenceScore { get; set; }
        
        /// <summary>
        /// Any warnings during processing
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Structured intent extracted from natural language query
    /// </summary>
    public class QueryIntent
    {
        /// <summary>
        /// The type of query (campaigns, recipients, events, metrics, etc.)
        /// </summary>
        public string QueryType { get; set; } = string.Empty;
        
        /// <summary>
        /// The action to perform (get, filter, count, aggregate, etc.)
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Entities mentioned in the query (campaign names, dates, etc.)
        /// </summary>
        public Dictionary<string, object> Entities { get; set; } = new();
        
        /// <summary>
        /// Filters to apply
        /// </summary>
        public List<QueryFilter> Filters { get; set; } = new();
        
        /// <summary>
        /// Grouping/aggregation requirements
        /// </summary>
        public List<string> GroupBy { get; set; } = new();
        
        /// <summary>
        /// Ordering requirements
        /// </summary>
        public List<QueryOrder> OrderBy { get; set; } = new();
        
        /// <summary>
        /// Limit/pagination
        /// </summary>
        public int? Limit { get; set; }
    }

    /// <summary>
    /// Filter condition extracted from query
    /// </summary>
    public class QueryFilter
    {
        /// <summary>
        /// Field name to filter on
        /// </summary>
        public string Field { get; set; } = string.Empty;
        
        /// <summary>
        /// Operator (equals, contains, greater_than, etc.)
        /// </summary>
        public string Operator { get; set; } = string.Empty;
        
        /// <summary>
        /// Value to filter by
        /// </summary>
        public object Value { get; set; } = new();
    }

    /// <summary>
    /// Ordering specification
    /// </summary>
    public class QueryOrder
    {
        /// <summary>
        /// Field to order by
        /// </summary>
        public string Field { get; set; } = string.Empty;
        
        /// <summary>
        /// Direction (asc, desc)
        /// </summary>
        public string Direction { get; set; } = "asc";
    }
}
