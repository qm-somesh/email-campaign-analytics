using LLama.Common;
using LLama;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Diagnostics;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// LLM service implementation using LLamaSharp for natural language query processing
    /// </summary>
    public class LLMService : ILLMService, IDisposable
    {
            private readonly LLMOptions _options;
        private readonly ILogger<LLMService> _logger;
        private readonly ICampaignQueryService _campaignQueryService;
        private LLamaWeights? _model;
        private LLamaContext? _context;
        private InteractiveExecutor? _executor;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public LLMService(IOptions<LLMOptions> options, ILogger<LLMService> logger, ICampaignQueryService campaignQueryService)
        {
            _options = options.Value;
            _logger = logger;
            _campaignQueryService = campaignQueryService;
        }

        /// <summary>
        /// Initialize the LLM model and context with timeout and fallback
        /// </summary>
        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                _logger.LogInformation("Initializing LLM model from {ModelPath}", _options.ModelPath);
                
                if (string.IsNullOrEmpty(_options.ModelPath) || !File.Exists(_options.ModelPath))
                {
                    throw new FileNotFoundException($"Model file not found: {_options.ModelPath}");
                }

                // Use timeout for model loading
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));                // Configure model parameters for better compatibility
                var parameters = new ModelParams(_options.ModelPath)
                {
                    ContextSize = (uint)Math.Min(_options.ContextSize, 4096), // Increased for Phi-3
                    GpuLayerCount = 0, // Start with CPU only for stability
                    UseMemorymap = true, // Enable for better performance
                    UseMemoryLock = false // Disable to avoid access violations
                };

                _logger.LogInformation("Attempting to load model with parameters: ContextSize={ContextSize}, GpuLayers={GpuLayers}", 
                    parameters.ContextSize, parameters.GpuLayerCount);                // Load model with timeout and detailed error handling
                await Task.Run(() =>
                {
                    try
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        _logger.LogInformation("Loading model weights...");
                        
                        // Try to load with primary settings
                        try 
                        {
                            _model = LLamaWeights.LoadFromFile(parameters);
                        }
                        catch (Exception ex) when (ex.Message.Contains("access violation") || ex.Message.Contains("0xC0000005"))
                        {
                            _logger.LogWarning("Access violation detected, trying with safer parameters...");                            // Fallback parameters for problematic models
                            parameters.UseMemorymap = false;
                            parameters.UseMemoryLock = false;
                            var currentContextSize = parameters.ContextSize;
                            var maxContextSize = 2048u;
                            parameters.ContextSize = currentContextSize < maxContextSize ? currentContextSize : maxContextSize;
                            
                            _model = LLamaWeights.LoadFromFile(parameters);
                        }                        cts.Token.ThrowIfCancellationRequested();
                        _logger.LogInformation("Creating model context...");
                        _context = _model.CreateContext(parameters);
                        
                        cts.Token.ThrowIfCancellationRequested();
                        _logger.LogInformation("Creating executor...");
                        _executor = new InteractiveExecutor(_context);
                        
                        _logger.LogInformation("Model initialization completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during model loading step: {ErrorType}", ex.GetType().Name);
                        throw;
                    }
                }, cts.Token);

                _isInitialized = true;
                _logger.LogInformation("LLM model initialized successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM model initialization timed out after {Timeout} seconds", _options.TimeoutSeconds);
                throw new TimeoutException($"Model initialization timed out after {_options.TimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize LLM model");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task<NaturalLanguageQueryResponseDto> ProcessQueryAsync(
            string query, 
            string? context = null, 
            bool includeDebugInfo = false)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new NaturalLanguageQueryResponseDto
            {
                OriginalQuery = query,
                Success = false
            };            try
            {                // Try rule-based processing first (faster and more reliable)
                _logger.LogInformation("Attempting rule-based processing first for query: {Query}", query);
                var ruleSuccess = await TryRuleBasedProcessing(query, response);
                
                if (ruleSuccess)
                {
                    _logger.LogInformation("Rule-based processing successful, skipping LLM");
                    response.Success = true;
                    response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    return response;
                }

                // If rule-based processing fails, try LLM
                _logger.LogInformation("Rule-based processing failed, trying LLM for query: {Query}", query);
                await InitializeAsync();

                if (_executor == null)
                {
                    throw new InvalidOperationException("LLM executor not initialized");
                }// Build the prompt with system instructions and context
                var systemPrompt = BuildSystemPrompt(context);
                var fullPrompt = $"{systemPrompt}\n\nQuery: {query}\n\nResponse:";

                _logger.LogInformation("Processing query: {Query}", query);                // Configure inference parameters for focused responses
                var inferenceParams = new InferenceParams()
                {
                    AntiPrompts = new List<string> { "Query:", "Examples:", "\n\nQuery", "User:", "Human:", "Assistant:" },
                    MaxTokens = 200, // Adequate for structured responses
                    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline()
                    {
                        Temperature = 0.2f // Low temperature for consistent structured output
                    }
                };

                // Process the query
                var responseText = "";
                await foreach (var token in _executor.InferAsync(fullPrompt, inferenceParams))
                {
                    responseText += token;
                    if (responseText.Length > _options.MaxTokens * 4) // Prevent runaway generation
                        break;
                }

                // Clean up the response
                responseText = responseText.Trim();
                if (string.IsNullOrEmpty(responseText))
                {
                    responseText = "I'm sorry, I couldn't generate a meaningful response to your query.";
                }                // Parse the response to extract SQL and explanation
                ParseLLMResponse(responseText, response);

                // If LLM didn't generate SQL, provide basic fallback
                if (string.IsNullOrEmpty(response.GeneratedSql))
                {
                    _logger.LogWarning("LLM failed to generate SQL");
                    response.GeneratedSql = "SELECT 'No query generated' as message";
                    response.Parameters = new Dictionary<string, object> { ["explanation"] = "Unable to process query" };
                }

                response.Success = true;
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Query processed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", query);
                response.Error = $"Error processing query: {ex.Message}";
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                return response;
            }
        }        public async Task<QueryIntent> ClassifyQueryAsync(string query)
        {
            var response = await ProcessQueryAsync(
                $"Classify this query intent as one of: Dashboard, Campaign, EmailList, Recipient, Analytics, Other. Query: {query}",
                "You are a query classifier. Respond with only the classification category.",
                false);

            var intent = new QueryIntent();
            
            if (response.Success && !string.IsNullOrEmpty(response.GeneratedSql))
            {
                var classification = response.GeneratedSql.Trim().ToLowerInvariant();
                intent.QueryType = classification switch
                {
                    "dashboard" => "dashboard",
                    "campaign" => "campaigns",
                    "emaillist" => "lists",
                    "recipient" => "recipients", 
                    "analytics" => "metrics",
                    _ => "campaigns"
                };
                intent.Action = "get";
            }
            else
            {
                intent.QueryType = "campaigns";
                intent.Action = "get";
            }

            return intent;
        }        public async Task<bool> ValidateQueryAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length > 1000)
                    return false;

                // Basic validation - could be enhanced with LLM-based validation
                var response = await ProcessQueryAsync(
                    $"Is this a valid email campaign analytics query? Respond with 'yes' or 'no': {query}",
                    "You validate if queries are appropriate for email campaign analytics.",
                    false);

                return response.Success && 
                       response.Intent?.Trim().ToLowerInvariant().StartsWith("yes") == true;
            }
            catch
            {
                return false;
            }
        }        private string BuildSystemPrompt(string? context = null)
        {
            var prompt = @"You are an expert BigQuery SQL generator for email campaign analytics. Respond in this EXACT format:

INTENT: [campaigns|recipients|events|metrics]
SQL: [BigQuery SQL query]
EXPLANATION: [Brief description]

Database Schema:
- Table: `PrecisionEmail.EmailOutbox` (sent emails)
  Key columns: StrategyName, EmailTo, FirstName, LastName, DateCreated, Status, MailgunMessageId
- Table: `PrecisionEmail.EmailStatus` (email events) 
  Key columns: Recipient, Status, DateCreated_UTC, StrategyName, Reason
- Join: EmailOutbox.EmailOutboxIdentifier = EmailStatus.EmailOutboxIdentifier

Response Rules:
1. Always include INTENT:, SQL:, EXPLANATION: labels
2. SQL must be valid BigQuery syntax with backticks around table names
3. Use EXTRACT(MONTH FROM date_column) for month filtering
4. Limit results with LIMIT clause (default 50-100)
5. Group by relevant columns for aggregations

Example responses:
Query: campaigns from March
INTENT: campaigns
SQL: SELECT StrategyName, COUNT(*) as emails_sent FROM `PrecisionEmail.EmailOutbox` WHERE EXTRACT(MONTH FROM DateCreated) = 3 GROUP BY StrategyName ORDER BY emails_sent DESC LIMIT 50
EXPLANATION: March campaigns with email counts

Query: bounced emails  
INTENT: events
SQL: SELECT Recipient, Status, Reason FROM `PrecisionEmail.EmailStatus` WHERE Status = 'Bounced' ORDER BY DateCreated_UTC DESC LIMIT 100
EXPLANATION: Recent bounced email addresses

Query: top recipients
INTENT: recipients  
SQL: SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50
EXPLANATION: Recipients ranked by email volume";

            if (!string.IsNullOrEmpty(context))
            {
                prompt += $"\n\nAdditional Context: {context}";
            }

            return prompt;
        }        private void ParseLLMResponse(string responseText, NaturalLanguageQueryResponseDto response)
        {
            try
            {
                _logger.LogDebug("Parsing LLM response: {Response}", responseText.Substring(0, Math.Min(500, responseText.Length)));

                // Clean up response text
                var cleanResponse = responseText.Trim();
                
                // Extract intent with multiple patterns
                var intentMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanResponse, 
                    @"INTENT:\s*([^\n\r]+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (intentMatch.Success)
                {
                    response.Intent = intentMatch.Groups[1].Value.Trim().ToLowerInvariant();
                    _logger.LogDebug("Extracted intent: {Intent}", response.Intent);
                }                // Extract SQL with improved pattern for multi-line SQL
                var sqlMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanResponse, 
                    @"SQL:\s*(SELECT.*?)(?=\s*EXPLANATION:|\s*Query:|\s*$)", 
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (sqlMatch.Success)
                {
                    var sqlText = sqlMatch.Groups[1].Value.Trim();
                    
                    // Clean up SQL - remove common artifacts and stop at certain keywords
                    sqlText = System.Text.RegularExpressions.Regex.Replace(sqlText, @"\s+", " "); // Normalize whitespace
                    sqlText = sqlText.Replace("```sql", "").Replace("```", "").Trim(); // Remove code blocks
                    
                    // Stop at common terminating patterns
                    var terminators = new[] { " Query", " Examples", " User", " Human", " Assistant" };
                    foreach (var terminator in terminators)
                    {
                        var index = sqlText.IndexOf(terminator, StringComparison.OrdinalIgnoreCase);
                        if (index > 0)
                        {
                            sqlText = sqlText.Substring(0, index).Trim();
                            break;
                        }
                    }
                    
                    response.GeneratedSql = sqlText;
                    _logger.LogDebug("Extracted SQL: {Sql}", sqlText.Substring(0, Math.Min(100, sqlText.Length)));
                }

                // Extract explanation
                var explanationMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanResponse, 
                    @"EXPLANATION:\s*([^\n\r]+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (explanationMatch.Success)
                {
                    response.Parameters = new Dictionary<string, object>
                    {
                        ["explanation"] = explanationMatch.Groups[1].Value.Trim()
                    };
                }                // Fallback: Look for SQL patterns anywhere in the response
                if (string.IsNullOrEmpty(response.GeneratedSql))
                {
                    var selectMatch = System.Text.RegularExpressions.Regex.Match(
                        cleanResponse, 
                        @"(SELECT\s+.*?FROM\s+`[^`]+`.*?)(?:\s*EXPLANATION|\s*Query|\s*Examples|\s*User|\s*Human|\s*$)", 
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (selectMatch.Success)
                    {
                        var fallbackSql = selectMatch.Groups[1].Value.Trim();
                        fallbackSql = System.Text.RegularExpressions.Regex.Replace(fallbackSql, @"\s+", " ");
                        
                        // Additional cleanup for fallback SQL
                        var stopWords = new[] { " Query", " Examples", " User", " Human", " Assistant", " EXPLANATION" };
                        foreach (var stopWord in stopWords)
                        {
                            var index = fallbackSql.IndexOf(stopWord, StringComparison.OrdinalIgnoreCase);
                            if (index > 0)
                            {
                                fallbackSql = fallbackSql.Substring(0, index).Trim();
                                break;
                            }
                        }
                        
                        response.GeneratedSql = fallbackSql;
                        _logger.LogDebug("Fallback SQL extraction successful");
                    }
                }

                // Validate extracted intent
                if (!string.IsNullOrEmpty(response.Intent))
                {
                    var validIntents = new[] { "campaigns", "recipients", "events", "metrics", "dashboard", "lists" };
                    if (!validIntents.Contains(response.Intent))
                    {
                        _logger.LogDebug("Intent '{Intent}' not in valid list, mapping to campaigns", response.Intent);
                        response.Intent = "campaigns"; // Default fallback
                    }
                }
                else
                {
                    response.Intent = "campaigns"; // Default intent
                    _logger.LogDebug("No intent extracted, using default: campaigns");
                }

                _logger.LogDebug("Parsing complete - Intent: {Intent}, HasSQL: {HasSql}, SQLLength: {SqlLength}", 
                    response.Intent, 
                    !string.IsNullOrEmpty(response.GeneratedSql),
                    response.GeneratedSql?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing LLM response, using fallback");
                response.Intent = "campaigns";
                response.GeneratedSql = "";
                response.Parameters = new Dictionary<string, object> { ["explanation"] = "Error parsing response" };
            }
        }

        public async Task<QueryIntent> ExtractIntentAsync(string query)
        {
            return await ClassifyQueryAsync(query);
        }        public async Task<string> GenerateSqlAsync(QueryIntent intent)
        {
            var contextPrompt = intent.QueryType switch
            {
                "dashboard" => "Generate a SQL query for dashboard metrics from EmailOutbox and EmailStatus tables",
                "campaigns" => "Generate a SQL query for campaign analysis from EmailOutbox table",
                "lists" => "Generate a SQL query for email list analysis",
                "recipients" => "Generate a SQL query for recipient analysis from EmailOutbox table",
                "metrics" => "Generate a SQL query for email analytics from EmailOutbox and EmailStatus tables",
                _ => "Generate a SQL query for general email campaign data analysis"
            };

            var response = await ProcessQueryAsync(
                $"Generate a BigQuery SQL query for: {intent.QueryType}. Only return the SQL query in a code block.",
                contextPrompt,
                false);

            return response.GeneratedSql ?? "SELECT 1 as placeholder";
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                await InitializeAsync();
                return _isInitialized && _executor != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            var info = new Dictionary<string, object>
            {
                ["IsInitialized"] = _isInitialized,
                ["ModelPath"] = _options.ModelPath ?? "Not set",
                ["ContextSize"] = _options.ContextSize,
                ["MaxTokens"] = _options.MaxTokens,
                ["Temperature"] = _options.Temperature,
                ["TimeoutSeconds"] = _options.TimeoutSeconds
            };

            if (_isInitialized && _model != null)
            {
                info["ModelLoaded"] = true;
                info["HasContext"] = _context != null;
                info["HasExecutor"] = _executor != null;
            }
            else
            {
                info["ModelLoaded"] = false;
                info["HasContext"] = false;
                info["HasExecutor"] = false;
            }

            return await Task.FromResult(info);
        }        public void Dispose()
        {
            _executor = null; // InteractiveExecutor doesn't implement IDisposable in this version
            _context?.Dispose();
            _model?.Dispose();
            _initializationLock?.Dispose();
        }        private async Task<bool> TryRuleBasedProcessing(string query, NaturalLanguageQueryResponseDto response)
        {
            var queryLower = query.ToLowerInvariant();
            
            try
            {
                _logger.LogInformation("Attempting rule-based processing for query: {Query}", query);
                
                // Date/month mapping for temporal queries
                var monthMapping = new Dictionary<string, int>
                {
                    ["january"] = 1, ["jan"] = 1,
                    ["february"] = 2, ["feb"] = 2,
                    ["march"] = 3, ["mar"] = 3,
                    ["april"] = 4, ["apr"] = 4,
                    ["may"] = 5,
                    ["june"] = 6, ["jun"] = 6,
                    ["july"] = 7, ["jul"] = 7,
                    ["august"] = 8, ["aug"] = 8,
                    ["september"] = 9, ["sep"] = 9,
                    ["october"] = 10, ["oct"] = 10,
                    ["november"] = 11, ["nov"] = 11,
                    ["december"] = 12, ["dec"] = 12
                };

                // Extract month from query
                int? monthNumber = null;
                string? monthName = null;
                foreach (var month in monthMapping)
                {
                    if (queryLower.Contains(month.Key))
                    {
                        monthNumber = month.Value;
                        monthName = month.Key;
                        break;
                    }
                }                // 🔥 PRIORITY: Numeric threshold detection for campaign queries
                _logger.LogInformation("Testing numeric threshold pattern for query: {Query}", queryLower);                var numericThresholdMatch = System.Text.RegularExpressions.Regex.Match(
                    queryLower, 
                    @"(click|open|deliver|bounce).*?(more than|greater than|above|over).*?(\d+)|" +
                    @"(click|open|deliver|bounce).*?(less than|below|under).*?(\d+)|" +
                    @"high.*?(click|open|deliver).*?(rate|count).*?(more than|greater than|above|over).*?(\d+)"
                );
                
                _logger.LogInformation("Numeric threshold match success: {Success}", numericThresholdMatch.Success);
                if (numericThresholdMatch.Success)
                {                    _logger.LogInformation("Match groups: 1='{Group1}', 4='{Group4}', 7='{Group7}', 3='{Group3}', 6='{Group6}', 10='{Group10}'", 
                        numericThresholdMatch.Groups[1].Value, numericThresholdMatch.Groups[4].Value, numericThresholdMatch.Groups[7].Value,
                        numericThresholdMatch.Groups[3].Value, numericThresholdMatch.Groups[6].Value, numericThresholdMatch.Groups[10].Value);
                        
                    var metricType = !string.IsNullOrEmpty(numericThresholdMatch.Groups[1].Value) ? numericThresholdMatch.Groups[1].Value :
                                   !string.IsNullOrEmpty(numericThresholdMatch.Groups[4].Value) ? numericThresholdMatch.Groups[4].Value :
                                   numericThresholdMatch.Groups[7].Value;
                    var thresholdStr = !string.IsNullOrEmpty(numericThresholdMatch.Groups[3].Value) ? numericThresholdMatch.Groups[3].Value :
                                     !string.IsNullOrEmpty(numericThresholdMatch.Groups[6].Value) ? numericThresholdMatch.Groups[6].Value :
                                     numericThresholdMatch.Groups[10].Value;
                    var threshold = int.Parse(thresholdStr);
                    var isGreater = numericThresholdMatch.Groups[2].Success || numericThresholdMatch.Groups[9].Success;
                    
                    _logger.LogInformation("Extracted values: metricType={MetricType}, threshold={Threshold}, isGreater={IsGreater}", 
                        metricType, threshold, isGreater);
                    
                    response.Intent = $"filtered_{metricType}";
                    response.Parameters = new Dictionary<string, object> 
                    { 
                        ["explanation"] = $"LLM Service: Getting strategies with {metricType} count {(isGreater ? "more than" : "less than")} {threshold}",
                        ["metricType"] = metricType,
                        ["threshold"] = threshold,
                        ["isGreater"] = isGreater,
                        ["processing_type"] = "llm_numeric_threshold"
                    };
                    
                    // This will be handled by the controller with the EmailTriggerService
                    response.GeneratedSql = $"-- Numeric threshold query: {metricType} {(isGreater ? ">" : "<")} {threshold}";
                    
                    _logger.LogInformation("LLM Service: Detected numeric threshold query - {MetricType} {Operator} {Threshold}", 
                        metricType, isGreater ? ">" : "<", threshold);
                    return true;
                }

                // Campaign queries
                if (queryLower.Contains("campaign") || queryLower.Contains("strategy"))
                {
                    response.Intent = "campaigns";
                    
                    if (monthNumber.HasValue)
                    {
                        response.GeneratedSql = $"SELECT StrategyName, COUNT(*) as emails_sent, MIN(DateCreated) as first_sent, MAX(DateCreated) as last_sent FROM `PrecisionEmail.EmailOutbox` WHERE EXTRACT(MONTH FROM DateCreated) = {monthNumber.Value} GROUP BY StrategyName ORDER BY emails_sent DESC";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = $"Shows campaigns sent in {monthName} with email counts and date ranges" };
                    }else if (queryLower.Contains("performance") || queryLower.Contains("top") || queryLower.Contains("best"))
                    {
                        // Call the service method for campaign performance metrics
                        try
                        {
                            var performanceMetrics = await _campaignQueryService.GetCampaignPerformanceMetricsAsync(10);
                            response.Results = performanceMetrics;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved campaign performance metrics using service method",
                                ["service_method"] = "GetCampaignPerformanceMetricsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling campaign performance service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT StrategyName, COUNT(*) as total_sent, COUNT(DISTINCT EmailTo) as unique_recipients FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName ORDER BY total_sent DESC LIMIT 10";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Shows top campaigns by volume and reach (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }                    else if (queryLower.Contains("recent") || queryLower.Contains("latest"))
                    {
                        // Call the service method for recent campaigns
                        try
                        {
                            var recentCampaigns = await _campaignQueryService.GetRecentCampaignsAsync(30, 20);
                            response.Results = recentCampaigns;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved recent campaigns using service method",
                                ["service_method"] = "GetRecentCampaignsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling recent campaigns service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT StrategyName, COUNT(*) as emails_sent, MAX(DateCreated) as last_sent FROM `PrecisionEmail.EmailOutbox` WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY) GROUP BY StrategyName ORDER BY last_sent DESC LIMIT 20";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Shows campaigns from the last 30 days (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    else
                    {
                        response.GeneratedSql = "SELECT StrategyName, COUNT(*) as emails_sent, MAX(DateCreated) as last_sent FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName ORDER BY last_sent DESC LIMIT 20";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recent campaigns with email counts" };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful: Intent={Intent}, HasSQL={HasSQL}", response.Intent, !string.IsNullOrEmpty(response.GeneratedSql));
                    return true;
                }
                  // Bounce/failure queries
                if (queryLower.Contains("bounce") || queryLower.Contains("fail"))
                {
                    response.Intent = "events";
                    
                    // Call the service method for bounced emails
                    try
                    {
                        var bouncedEmails = await _campaignQueryService.GetBouncedEmailsAsync(100);
                        response.Results = bouncedEmails;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved bounced emails using service method",
                            ["service_method"] = "GetBouncedEmailsAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling bounced emails service method");
                        
                        // Fallback to SQL generation
                        if (monthNumber.HasValue)
                        {
                            response.GeneratedSql = $"SELECT Recipient, Status, Reason, StrategyName FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Failed', 'Bounced') AND EXTRACT(MONTH FROM DateCreated_UTC) = {monthNumber.Value} ORDER BY DateCreated_UTC DESC LIMIT 100";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = $"Shows bounced and failed emails from {monthName} (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                        else
                        {
                            response.GeneratedSql = "SELECT Recipient, Status, Reason, StrategyName FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Failed', 'Bounced') ORDER BY DateCreated_UTC DESC LIMIT 100";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Shows recent bounced and failed emails (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for bounce query");
                    return true;
                }
                  // Open/click queries  
                if (queryLower.Contains("open") || queryLower.Contains("click") || queryLower.Contains("engagement"))
                {
                    response.Intent = "events";                    // Call the service method for email engagement events
                    try
                    {
                        var engagementEvents = await _campaignQueryService.GetEmailEngagementAsync(null, 100);
                        response.Results = engagementEvents;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved email engagement events using service method",
                            ["service_method"] = "GetEmailEngagementAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling email engagement service method");
                        
                        // Fallback to SQL generation
                        if (monthNumber.HasValue)
                        {
                            response.GeneratedSql = $"SELECT Recipient, Status, StrategyName, DateCreated_UTC FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Opened', 'Clicked') AND EXTRACT(MONTH FROM DateCreated_UTC) = {monthNumber.Value} ORDER BY DateCreated_UTC DESC LIMIT 100";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = $"Shows email opens and clicks from {monthName} (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                        else
                        {
                            response.GeneratedSql = "SELECT Recipient, Status, StrategyName, DateCreated_UTC FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Opened', 'Clicked') ORDER BY DateCreated_UTC DESC LIMIT 100";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Shows recent email opens and clicks (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for engagement query");
                    return true;
                }
                
                // Recipient queries
                if (queryLower.Contains("recipient") || queryLower.Contains("customer"))
                {
                    response.Intent = "recipients";
                      if (queryLower.Contains("top") || queryLower.Contains("most"))
                    {
                        // Call the service method for top recipients
                        try
                        {
                            var topRecipients = await _campaignQueryService.GetTopRecipientsAsync(50);
                            response.Results = topRecipients;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved top recipients using service method",
                                ["service_method"] = "GetTopRecipientsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling top recipients service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Shows top recipients by email volume (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    else if (monthNumber.HasValue)
                    {
                        response.GeneratedSql = $"SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` WHERE EXTRACT(MONTH FROM DateCreated) = {monthNumber.Value} GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = $"Shows recipients who received emails in {monthName}" };
                    }
                    else
                    {
                        response.GeneratedSql = "SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recipients with email counts" };
                    }
                      _logger.LogInformation("Rule-based processing successful for recipient query");
                    return true;
                }

                // Email list queries
                if (queryLower.Contains("list") || queryLower.Contains("email list") || queryLower.Contains("mailing list"))
                {
                    response.Intent = "lists";
                    
                    if (queryLower.Contains("performance") || queryLower.Contains("metrics"))
                    {                        // Call the service method for email list performance
                        try
                        {
                            var listPerformance = await _campaignQueryService.GetListPerformanceMetricsAsync(20);
                            response.Results = listPerformance;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved email list performance using service method",
                                ["service_method"] = "GetListPerformanceMetricsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling email list performance service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT 'Email List Performance' as message, 'Service method failed' as note";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Email list performance query failed (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    else
                    {                        // Call the service method for all email lists
                        try
                        {
                            var emailLists = await _campaignQueryService.GetEmailListsSummaryAsync(50);
                            response.Results = emailLists;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved email lists using service method",
                                ["service_method"] = "GetEmailListsSummaryAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling email lists service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT 'Email Lists' as message, 'Service method failed' as note";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Email lists query failed (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for email list query");
                    return true;
                }

                // Unsubscribe queries
                if (queryLower.Contains("unsubscribe") || queryLower.Contains("opt out") || queryLower.Contains("optout"))
                {
                    response.Intent = "events";
                    
                    // Call the service method for unsubscribe events
                    try
                    {
                        var unsubscribeEvents = await _campaignQueryService.GetEmailEventsByTypeAsync("Unsubscribed", 100);
                        response.Results = unsubscribeEvents;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved unsubscribe events using service method",
                            ["service_method"] = "GetEmailEventsByTypeAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling unsubscribe events service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = "SELECT Recipient, Status, StrategyName, DateCreated_UTC FROM `PrecisionEmail.EmailStatus` WHERE Status = 'Unsubscribed' ORDER BY DateCreated_UTC DESC LIMIT 100";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Shows recent unsubscribe events (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for unsubscribe query");
                    return true;
                }

                // Delivery/delivered queries
                if (queryLower.Contains("deliver") && !queryLower.Contains("fail"))
                {
                    response.Intent = "events";
                    
                    // Call the service method for delivered emails
                    try
                    {
                        var deliveredEvents = await _campaignQueryService.GetEmailEventsByTypeAsync("Delivered", 100);
                        response.Results = deliveredEvents;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved delivered email events using service method",
                            ["service_method"] = "GetEmailEventsByTypeAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling delivered events service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = "SELECT Recipient, Status, StrategyName, DateCreated_UTC FROM `PrecisionEmail.EmailStatus` WHERE Status = 'Delivered' ORDER BY DateCreated_UTC DESC LIMIT 100";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Shows recent delivered emails (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for delivery query");
                    return true;
                }// Dashboard/metrics queries
                if (queryLower.Contains("dashboard") || queryLower.Contains("metric") || queryLower.Contains("summary") || queryLower.Contains("overview"))
                {
                    response.Intent = "metrics";
                    
                    // Call the service method instead of generating SQL
                    try
                    {
                        var dashboardMetrics = await _campaignQueryService.GetDashboardMetricsAsync();
                        response.Results = dashboardMetrics;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved dashboard metrics using service method",
                            ["service_method"] = "GetDashboardMetricsAsync",
                            ["processing_type"] = "service_call"
                        };
                        
                        _logger.LogInformation("Rule-based processing successful for metrics query using service method");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling dashboard metrics service method");
                        
                        // Fallback to SQL generation if service method fails
                        response.GeneratedSql = @"SELECT 
                            COUNT(*) as total_emails_sent,
                            COUNT(DISTINCT StrategyName) as total_campaigns,
                            COUNT(DISTINCT EmailTo) as unique_recipients,
                            COUNTIF(Status = 'Delivered') as delivered_count,
                            COUNTIF(Status = 'Failed') as failed_count,
                            COUNTIF(Status = 'Bounced') as bounced_count
                        FROM `PrecisionEmail.EmailOutbox` o
                        LEFT JOIN `PrecisionEmail.EmailStatus` s ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                        WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Shows overall email metrics for the last 30 days (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                        
                        _logger.LogInformation("Rule-based processing successful for metrics query using SQL fallback");
                        return true;
                    }                }

                // Analytics and reporting queries
                if (queryLower.Contains("analytics") || queryLower.Contains("report") || queryLower.Contains("stats"))
                {
                    response.Intent = "metrics";
                    
                    // Call the service method for email metrics summary
                    try
                    {
                        var emailMetrics = await _campaignQueryService.GetEmailMetricsSummaryAsync(30);
                        response.Results = emailMetrics;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved email analytics summary using service method",
                            ["service_method"] = "GetEmailMetricsSummaryAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling email metrics service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            COUNT(*) as total_emails,
                            COUNT(DISTINCT StrategyName) as campaigns,
                            COUNT(DISTINCT EmailTo) as recipients,
                            AVG(CASE WHEN Status = 'Delivered' THEN 1 ELSE 0 END) * 100 as delivery_rate
                        FROM `PrecisionEmail.EmailOutbox`
                        WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Email analytics summary for the last 30 days (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for analytics query");
                    return true;
                }

                // Campaign comparison queries
                if (queryLower.Contains("compare") && queryLower.Contains("campaign"))
                {
                    response.Intent = "campaigns";
                    
                    // Call the service method for campaign performance comparison
                    try
                    {
                        var campaignMetrics = await _campaignQueryService.GetCampaignPerformanceMetricsAsync(20);
                        response.Results = campaignMetrics;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved campaign comparison metrics using service method",
                            ["service_method"] = "GetCampaignPerformanceMetricsAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling campaign comparison service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            StrategyName,
                            COUNT(*) as sent_count,
                            COUNT(DISTINCT EmailTo) as unique_recipients,
                            ROUND(COUNT(*) / COUNT(DISTINCT EmailTo), 2) as emails_per_recipient
                        FROM `PrecisionEmail.EmailOutbox`
                        GROUP BY StrategyName
                        ORDER BY sent_count DESC
                        LIMIT 20";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Campaign comparison metrics (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for campaign comparison query");
                    return true;
                }

                // Subscriber/audience queries
                if (queryLower.Contains("subscriber") || queryLower.Contains("audience"))
                {
                    response.Intent = "recipients";
                    
                    if (queryLower.Contains("growth") || queryLower.Contains("trend"))
                    {
                        // Call service method for engaged recipients (representing growth)
                        try
                        {
                            var engagedRecipients = await _campaignQueryService.GetEngagedRecipientsAsync(100);
                            response.Results = engagedRecipients;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved engaged subscribers showing audience growth using service method",
                                ["service_method"] = "GetEngagedRecipientsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling engaged recipients service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = @"SELECT 
                                EmailTo,
                                FirstName,
                                LastName,
                                COUNT(*) as emails_received,
                                MAX(DateCreated) as last_email_date
                            FROM `PrecisionEmail.EmailOutbox`
                            WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY)
                            GROUP BY EmailTo, FirstName, LastName
                            HAVING COUNT(*) > 1
                            ORDER BY emails_received DESC
                            LIMIT 100";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Engaged subscribers over the last 90 days (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    else
                    {
                        // General subscriber query
                        try
                        {
                            var topRecipients = await _campaignQueryService.GetTopRecipientsAsync(50);
                            response.Results = topRecipients;
                            response.GeneratedSql = ""; // No SQL needed, used service method
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Retrieved subscriber information using service method",
                                ["service_method"] = "GetTopRecipientsAsync",
                                ["processing_type"] = "service_call"
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling subscriber service method");
                            
                            // Fallback to SQL generation
                            response.GeneratedSql = "SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50";
                            response.Parameters = new Dictionary<string, object> 
                            { 
                                ["explanation"] = "Subscriber information (fallback SQL)",
                                ["service_error"] = ex.Message,
                                ["processing_type"] = "sql_fallback"
                            };
                        }
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for subscriber query");
                    return true;
                }

                // Performance optimization queries
                if (queryLower.Contains("slow") || queryLower.Contains("poor") || (queryLower.Contains("low") && (queryLower.Contains("rate") || queryLower.Contains("performance"))))
                {
                    response.Intent = "events";
                    
                    // Call service method for bounced emails (poor performance indicator)
                    try
                    {
                        var bouncedEmails = await _campaignQueryService.GetBouncedEmailsAsync(null, 50);
                        response.Results = bouncedEmails;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved poor performance indicators (bounced emails) using service method",
                            ["service_method"] = "GetBouncedEmailsAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling poor performance service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            StrategyName,
                            COUNT(*) as issues,
                            COUNT(CASE WHEN Status = 'Failed' THEN 1 END) as failures,
                            COUNT(CASE WHEN Status = 'Bounced' THEN 1 END) as bounces
                        FROM `PrecisionEmail.EmailStatus`
                        WHERE Status IN ('Failed', 'Bounced')
                        GROUP BY StrategyName
                        ORDER BY issues DESC
                        LIMIT 50";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Performance issues by campaign (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for performance issue query");
                    return true;
                }                // General temporal queries (if month is mentioned but no specific category)
                if (monthNumber.HasValue && !queryLower.Contains("campaign") && !queryLower.Contains("bounce") && !queryLower.Contains("open") && !queryLower.Contains("recipient"))
                {
                    response.Intent = "campaigns"; // Default to campaigns for temporal queries
                    response.GeneratedSql = $"SELECT StrategyName, COUNT(*) as emails_sent, COUNT(DISTINCT EmailTo) as unique_recipients FROM `PrecisionEmail.EmailOutbox` WHERE EXTRACT(MONTH FROM DateCreated) = {monthNumber.Value} GROUP BY StrategyName ORDER BY emails_sent DESC";
                    response.Parameters = new Dictionary<string, object> { ["explanation"] = $"Shows email activity for {monthName}" };
                    _logger.LogInformation("Rule-based processing successful for temporal query");
                    return true;
                }

                // High-level business queries
                if (queryLower.Contains("roi") || queryLower.Contains("return on investment") || (queryLower.Contains("business") && queryLower.Contains("impact")))
                {
                    response.Intent = "metrics";
                    
                    // Call dashboard metrics as a proxy for ROI analysis
                    try
                    {
                        var dashboardMetrics = await _campaignQueryService.GetDashboardMetricsAsync(30);
                        response.Results = dashboardMetrics;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved business impact metrics using service method",
                            ["service_method"] = "GetDashboardMetricsAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling ROI metrics service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            'Business Impact Analysis' as metric_type,
                            COUNT(*) as total_emails,
                            COUNT(DISTINCT StrategyName) as campaigns_run,
                            COUNT(DISTINCT EmailTo) as audience_reached,
                            ROUND(COUNT(DISTINCT EmailTo) / COUNT(DISTINCT StrategyName), 0) as avg_reach_per_campaign
                        FROM `PrecisionEmail.EmailOutbox`
                        WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Business impact analysis for the last 30 days (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for ROI/business impact query");
                    return true;
                }

                // Compliance and deliverability queries
                if (queryLower.Contains("compliance") || queryLower.Contains("deliverability") || queryLower.Contains("reputation"))
                {
                    response.Intent = "events";
                    
                    // Use bounce data as a proxy for deliverability issues
                    try
                    {
                        var bouncedEmails = await _campaignQueryService.GetBouncedEmailsAsync(null, 100);
                        response.Results = bouncedEmails;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved compliance/deliverability indicators using service method",
                            ["service_method"] = "GetBouncedEmailsAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling deliverability service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            'Deliverability Report' as report_type,
                            COUNT(CASE WHEN Status = 'Delivered' THEN 1 END) as delivered,
                            COUNT(CASE WHEN Status = 'Bounced' THEN 1 END) as bounced,
                            COUNT(CASE WHEN Status = 'Failed' THEN 1 END) as failed,
                            ROUND(COUNT(CASE WHEN Status = 'Delivered' THEN 1 END) * 100.0 / COUNT(*), 2) as delivery_rate
                        FROM `PrecisionEmail.EmailStatus`
                        WHERE DateCreated_UTC >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Deliverability and compliance report (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for compliance/deliverability query");
                    return true;
                }

                // Segmentation and targeting queries
                if (queryLower.Contains("segment") || queryLower.Contains("target") || queryLower.Contains("audience"))
                {
                    response.Intent = "recipients";
                    
                    // Use email lists as proxy for segmentation
                    try
                    {
                        var emailLists = await _campaignQueryService.GetEmailListsSummaryAsync(20);
                        response.Results = emailLists;
                        response.GeneratedSql = ""; // No SQL needed, used service method
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Retrieved audience segmentation data using service method",
                            ["service_method"] = "GetEmailListsSummaryAsync",
                            ["processing_type"] = "service_call"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling segmentation service method");
                        
                        // Fallback to SQL generation
                        response.GeneratedSql = @"SELECT 
                            CASE 
                                WHEN COUNT(*) > 10 THEN 'High Engagement'
                                WHEN COUNT(*) > 3 THEN 'Medium Engagement'
                                ELSE 'Low Engagement'
                            END as segment,
                            COUNT(DISTINCT EmailTo) as recipients,
                            AVG(COUNT(*)) as avg_emails_per_recipient
                        FROM `PrecisionEmail.EmailOutbox`
                        WHERE DateCreated >= DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY)
                        GROUP BY EmailTo
                        ORDER BY recipients DESC";
                        response.Parameters = new Dictionary<string, object> 
                        { 
                            ["explanation"] = "Audience segmentation by engagement level (fallback SQL)",
                            ["service_error"] = ex.Message,
                            ["processing_type"] = "sql_fallback"
                        };
                    }
                    
                    _logger.LogInformation("Rule-based processing successful for segmentation query");
                    return true;
                }
                
                _logger.LogInformation("No rule-based pattern matched for query");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in rule-based processing");
                return false;
            }
        }
    }
}
