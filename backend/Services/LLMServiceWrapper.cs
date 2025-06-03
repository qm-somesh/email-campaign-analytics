using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Configuration;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services
{    /// <summary>
    /// Wrapper service that attempts to use real LLM service and falls back to mock service on errors
    /// </summary>
    public class LLMServiceWrapper : ILLMService
    {            private ILLMService? _activeService;
        private readonly IOptions<LLMOptions> _options;
        private readonly ILogger<EmailCampaignReporting.API.Services.LLMService> _realLogger;
        private readonly ILogger<MockLLMService> _mockLogger;
        private readonly ILogger<LLMServiceWrapper> _logger;
        private readonly ICampaignQueryService _campaignQueryService;
        private bool _initializationAttempted = false;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public LLMServiceWrapper(
            IOptions<LLMOptions> options, 
            ILogger<EmailCampaignReporting.API.Services.LLMService> realLogger, 
            ILogger<MockLLMService> mockLogger,
            ICampaignQueryService campaignQueryService)
        {
            _options = options;
            _realLogger = realLogger;
            _mockLogger = mockLogger;
            _campaignQueryService = campaignQueryService;
            var loggerFactory = new LoggerFactory();
            _logger = loggerFactory.CreateLogger<LLMServiceWrapper>();
        }

        private async Task<ILLMService> GetServiceAsync()
        {
            if (_activeService != null) return _activeService;

            await _initLock.WaitAsync();
            try
            {
                if (_activeService != null) return _activeService;

                if (!_initializationAttempted)
                {
                    _initializationAttempted = true;                    try
                    {
                        _logger.LogInformation("Attempting to initialize real LLM service with model: {ModelPath}", _options.Value.ModelPath);
                        
                        // Check model compatibility first
                        var compatResult = ModelCompatibilityChecker.CheckModel(_options.Value.ModelPath);
                        if (!compatResult.IsCompatible)
                        {
                            _logger.LogWarning("Model compatibility issues detected: {Issues}", string.Join(", ", compatResult.Issues));
                        }
                        
                        _activeService = new EmailCampaignReporting.API.Services.LLMService(_options, _realLogger, _campaignQueryService);
                        
                        // Test the service by checking if it's available with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var isAvailable = await _activeService.IsAvailableAsync();
                        
                        if (isAvailable)
                        {
                            _logger.LogInformation("✅ Real LLM service initialized successfully");
                            var modelInfo = await _activeService.GetModelInfoAsync();
                            _logger.LogInformation("Model info: {ModelInfo}", System.Text.Json.JsonSerializer.Serialize(modelInfo));
                            return _activeService;
                        }
                        else
                        {
                            _logger.LogWarning("Real LLM service is not available, falling back to mock service");
                            _activeService = new MockLLMService(_mockLogger);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to initialize real LLM service: {ErrorType} - {Message}", 
                            ex.GetType().Name, ex.Message);
                        
                        if (ex.InnerException != null)
                        {
                            _logger.LogError("Inner exception: {InnerException}", ex.InnerException.Message);
                        }
                        
                        _logger.LogInformation("Falling back to mock LLM service");
                        _activeService = new MockLLMService(_mockLogger);
                    }
                }

                return _activeService ?? new MockLLMService(_mockLogger);
            }
            finally
            {
                _initLock.Release();
            }
        }        public async Task<NaturalLanguageQueryResponseDto> ProcessQueryAsync(
            string query, 
            string? context = null, 
            bool includeDebugInfo = false)
        {
            try
            {
                var service = await GetServiceAsync();
                return await service.ProcessQueryAsync(query, context, includeDebugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query with LLM service");
                
                // Return error response
                return new NaturalLanguageQueryResponseDto
                {
                    OriginalQuery = query,
                    Success = false,
                    Error = "LLM service encountered an error processing the query",
                    ProcessingTimeMs = 0
                };
            }
        }

        public async Task<QueryIntent> ExtractIntentAsync(string query)
        {
            var service = await GetServiceAsync();
            return await service.ExtractIntentAsync(query);
        }

        public async Task<string> GenerateSqlAsync(QueryIntent intent)
        {
            var service = await GetServiceAsync();
            return await service.GenerateSqlAsync(intent);
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var service = await GetServiceAsync();
                return await service.IsAvailableAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            try
            {
                var service = await GetServiceAsync();
                return await service.GetModelInfoAsync();
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ServiceType"] = _activeService?.GetType().Name ?? "None",
                    ["IsInitialized"] = _activeService != null
                };
            }
        }
    }
}
