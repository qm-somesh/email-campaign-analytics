using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Response DTO for natural language queries specifically for EmailTrigger operations
    /// </summary>
    public class EmailTriggerNaturalLanguageResponseDto
    {
        /// <summary>
        /// The original natural language query
        /// </summary>
        public string OriginalQuery { get; set; } = string.Empty;

        /// <summary>
        /// Whether the query was processed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The extracted intent from the query (summary, strategy, filtered, comparison, etc.)
        /// </summary>
        public string? Intent { get; set; }

        /// <summary>
        /// The generated SQL query (if applicable)
        /// </summary>
        public string? GeneratedSql { get; set; }

        /// <summary>
        /// Explanation of what the query is doing
        /// </summary>
        public string? Explanation { get; set; }

        /// <summary>
        /// The email trigger report results
        /// </summary>
        public IEnumerable<EmailTriggerReportDto>? TriggerReports { get; set; }

        /// <summary>
        /// Summary statistics (for summary queries)
        /// </summary>
        public EmailTriggerReportDto? Summary { get; set; }

        /// <summary>
        /// Available strategy names (for strategy-related queries)
        /// </summary>
        public IEnumerable<string>? AvailableStrategies { get; set; }

        /// <summary>
        /// Total count of results (for pagination)
        /// </summary>
        public int? TotalCount { get; set; }

        /// <summary>
        /// Parameters extracted from the natural language query
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public int ProcessingTimeMs { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Debug information (if requested)
        /// </summary>
        public EmailTriggerQueryDebugInfo? DebugInfo { get; set; }
    }

    /// <summary>
    /// Debug information for EmailTrigger natural language queries
    /// </summary>
    public class EmailTriggerQueryDebugInfo
    {
        /// <summary>
        /// The method used to process the query (rule-based, LLM, fallback)
        /// </summary>
        public string ProcessingMethod { get; set; } = string.Empty;

        /// <summary>
        /// The specific service method called
        /// </summary>
        public string? ServiceMethodCalled { get; set; }

        /// <summary>
        /// Extracted filters and parameters
        /// </summary>
        public Dictionary<string, object>? ExtractedFilters { get; set; }

        /// <summary>
        /// Warnings during processing
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Additional debug information
        /// </summary>
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }
}
