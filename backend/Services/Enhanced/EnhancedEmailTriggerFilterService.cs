using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace EmailCampaignReporting.API.Services.Enhanced
{
    /// <summary>
    /// Enhanced email trigger filter service that supports multiple LLM providers and RAG
    /// </summary>
    public class EnhancedEmailTriggerFilterService : IEmailTriggerFilterService
    {
        private readonly ILLMServiceFactory _llmServiceFactory;
        private readonly IRAGService? _ragService;
        private readonly LLMProviderOptions _options;
        private readonly ILogger<EnhancedEmailTriggerFilterService> _logger;

        public EnhancedEmailTriggerFilterService(
            ILLMServiceFactory llmServiceFactory,
            IOptions<LLMProviderOptions> options,
            ILogger<EnhancedEmailTriggerFilterService> logger,
            IRAGService? ragService = null)
        {
            _llmServiceFactory = llmServiceFactory;
            _ragService = ragService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailTriggerFilterExtractionResult> ExtractFiltersAsync(string query, string? context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new EmailTriggerFilterExtractionResult
                    {
                        Success = false,
                        Error = "Query cannot be empty",
                        Filters = new EmailTriggerReportFilterDto()
                    };
                }

                _logger.LogInformation("Processing query with enhanced filter service. RAG enabled: {RagEnabled}, Query: {Query}", 
                    _options.EnableRAG, query);

                // Step 1: Enhance query with RAG if enabled
                string enhancedQuery = query;
                var ragInfo = new List<string>();
                
                if (_options.EnableRAG && _ragService != null && await _ragService.IsAvailableAsync())
                {
                    try
                    {
                        var ragResult = await _ragService.EnhanceQueryAsync(query, _options.RAG.MaxRetrievedDocs);
                        enhancedQuery = ragResult.EnhancedQuery;
                        context = string.Join("\n", ragResult.RetrievedContext.Select(c => c.Content));
                        
                        ragInfo.Add($"RAG enhanced query with {ragResult.RetrievedContext.Count} context items");
                        ragInfo.Add($"RAG confidence: {ragResult.ConfidenceScore:F2}");
                        ragInfo.Add($"RAG processing time: {ragResult.ProcessingTimeMs}ms");
                        
                        _logger.LogInformation("Query enhanced with RAG. Context items: {ContextCount}, Confidence: {Confidence}", 
                            ragResult.RetrievedContext.Count, ragResult.ConfidenceScore);
                    }
                    catch (Exception ragEx)
                    {
                        _logger.LogWarning(ragEx, "RAG enhancement failed, proceeding with original query");
                        ragInfo.Add($"RAG enhancement failed: {ragEx.Message}");
                    }
                }

                // Step 2: Get LLM provider and extract filters
                var provider = await _llmServiceFactory.GetDefaultProviderAsync();
                var result = await provider.ExtractFiltersAsync(enhancedQuery, context);

                // Step 3: Add enhanced service metadata
                result.ExtractedParameters.AddRange(ragInfo);
                result.ExtractedParameters.Add($"Enhanced service used");
                result.ExtractedParameters.Add($"Original query length: {query.Length}");
                result.ExtractedParameters.Add($"Enhanced query length: {enhancedQuery.Length}");

                stopwatch.Stop();
                
                _logger.LogInformation("Filter extraction completed. Success: {Success}, Provider: {Provider}, Time: {TimeMs}ms", 
                    result.Success, provider.ProviderName, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in enhanced filter extraction");
                
                return new EmailTriggerFilterExtractionResult
                {
                    Success = false,
                    Error = $"Enhanced filter service error: {ex.Message}",
                    Filters = new EmailTriggerReportFilterDto(),
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var provider = await _llmServiceFactory.GetDefaultProviderAsync();
                return await provider.IsAvailableAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enhanced filter service availability check failed");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            try
            {
                var provider = await _llmServiceFactory.GetDefaultProviderAsync();
                var modelInfo = await provider.GetModelInfoAsync();
                
                // Add enhanced service info
                modelInfo["ServiceType"] = "Enhanced";
                modelInfo["RAGEnabled"] = _options.EnableRAG;
                modelInfo["AvailableProviders"] = _llmServiceFactory.GetAvailableProviders();
                modelInfo["DefaultProvider"] = _options.DefaultProvider;
                modelInfo["FallbackProvider"] = _options.FallbackProvider;

                if (_options.EnableRAG && _ragService != null)
                {
                    try
                    {
                        var ragStats = await _ragService.GetKnowledgeBaseStatsAsync();
                        modelInfo["RAGStats"] = ragStats;
                    }
                    catch (Exception ragEx)
                    {
                        _logger.LogWarning(ragEx, "Failed to get RAG statistics");
                        modelInfo["RAGStats"] = new { Error = ragEx.Message };
                    }
                }

                return modelInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get enhanced model info");
                return new Dictionary<string, object>
                {
                    ["ServiceType"] = "Enhanced",
                    ["Available"] = false,
                    ["Error"] = ex.Message
                };
            }
        }
    }
}
