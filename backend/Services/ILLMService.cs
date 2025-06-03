using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Interface for LLM-based natural language query processing
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Process a natural language query and return structured results
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="context">Optional context for the query</param>
        /// <param name="includeDebugInfo">Whether to include debug information</param>
        /// <returns>Processed query response</returns>
        Task<NaturalLanguageQueryResponseDto> ProcessQueryAsync(
            string query, 
            string? context = null, 
            bool includeDebugInfo = false);

        /// <summary>
        /// Extract intent from natural language query
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <returns>Structured query intent</returns>
        Task<QueryIntent> ExtractIntentAsync(string query);

        /// <summary>
        /// Generate SQL query from structured intent
        /// </summary>
        /// <param name="intent">The query intent</param>
        /// <returns>SQL query string</returns>
        Task<string> GenerateSqlAsync(QueryIntent intent);

        /// <summary>
        /// Check if the LLM service is available and ready
        /// </summary>
        /// <returns>True if service is ready</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get information about the loaded model
        /// </summary>
        /// <returns>Model information</returns>
        Task<Dictionary<string, object>> GetModelInfoAsync();
    }
}
