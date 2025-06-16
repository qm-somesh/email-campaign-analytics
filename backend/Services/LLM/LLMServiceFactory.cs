using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using EmailCampaignReporting.API.Services.LLM.Providers;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services.LLM
{
    /// <summary>
    /// Factory for creating LLM provider instances
    /// </summary>
    public class LLMServiceFactory : ILLMServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LLMProviderOptions _options;
        private readonly ILogger<LLMServiceFactory> _logger;
        private readonly Dictionary<string, Func<Task<ILLMProvider>>> _providerFactories;

        public LLMServiceFactory(
            IServiceProvider serviceProvider,
            IOptions<LLMProviderOptions> options,
            ILogger<LLMServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;

            // Initialize provider factories
            _providerFactories = new Dictionary<string, Func<Task<ILLMProvider>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["LLAMA"] = () => Task.FromResult(_serviceProvider.GetRequiredService<LlamaLLMProvider>() as ILLMProvider),
                ["GEMINI"] = () => Task.FromResult(_serviceProvider.GetRequiredService<GeminiLLMProvider>() as ILLMProvider)
            };
        }

        public async Task<ILLMProvider> CreateProviderAsync(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
            }

            if (!_providerFactories.TryGetValue(providerName, out var factory))
            {
                throw new NotSupportedException($"LLM provider '{providerName}' is not supported. Available providers: {string.Join(", ", GetAvailableProviders())}");
            }

            try
            {
                _logger.LogInformation("Creating LLM provider: {ProviderName}", providerName);
                var provider = await factory();
                
                // Verify the provider is available
                if (!await provider.IsAvailableAsync())
                {
                    _logger.LogWarning("LLM provider {ProviderName} is not available", providerName);
                    throw new InvalidOperationException($"LLM provider '{providerName}' is not available");
                }

                _logger.LogInformation("Successfully created LLM provider: {ProviderName}", providerName);
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create LLM provider: {ProviderName}", providerName);
                throw;
            }
        }

        public async Task<ILLMProvider> GetDefaultProviderAsync()
        {
            var defaultProvider = _options.DefaultProvider;
            
            try
            {
                _logger.LogInformation("Getting default LLM provider: {DefaultProvider}", defaultProvider);
                return await CreateProviderAsync(defaultProvider);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create default provider {DefaultProvider}, trying fallback", defaultProvider);
                
                // Try fallback provider if configured
                if (!string.IsNullOrEmpty(_options.FallbackProvider) && 
                    !string.Equals(_options.FallbackProvider, defaultProvider, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _logger.LogInformation("Attempting fallback provider: {FallbackProvider}", _options.FallbackProvider);
                        return await CreateProviderAsync(_options.FallbackProvider);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback provider {FallbackProvider} also failed", _options.FallbackProvider);
                        throw new InvalidOperationException($"Both default provider '{defaultProvider}' and fallback provider '{_options.FallbackProvider}' failed", ex);
                    }
                }
                
                throw new InvalidOperationException($"Default provider '{defaultProvider}' failed and no fallback is configured", ex);
            }
        }

        public List<string> GetAvailableProviders()
        {
            return _providerFactories.Keys.ToList();
        }

        public bool IsProviderAvailable(string providerName)
        {
            return _providerFactories.ContainsKey(providerName);
        }
    }
}
