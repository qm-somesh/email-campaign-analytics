using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services.LLM.Abstractions
{
    /// <summary>
    /// Abstraction for different LLM providers (LLAMA, Gemini, etc.)
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// The name of the LLM provider (e.g., "LLAMA", "GEMINI")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Extract filter parameters from natural language query
        /// </summary>
        /// <param name="query">Natural language query from user</param>
        /// <param name="context">Optional context information for RAG</param>
        /// <returns>Filter extraction result</returns>
        Task<EmailTriggerFilterExtractionResult> ExtractFiltersAsync(string query, string? context = null);

        /// <summary>
        /// Check if the provider is available and ready to process queries
        /// </summary>
        /// <returns>True if provider is ready</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get information about the underlying model
        /// </summary>
        /// <returns>Model information</returns>
        Task<Dictionary<string, object>> GetModelInfoAsync();

        /// <summary>
        /// Get provider-specific configuration information
        /// </summary>
        /// <returns>Configuration details</returns>
        Dictionary<string, object> GetProviderConfig();
    }
}
