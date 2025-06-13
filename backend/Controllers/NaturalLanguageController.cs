using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Controllers
{    [ApiController]
    [Route("api/[controller]")]
    public class NaturalLanguageController : ControllerBase
    {
        private readonly ILLMService? _llmService;
        private readonly ISqlServerTriggerService _emailTriggerService;
        private readonly ILogger<NaturalLanguageController> _logger;

        public NaturalLanguageController(
            ISqlServerTriggerService emailTriggerService,
            ILogger<NaturalLanguageController> logger,
            ILLMService? llmService = null)
        {
            _llmService = llmService;
            _emailTriggerService = emailTriggerService;
            _logger = logger;
        }

        /// <summary>
        /// Process natural language queries specifically for EmailTrigger operations
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>EmailTrigger-specific query results with extracted intent and data</returns>
        [HttpPost("triggers/query")]
        public async Task<ActionResult<EmailTriggerNaturalLanguageResponseDto>> ProcessEmailTriggerQuery(
            [FromBody] NaturalLanguageQueryDto request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = new EmailTriggerNaturalLanguageResponseDto
            {
                OriginalQuery = request?.Query ?? string.Empty,
                Success = false
            };

            try
            {
                if (string.IsNullOrWhiteSpace(request?.Query))
                {
                    return BadRequest("Query cannot be empty");
                }

                _logger.LogInformation("Processing EmailTrigger natural language query: {Query}", request.Query);                // Initialize debug info if requested
                if (request.IncludeDebugInfo)
                {
                    response.DebugInfo = new EmailTriggerQueryDebugInfo
                    {
                        ProcessingMethod = "LLM-only", // Rule-based processing is disabled
                        ExtractedFilters = new Dictionary<string, object>(),
                        Warnings = new List<string> { "Rule-based processing disabled, forcing LLM processing only" },
                        AdditionalInfo = new Dictionary<string, object>()
                    };
                }

                // Skip rule-based processing - process with LLM service only
                if (_llmService != null)
                {
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ProcessingMethod = "LLM";
                        // Note: No warning about rule-based failure since it's intentionally disabled
                    }

                    await ProcessEmailTriggerLLMQuery(request.Query, response, request.Context);
                }
                else
                {
                    // Final fallback to mock data
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ProcessingMethod = "fallback";
                        response.DebugInfo.Warnings.Add("LLM service unavailable, using fallback data");
                    }
                    
                    await ProcessEmailTriggerFallbackQuery(request.Query, response);
                }

                response.Success = true;
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EmailTrigger natural language query: {Query}", request?.Query);
                
                response.Error = $"Error processing query: {ex.Message}";
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Process EmailTrigger queries using LLM service
        /// </summary>
        private async Task<bool> ProcessEmailTriggerLLMQuery(string query, EmailTriggerNaturalLanguageResponseDto response, string? context)
        {
            try
            {
                // Create EmailTrigger-specific context for LLM
                var emailTriggerContext = $"""
                    You are analyzing email trigger reports. Available operations:
                    - Summary: Overall email trigger performance statistics
                    - Strategy lookup: Get specific strategy performance by name  
                    - Filtered reports: Get reports with filters (date, performance metrics)
                    - Strategy list: Get all available strategy names
                    - Top performers: Get best performing strategies
                    
                    Context: {context ?? "email_triggers"}
                    """;

                var llmResponse = await _llmService!.ProcessQueryAsync(query, emailTriggerContext, false);
                
                if (llmResponse.Success)
                {
                    response.Intent = llmResponse.Intent;
                    response.GeneratedSql = llmResponse.GeneratedSql;
                    response.Explanation = llmResponse.Parameters?.GetValueOrDefault("explanation")?.ToString();
                    
                    // Map LLM intent to EmailTrigger service calls
                    await MapLLMIntentToEmailTriggerService(llmResponse, response);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in LLM processing for EmailTrigger query");
                return false;
            }
        }        /// <summary>
        /// Map LLM response intent to specific EmailTrigger service calls
        /// </summary>
        private async Task MapLLMIntentToEmailTriggerService(NaturalLanguageQueryResponseDto llmResponse, EmailTriggerNaturalLanguageResponseDto response)
        {
            try
            {
                var intent = llmResponse.Intent?.ToLowerInvariant();
                _logger.LogInformation("MapLLMIntentToEmailTriggerService: Processing intent '{Intent}'", intent);
                
                // 🔥 PRIORITY: Handle numeric threshold filtering from LLM service                if (intent?.StartsWith("filtered_") == true)
                {
                    _logger.LogInformation("MapLLMIntentToEmailTriggerService: Processing filtered intent '{Intent}'", intent);
                    var metricType = intent!.Substring("filtered_".Length);
                    _logger.LogInformation("MapLLMIntentToEmailTriggerService: Extracted metric type '{MetricType}'", metricType);
                    
                    // Extract threshold parameters from LLM response
                    if (llmResponse.Parameters != null &&
                        llmResponse.Parameters.TryGetValue("threshold", out var thresholdObj) &&
                        llmResponse.Parameters.TryGetValue("isGreater", out var isGreaterObj) &&
                        int.TryParse(thresholdObj?.ToString(), out var threshold) &&
                        bool.TryParse(isGreaterObj?.ToString(), out var isGreater))
                    {
                        _logger.LogInformation("MapLLMIntentToEmailTriggerService: Extracted parameters - threshold: {Threshold}, isGreater: {IsGreater}", threshold, isGreater);
                        var filter = new EmailTriggerReportFilterDto
                        {
                            PageSize = 20,
                            SortBy = metricType.Contains("click") ? "ClickedCount" : 
                                    metricType.Contains("open") ? "OpenedCount" : 
                                    metricType.Contains("deliver") ? "DeliveredCount" : "BouncedCount",
                            SortDirection = "desc"
                        };                        // Check if the query is asking for percentages/rates vs raw counts
                        var originalQuery = llmResponse.OriginalQuery?.ToLowerInvariant() ?? "";
                        var isPercentageQuery = originalQuery.Contains("percentage") || originalQuery.Contains("rate") || originalQuery.Contains("%");
                        
                        _logger.LogInformation("MapLLMIntentToEmailTriggerService: Query analysis - isPercentageQuery: {IsPercentageQuery}, originalQuery: {OriginalQuery}", isPercentageQuery, originalQuery);
                        
                        // Apply the appropriate filter based on whether it's percentage or count based
                        if (metricType.Contains("click"))
                        {
                            if (isPercentageQuery)
                            {
                                // Use percentage-based filtering
                                if (isGreater)
                                {
                                    filter.MinClickRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with click rate greater than {threshold}%";
                                }
                                else
                                {
                                    filter.MaxClickRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with click rate less than {threshold}%";
                                }
                            }
                            else
                            {
                                // Use count-based filtering
                                if (isGreater)
                                {
                                    filter.MinClickedCount = threshold;
                                    response.Explanation = $"Getting strategies with click count more than {threshold}";
                                }
                                else
                                {
                                    response.Explanation = $"Getting strategies with click count less than {threshold} (filtering after query)";
                                }
                            }
                        }
                        else if (metricType.Contains("open"))
                        {
                            if (isPercentageQuery)
                            {
                                // Use percentage-based filtering
                                if (isGreater)
                                {
                                    filter.MinOpenRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with open rate greater than {threshold}%";
                                }
                                else
                                {
                                    filter.MaxOpenRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with open rate less than {threshold}%";
                                }
                            }
                            else
                            {
                                // Use count-based filtering
                                if (isGreater)
                                {
                                    filter.MinOpenedCount = threshold;
                                    response.Explanation = $"Getting strategies with open count more than {threshold}";
                                }
                            }
                        }
                        else if (metricType.Contains("deliver"))
                        {
                            if (isPercentageQuery)
                            {
                                // Use percentage-based filtering
                                if (isGreater)
                                {
                                    filter.MinDeliveryRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with delivery rate greater than {threshold}%";
                                }
                                else
                                {
                                    filter.MaxDeliveryRatePercentage = threshold;
                                    response.Explanation = $"Getting strategies with delivery rate less than {threshold}%";
                                }
                            }
                            else
                            {
                                // Use count-based filtering
                                if (isGreater)
                                {
                                    filter.MinDeliveredCount = threshold;
                                    response.Explanation = $"Getting strategies with delivered count more than {threshold}";
                                }
                            }
                        }
                        
                        var (reports, totalCount) = await _emailTriggerService.GetEmailTriggerReportsFilteredAsync(filter);
                        response.TriggerReports = reports;
                        response.TotalCount = totalCount;
                          if (response.DebugInfo != null)
                        {
                            response.DebugInfo.ServiceMethodCalled = "GetEmailTriggerReportsFilteredAsync (from LLM numeric threshold)";
                            response.DebugInfo.ExtractedFilters ??= new Dictionary<string, object>();
                            response.DebugInfo.ExtractedFilters["metricType"] = metricType;
                            response.DebugInfo.ExtractedFilters["threshold"] = threshold;
                            response.DebugInfo.ExtractedFilters["isGreater"] = isGreater;
                            response.DebugInfo.ExtractedFilters["isPercentageQuery"] = isPercentageQuery;
                            
                            if (isPercentageQuery)
                            {
                                var filterProperty = metricType.Contains("click") ? "MinClickRatePercentage" : 
                                                   metricType.Contains("open") ? "MinOpenRatePercentage" : 
                                                   metricType.Contains("deliver") ? "MinDeliveryRatePercentage" : "UnknownPercentage";
                                response.DebugInfo.ExtractedFilters["appliedFilter"] = $"{filterProperty}={threshold}%";
                            }
                            else
                            {
                                var filterProperty = metricType.Contains("click") ? "MinClickedCount" : 
                                                   metricType.Contains("open") ? "MinOpenedCount" : 
                                                   metricType.Contains("deliver") ? "MinDeliveredCount" : "UnknownCount";
                                response.DebugInfo.ExtractedFilters["appliedFilter"] = $"{filterProperty}={threshold}";
                            }
                        }
                        
                        _logger.LogInformation("Successfully applied LLM numeric threshold filter: {MetricType} {Operator} {Threshold}", 
                            metricType, isGreater ? ">" : "<", threshold);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("LLM numeric threshold intent detected but parameters missing or invalid");
                        // Fall through to default handling
                    }
                }
                
                switch (intent)
                {
                    case "summary":
                    case "metrics":
                        response.Summary = await _emailTriggerService.GetEmailTriggerSummaryAsync();
                        break;
                        
                    case "campaigns":
                    case "strategy":
                        // Try to extract strategy name from parameters or SQL
                        var strategyName = ExtractStrategyNameFromLLMResponse(llmResponse);
                        if (!string.IsNullOrEmpty(strategyName))
                        {
                            var report = await _emailTriggerService.GetEmailTriggerReportByStrategyNameAsync(strategyName);
                            if (report != null)
                            {
                                response.TriggerReports = new[] { report };
                            }
                        }
                        else
                        {
                            // Default to getting recent reports
                            response.TriggerReports = await _emailTriggerService.GetEmailTriggerReportsAsync();
                        }
                        break;
                        
                    case "recipients":
                    case "events":
                    default:
                        // Default to getting recent reports with summary
                        response.TriggerReports = await _emailTriggerService.GetEmailTriggerReportsAsync(pageSize: 10);
                        response.Summary = await _emailTriggerService.GetEmailTriggerSummaryAsync();
                        break;
                }
                
                if (response.DebugInfo != null)
                {
                    response.DebugInfo.ServiceMethodCalled = $"Mapped from LLM intent: {llmResponse.Intent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping LLM intent to EmailTrigger service");
                // Fallback to summary
                response.Summary = await _emailTriggerService.GetEmailTriggerSummaryAsync();
            }
        }

        /// <summary>
        /// Extract strategy name from LLM response parameters or SQL
        /// </summary>
        private string? ExtractStrategyNameFromLLMResponse(NaturalLanguageQueryResponseDto llmResponse)
        {
            // Try to extract from parameters first
            if (llmResponse.Parameters?.ContainsKey("strategyName") == true)
            {
                return llmResponse.Parameters["strategyName"]?.ToString();
            }
            
            // Try to extract from SQL WHERE clauses
            if (!string.IsNullOrEmpty(llmResponse.GeneratedSql))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    llmResponse.GeneratedSql, 
                    @"StrategyName\s*=\s*['""]([^'""]+)['""]", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Provide fallback responses when both rule-based and LLM processing fail
        /// </summary>
        private async Task ProcessEmailTriggerFallbackQuery(string query, EmailTriggerNaturalLanguageResponseDto response)
        {
            response.Intent = "fallback";
            response.Explanation = "Provided fallback data due to processing limitations";
            
            // Provide a basic summary as fallback
            try
            {
                response.Summary = await _emailTriggerService.GetEmailTriggerSummaryAsync();
                response.TriggerReports = await _emailTriggerService.GetEmailTriggerReportsAsync(pageSize: 5);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting fallback data for EmailTrigger query");
                response.Error = "Unable to retrieve data";
            }
        }

    }
}
