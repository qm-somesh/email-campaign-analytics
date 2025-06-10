using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Interface for extracting EmailTriggerReportFilterDto filters from natural language queries using LLM
    /// </summary>
    public interface IEmailTriggerFilterService
    {
        /// <summary>
        /// Extract filter parameters from natural language query using LLM exclusively
        /// </summary>
        /// <param name="query">Natural language query from user</param>
        /// <param name="context">Optional context information</param>
        /// <returns>EmailTriggerReportFilterDto with extracted filter parameters</returns>
        Task<EmailTriggerFilterExtractionResult> ExtractFiltersAsync(string query, string? context = null);

        /// <summary>
        /// Check if the service is available and ready to process queries
        /// </summary>
        /// <returns>True if service is ready</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get information about the underlying LLM model
        /// </summary>
        /// <returns>Model information</returns>
        Task<Dictionary<string, object>> GetModelInfoAsync();
    }

    /// <summary>
    /// Result of filter extraction from natural language query
    /// </summary>
    public class EmailTriggerFilterExtractionResult
    {
        /// <summary>
        /// The extracted filter parameters
        /// </summary>
        public EmailTriggerReportFilterDto Filters { get; set; } = new();

        /// <summary>
        /// Explanation of what filters were extracted and why
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// Whether the extraction was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if extraction failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public int ProcessingTimeMs { get; set; }

        /// <summary>
        /// Raw LLM response for debugging
        /// </summary>
        public string? RawLLMResponse { get; set; }

        /// <summary>
        /// Confidence score of the extraction (0-1)
        /// </summary>
        public decimal Confidence { get; set; } = 1.0m;

        /// <summary>
        /// List of filter parameters that were set
        /// </summary>
        public List<string> ExtractedParameters { get; set; } = new();
    }
}
