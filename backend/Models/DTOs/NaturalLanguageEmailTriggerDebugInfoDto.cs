using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Debug information for natural language email trigger query processing
    /// Provides detailed insights into how the query was processed and filters were extracted
    /// </summary>
    public class NaturalLanguageEmailTriggerDebugInfoDto
    {
        /// <summary>
        /// The raw response from the LLM
        /// </summary>
        public string RawLlmResponse { get; set; } = string.Empty;

        /// <summary>
        /// The system prompt sent to the LLM
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// The user prompt sent to the LLM
        /// </summary>
        public string UserPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Time taken for LLM processing in milliseconds
        /// </summary>
        public long LlmProcessingTimeMs { get; set; }

        /// <summary>
        /// Time taken for filter parsing in milliseconds
        /// </summary>
        public long FilterParsingTimeMs { get; set; }

        /// <summary>
        /// Time taken for database query execution in milliseconds
        /// </summary>
        public long DatabaseQueryTimeMs { get; set; }

        /// <summary>
        /// Whether JSON parsing was successful
        /// </summary>
        public bool JsonParsingSuccessful { get; set; }

        /// <summary>
        /// Any JSON parsing errors encountered
        /// </summary>
        public string? JsonParsingError { get; set; }

        /// <summary>
        /// The parsed JSON object from LLM response (before filter mapping)
        /// </summary>
        public object? ParsedLlmJson { get; set; }

        /// <summary>
        /// List of filter fields that were successfully extracted
        /// </summary>
        public List<string> ExtractedFilterFields { get; set; } = new();

        /// <summary>
        /// List of filter fields that could not be extracted
        /// </summary>
        public List<string> FailedFilterFields { get; set; } = new();

        /// <summary>
        /// Any errors encountered during processing
        /// </summary>
        public List<string> ProcessingErrors { get; set; } = new();

        /// <summary>
        /// Additional debug messages
        /// </summary>
        public List<string> DebugMessages { get; set; } = new();

        /// <summary>
        /// The confidence score of the LLM response (if provided)
        /// </summary>
        public double? ConfidenceScore { get; set; }

        /// <summary>
        /// Whether the query required any fallback processing
        /// </summary>
        public bool RequiredFallback { get; set; }

        /// <summary>
        /// The final SQL query parameters that were generated
        /// </summary>
        public object? GeneratedSqlParameters { get; set; }

        /// <summary>
        /// Number of database records found before filtering
        /// </summary>
        public int? TotalRecordsBeforeFiltering { get; set; }

        /// <summary>
        /// Number of database records found after filtering
        /// </summary>
        public int? TotalRecordsAfterFiltering { get; set; }
    }
}
