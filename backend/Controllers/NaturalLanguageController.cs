using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Controllers
{    [ApiController]
    [Route("api/[controller]")]
    public class NaturalLanguageController : ControllerBase
    {
        private readonly ILLMService? _llmService;
        private readonly IBigQueryService _bigQueryService;
        private readonly ISqlServerTriggerService _emailTriggerService;
        private readonly ILogger<NaturalLanguageController> _logger;

        public NaturalLanguageController(
            IBigQueryService bigQueryService,
            ISqlServerTriggerService emailTriggerService,
            ILogger<NaturalLanguageController> logger,
            ILLMService? llmService = null)
        {
            _llmService = llmService;
            _bigQueryService = bigQueryService;
            _emailTriggerService = emailTriggerService;
            _logger = logger;
        }

        /// <summary>
        /// Process a natural language query and return results
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>Query results with extracted intent and generated SQL</returns>
        [HttpPost("query")]
        public async Task<ActionResult<NaturalLanguageQueryResponseDto>> ProcessQuery(
            [FromBody] NaturalLanguageQueryDto request)        {
            try
            {                // 🔴 BREAKPOINT HERE - Add breakpoint on this line to debug incoming requests
                var queryText = request?.Query ?? string.Empty;
                var timestamp = DateTime.UtcNow;
                _logger.LogInformation("Processing query at {Timestamp}: '{Query}'", timestamp, queryText);

                if (string.IsNullOrWhiteSpace(request?.Query))
                {
                    return BadRequest("Query cannot be empty");
                }

                if (_llmService == null)
                {
                    return BadRequest("LLM service is not available");
                }

                _logger.LogInformation("Processing natural language query: {Query}", request.Query);

                // Process the query using LLM service
                var response = await _llmService.ProcessQueryAsync(
                    request.Query, 
                    request.Context, 
                    request.IncludeDebugInfo);

                // If SQL was generated successfully, try to execute it
                if (response.Success && !string.IsNullOrEmpty(response.GeneratedSql))
                {
                    try
                    {
                        var results = await ExecuteGeneratedQuery(response);
                        response.Results = results;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to execute generated SQL: {Sql}", response.GeneratedSql);
                        response.DebugInfo?.Warnings.Add($"SQL execution failed: {ex.Message}");
                        
                        // Try to provide fallback results based on intent
                        response.Results = await GetFallbackResults(response.Intent, response.Parameters);
                    }
                }

                return Ok(response);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language query");
                
                return StatusCode(500, new NaturalLanguageQueryResponseDto
                {
                    OriginalQuery = request.Query,
                    Success = false,
                    Error = "Internal server error while processing query"
                });
            }
        }

        /// <summary>
        /// Extract intent from natural language query without executing
        /// </summary>
        /// <param name="request">The query request</param>
        /// <returns>Extracted query intent</returns>
        [HttpPost("intent")]
        public async Task<ActionResult<QueryIntent>> ExtractIntent([FromBody] QueryRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest("Query cannot be empty");
                }

                if (_llmService == null)
                {
                    return BadRequest("LLM service is not available");
                }

                var intent = await _llmService.ExtractIntentAsync(request.Query);
                return Ok(intent);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting intent from query: {Query}", request.Query);
                
                return StatusCode(500, "Failed to extract intent from query");
            }
        }

        /// <summary>
        /// Generate SQL from query intent
        /// </summary>
        /// <param name="intent">The query intent</param>
        /// <returns>Generated SQL query</returns>
        [HttpPost("sql")]
        public async Task<ActionResult<string>> GenerateSql([FromBody] QueryIntent intent)
        {
            try
            {
                if (_llmService == null)
                {
                    return BadRequest("LLM service is not available");
                }

                var sql = await _llmService.GenerateSqlAsync(intent);
                return Ok(sql);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL from intent");
                
                return StatusCode(500, "Failed to generate SQL from intent");
            }
        }

        /// <summary>
        /// Get information about the LLM service status
        /// </summary>
        /// <returns>LLM service information</returns>
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetStatus()
        {
            try
            {
                if (_llmService == null)
                {
                    return Ok(new
                    {
                        IsAvailable = false,
                        ModelInfo = new { Error = "LLM service is not configured" },
                        Timestamp = DateTime.UtcNow
                    });
                }

                var isAvailable = await _llmService.IsAvailableAsync();
                var modelInfo = await _llmService.GetModelInfoAsync();

                return Ok(new
                {
                    IsAvailable = isAvailable,
                    ModelInfo = modelInfo,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LLM service status");
                return StatusCode(500, "Failed to get service status");
            }
        }

        /// <summary>
        /// Get example queries that the system can handle
        /// </summary>
        /// <returns>List of example queries</returns>
        [HttpGet("examples")]
        public ActionResult<object> GetExamples()
        {
            var examples = new
            {
                Campaigns = new[]
                {
                    "Show me all campaigns from March",
                    "What campaigns had the highest click rates?",
                    "Find campaigns with Black Friday in the name",
                    "Get campaign performance for the last month"
                },
                Recipients = new[]
                {
                    "Show customers who clicked in March for XYZ campaign",
                    "Find recipients who haven't opened emails in 30 days",
                    "Get all bounced email addresses",
                    "Show top engaged customers by click count"
                },
                Events = new[]
                {
                    "Show all email opens from last week",
                    "Find click events for Black Friday campaign",
                    "Get bounce events with reasons",
                    "Show recent unsubscribe events"
                },
                Metrics = new[]
                {
                    "What's our overall email performance?",
                    "Show delivery rates by campaign",
                    "Calculate click-through rates for March",
                    "Get engagement metrics for VIP customers"
                }
            };            return Ok(examples);
        }

        private async Task<object?> ExecuteGeneratedQuery(NaturalLanguageQueryResponseDto response)
        {
            // This is where we would execute the generated SQL against BigQuery
            // For now, we'll return mock results based on the intent type
            
            return response.Intent switch
            {
                "campaigns" => await GetMockCampaignResults(),
                "recipients" => await GetMockRecipientResults(),
                "events" => await GetMockEventResults(),
                "lists" => await GetMockListResults(),
                "metrics" => await GetMockMetricResults(),
                _ => new { message = "Query processed successfully but no specific results available" }
            };
        }

        private async Task<object> GetFallbackResults(string intent, Dictionary<string, object> parameters)
        {
            // Provide fallback results using existing BigQuery service methods
            return intent switch
            {
                "campaigns" => await _bigQueryService.GetCampaignsAsync(20),
                "recipients" => await _bigQueryService.GetRecipientsAsync(null, 20),
                "events" => await _bigQueryService.GetRecentEmailEventsAsync(50),
                "lists" => await _bigQueryService.GetEmailListsAsync(20),
                "metrics" => await _bigQueryService.GetDashboardMetricsAsync(),
                _ => new { message = "No fallback results available for this query type" }
            };
        }

        private async Task<object> GetMockCampaignResults()
        {
            await Task.Delay(10);
            return new[]
            {
                new { campaign_id = "campaign-001", name = "Black Friday Campaign", open_rate = 30.5, click_rate = 12.3 },
                new { campaign_id = "campaign-002", name = "Holiday Newsletter", open_rate = 28.1, click_rate = 8.7 },
                new { campaign_id = "campaign-003", name = "Product Launch", open_rate = 35.2, click_rate = 15.6 }
            };
        }

        private async Task<object> GetMockRecipientResults()
        {
            await Task.Delay(10);
            return new[]
            {
                new { email = "john.doe@example.com", first_name = "John", last_name = "Doe", total_clicks = 5 },
                new { email = "jane.smith@example.com", first_name = "Jane", last_name = "Smith", total_clicks = 8 },
                new { email = "bob.wilson@example.com", first_name = "Bob", last_name = "Wilson", total_clicks = 3 }
            };
        }

        private async Task<object> GetMockEventResults()
        {
            await Task.Delay(10);
            return new[]
            {
                new { event_type = "opened", email = "john.doe@example.com", timestamp = DateTime.Now.AddHours(-2), campaign = "Black Friday" },
                new { event_type = "clicked", email = "jane.smith@example.com", timestamp = DateTime.Now.AddHours(-1), campaign = "Black Friday" },
                new { event_type = "opened", email = "bob.wilson@example.com", timestamp = DateTime.Now.AddMinutes(-30), campaign = "Holiday Newsletter" }
            };
        }

        private async Task<object> GetMockListResults()
        {
            await Task.Delay(10);
            return new[]
            {
                new { list_id = "list-001", name = "VIP Customers", total_recipients = 5000, active_recipients = 4850 },
                new { list_id = "list-002", name = "Newsletter Subscribers", total_recipients = 25000, active_recipients = 23500 },
                new { list_id = "list-003", name = "Product Updates", total_recipients = 12000, active_recipients = 11200 }
            };
        }

        private async Task<object> GetMockMetricResults()
        {
            await Task.Delay(10);
            return new
            {
                total_campaigns = 15,
                total_emails_sent = 456000,
                overall_delivery_rate = 94.2,
                overall_open_rate = 28.5,
                overall_click_rate = 18.7,
                period = "Last 30 days"
            };        }

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

                _logger.LogInformation("Processing EmailTrigger natural language query: {Query}", request.Query);

                // Initialize debug info if requested
                if (request.IncludeDebugInfo)
                {
                    response.DebugInfo = new EmailTriggerQueryDebugInfo
                    {
                        ProcessingMethod = "rule-based",
                        ExtractedFilters = new Dictionary<string, object>(),
                        Warnings = new List<string>(),
                        AdditionalInfo = new Dictionary<string, object>()
                    };
                }

                // Try rule-based processing first for common EmailTrigger queries
                var ruleBasedSuccess = await ProcessEmailTriggerRuleBasedQuery(request.Query, response);
                
                if (ruleBasedSuccess)
                {
                    response.Success = true;
                    response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    return Ok(response);
                }

                // Fallback to LLM processing if rule-based fails
                if (_llmService != null)
                {
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ProcessingMethod = "LLM";
                        response.DebugInfo.Warnings.Add("Rule-based processing failed, using LLM");
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
        /// Process EmailTrigger queries using rule-based logic for common patterns
        /// </summary>
        private async Task<bool> ProcessEmailTriggerRuleBasedQuery(string query, EmailTriggerNaturalLanguageResponseDto response)
        {
            var queryLower = query.ToLowerInvariant().Trim();
            
            try
            {
                // Summary queries
                if (queryLower.Contains("summary") || queryLower.Contains("overview") || queryLower.Contains("total"))
                {
                    response.Intent = "summary";
                    response.Explanation = "Getting email trigger performance summary";
                    response.Summary = await _emailTriggerService.GetEmailTriggerSummaryAsync();
                    
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ServiceMethodCalled = "GetEmailTriggerSummaryAsync";
                    }
                    
                    return true;
                }

                // Strategy-specific queries
                var strategyMatch = System.Text.RegularExpressions.Regex.Match(
                    queryLower, 
                    @"strategy\s+[""']?([^""'\s]+)[""']?|campaign\s+[""']?([^""'\s]+)[""']?|for\s+[""']?([^""'\s]+)[""']?"
                );
                
                if (strategyMatch.Success)
                {
                    var strategyName = strategyMatch.Groups[1].Value ?? 
                                     strategyMatch.Groups[2].Value ?? 
                                     strategyMatch.Groups[3].Value;
                    
                    if (!string.IsNullOrEmpty(strategyName))
                    {
                        response.Intent = "strategy";
                        response.Explanation = $"Getting metrics for strategy: {strategyName}";
                        
                        var strategyReport = await _emailTriggerService.GetEmailTriggerReportByStrategyNameAsync(strategyName);
                        if (strategyReport != null)
                        {
                            response.TriggerReports = new[] { strategyReport };
                        }
                        else
                        {
                            // Get available strategies to help user
                            response.AvailableStrategies = await _emailTriggerService.GetStrategyNamesAsync();
                            response.Explanation = $"Strategy '{strategyName}' not found. Available strategies listed.";
                        }
                        
                        if (response.DebugInfo != null)
                        {
                            response.DebugInfo.ServiceMethodCalled = "GetEmailTriggerReportByStrategyNameAsync";
                            response.DebugInfo.ExtractedFilters!["strategyName"] = strategyName;
                        }
                        
                        return true;
                    }
                }

                // List all strategies
                if (queryLower.Contains("strategies") || queryLower.Contains("campaigns") || queryLower.Contains("list"))
                {
                    response.Intent = "list_strategies";
                    response.Explanation = "Getting all available strategy names";
                    response.AvailableStrategies = await _emailTriggerService.GetStrategyNamesAsync();
                    
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ServiceMethodCalled = "GetStrategyNamesAsync";
                    }
                    
                    return true;
                }

                // Top performers or best queries
                if (queryLower.Contains("top") || queryLower.Contains("best") || queryLower.Contains("highest"))
                {
                    response.Intent = "top_performers";
                    response.Explanation = "Getting top performing email triggers";
                      var filter = new EmailTriggerReportFilterDto
                    {
                        PageSize = 10,
                        SortBy = "ClickRate",
                        SortDirection = "desc"
                    };
                    
                    var (reports, totalCount) = await _emailTriggerService.GetEmailTriggerReportsFilteredAsync(filter);
                    response.TriggerReports = reports;
                    response.TotalCount = totalCount;
                    
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ServiceMethodCalled = "GetEmailTriggerReportsFilteredAsync";                        response.DebugInfo.ExtractedFilters!["sortBy"] = "ClickRate";
                        response.DebugInfo.ExtractedFilters["sortDirection"] = "desc";
                    }
                    
                    return true;
                }

                // Recent reports
                if (queryLower.Contains("recent") || queryLower.Contains("latest") || queryLower.Contains("new"))
                {
                    response.Intent = "recent";
                    response.Explanation = "Getting recent email trigger reports";
                    
                    var reports = await _emailTriggerService.GetEmailTriggerReportsAsync(pageSize: 20, offset: 0);
                    response.TriggerReports = reports;
                    
                    if (response.DebugInfo != null)
                    {
                        response.DebugInfo.ServiceMethodCalled = "GetEmailTriggerReportsAsync";
                        response.DebugInfo.ExtractedFilters!["pageSize"] = 20;
                    }
                    
                    return true;
                }

                return false; // No rule matched
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in rule-based processing for EmailTrigger query");
                return false;
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
        }

        /// <summary>
        /// Map LLM response intent to specific EmailTrigger service calls
        /// </summary>
        private async Task MapLLMIntentToEmailTriggerService(NaturalLanguageQueryResponseDto llmResponse, EmailTriggerNaturalLanguageResponseDto response)
        {
            try
            {
                switch (llmResponse.Intent?.ToLowerInvariant())
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

        /// <summary>
        /// Trigger an email campaign based on natural language command
        /// </summary>
        /// <param name="request">The request containing the natural language command</param>
        /// <returns>Result of the email trigger operation</returns>
        [HttpPost("trigger-email")]
        public async Task<ActionResult<EmailTriggerResponseDto>> TriggerEmailCampaign(
            [FromBody] EmailTriggerRequestDto request)
        {            try
            {
                if (string.IsNullOrWhiteSpace(request.Command))
                {
                    return BadRequest("Command cannot be empty");
                }

                if (_llmService == null)
                {
                    return BadRequest("LLM service is not available");
                }

                // Log the received command
                _logger.LogInformation("Received email trigger command: {Command}", request.Command);

                // Process the command using LLM service to extract intent and parameters
                var response = await _llmService.ProcessQueryAsync(request.Command, null, false);

                if (!response.Success || string.IsNullOrEmpty(response.GeneratedSql))
                {
                    return BadRequest("Failed to generate valid SQL for the email trigger");
                }                // Execute the generated SQL to get the recipient list
                var recipientList = await ExecuteGeneratedQuery(response);

                // Ensure we have a valid recipient list
                if (recipientList == null)
                {
                    return BadRequest("Failed to generate recipient list from query");
                }

                // Trigger the email campaign using the SQL execution results
                var triggerResult = await _emailTriggerService.TriggerCampaignAsync(recipientList, response.Parameters);

                return Ok(triggerResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering email campaign");
                return StatusCode(500, "Failed to trigger email campaign");
            }
        }
    }
}
