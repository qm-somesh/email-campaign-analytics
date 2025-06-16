using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services.LLM.Providers
{
    /// <summary>
    /// LLAMA LLM provider wrapper that adapts the existing EmailTriggerFilterService
    /// </summary>
    public class LlamaLLMProvider : ILLMProvider, IDisposable
    {
        private readonly EmailTriggerFilterService _llamaService;
        private readonly LLMOptions _options;
        private readonly ILogger<LlamaLLMProvider> _logger;

        public string ProviderName => "LLAMA";

        public LlamaLLMProvider(
            EmailTriggerFilterService llamaService,
            IOptions<LLMOptions> options,
            ILogger<LlamaLLMProvider> logger)
        {
            _llamaService = llamaService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailTriggerFilterExtractionResult> ExtractFiltersAsync(string query, string? context = null)
        {
            _logger.LogInformation("Processing query with LLAMA provider: {Query}", query);
            
            try
            {
                // Use existing LLAMA service logic
                var result = await _llamaService.ExtractFiltersAsync(query, context);
                
                // Add provider-specific metadata
                result.ExtractedParameters.Add($"Provider: {ProviderName}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query with LLAMA provider");
                return new EmailTriggerFilterExtractionResult
                {
                    Success = false,
                    Error = $"LLAMA provider error: {ex.Message}",
                    Filters = new EmailTriggerReportFilterDto()
                };
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                return await _llamaService.IsAvailableAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLAMA provider availability check failed");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            try
            {
                var info = await _llamaService.GetModelInfoAsync();
                info["Provider"] = ProviderName;
                info["ModelType"] = "LLAMA";
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get LLAMA model info");
                return new Dictionary<string, object>
                {
                    ["Provider"] = ProviderName,
                    ["Available"] = false,
                    ["Error"] = ex.Message
                };
            }
        }

        public Dictionary<string, object> GetProviderConfig()
        {
            return new Dictionary<string, object>
            {
                ["Provider"] = ProviderName,
                ["ModelPath"] = _options.ModelPath,
                ["MaxTokens"] = _options.MaxTokens,
                ["Temperature"] = _options.Temperature,
                ["TopP"] = _options.TopP,
                ["ContextSize"] = _options.ContextSize,
                ["GpuLayers"] = _options.GpuLayers,
                ["TimeoutSeconds"] = _options.TimeoutSeconds
            };
        }

        public void Dispose()
        {
            _llamaService?.Dispose();
        }
    }
}
