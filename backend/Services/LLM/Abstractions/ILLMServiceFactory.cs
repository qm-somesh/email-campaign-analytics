using EmailCampaignReporting.API.Services.LLM.Abstractions;

namespace EmailCampaignReporting.API.Services.LLM.Abstractions
{
    /// <summary>
    /// Factory interface for creating LLM provider instances
    /// </summary>
    public interface ILLMServiceFactory
    {
        /// <summary>
        /// Create an LLM provider instance based on configuration
        /// </summary>
        /// <param name="providerName">Name of the provider (e.g., "LLAMA", "GEMINI")</param>
        /// <returns>LLM provider instance</returns>
        Task<ILLMProvider> CreateProviderAsync(string providerName);

        /// <summary>
        /// Get the default LLM provider based on configuration
        /// </summary>
        /// <returns>Default LLM provider instance</returns>
        Task<ILLMProvider> GetDefaultProviderAsync();

        /// <summary>
        /// Get all available provider names
        /// </summary>
        /// <returns>List of available provider names</returns>
        List<string> GetAvailableProviders();

        /// <summary>
        /// Check if a specific provider is available
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <returns>True if provider is available</returns>
        bool IsProviderAvailable(string providerName);
    }
}
