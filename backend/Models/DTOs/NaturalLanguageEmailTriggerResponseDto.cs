using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Response DTO for natural language email trigger queries
    /// Contains the filtered results and metadata about the query processing
    /// </summary>
    public class NaturalLanguageEmailTriggerResponseDto
    {
        /// <summary>
        /// The original natural language query from the user
        /// </summary>
        [Required]
        public string OriginalQuery { get; set; } = string.Empty;

        /// <summary>
        /// Paginated response containing the filtered email trigger reports
        /// </summary>
        [Required]
        public PaginatedResponse<EmailTriggerReportDto> Results { get; set; } = new();

        /// <summary>
        /// The extracted filter parameters that were applied
        /// </summary>
        [Required]
        public EmailTriggerReportFilterDto AppliedFilters { get; set; } = new();

        /// <summary>
        /// Total processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Whether the LLM successfully extracted meaningful filters
        /// </summary>
        public bool FilterExtractionSuccessful { get; set; }

        /// <summary>
        /// Brief summary of what filters were applied
        /// </summary>
        public string FilterSummary { get; set; } = string.Empty;

        /// <summary>
        /// Optional debug information (only included when debug mode is enabled)
        /// </summary>
        public NaturalLanguageEmailTriggerDebugInfoDto? DebugInfo { get; set; }

        /// <summary>
        /// Timestamp when the query was processed
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether any warnings were encountered during processing
        /// </summary>
        public bool HasWarnings { get; set; }

        /// <summary>
        /// List of warnings encountered during processing
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}
