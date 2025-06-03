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
        private readonly ILogger<NaturalLanguageController> _logger;

        public NaturalLanguageController(
            IBigQueryService bigQueryService,
            ILogger<NaturalLanguageController> logger,
            ILLMService? llmService = null)
        {
            _llmService = llmService;
            _bigQueryService = bigQueryService;
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
            {
                // 🔴 BREAKPOINT HERE - Add breakpoint on this line to debug incoming requests
                var queryText = request?.Query ?? string.Empty;
                var timestamp = DateTime.UtcNow;
                _logger.LogInformation("Processing query at {Timestamp}: '{Query}'", timestamp, queryText);

                if (string.IsNullOrWhiteSpace(request.Query))
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
            };
        }
    }
}
