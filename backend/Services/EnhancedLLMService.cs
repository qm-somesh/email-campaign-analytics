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
    /// Enhanced LLM service with rule-based fallback for better natural language query processing
    /// </summary>
    public class EnhancedLLMService : ILLMService, IDisposable
    {
        private readonly LLMOptions _options;
        private readonly ILogger<EnhancedLLMService> _logger;
        private LLamaWeights? _model;
        private LLamaContext? _context;
        private InteractiveExecutor? _executor;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public EnhancedLLMService(IOptions<LLMOptions> options, ILogger<EnhancedLLMService> logger)
        {
            _options = options.Value;
            _logger = logger;
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

                _logger.LogInformation("Initializing Enhanced LLM model from {ModelPath}", _options.ModelPath);
                
                if (string.IsNullOrEmpty(_options.ModelPath) || !File.Exists(_options.ModelPath))
                {
                    _logger.LogWarning("Model file not found, will use rule-based processing only");
                    return;
                }

                // Use timeout for model loading
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                
                // Configure model parameters for better compatibility
                var parameters = new ModelParams(_options.ModelPath)
                {
                    ContextSize = (uint)Math.Min(_options.ContextSize, 4096),
                    GpuLayerCount = 0, // Start with CPU only for stability
                    UseMemorymap = true, // Enable for better performance
                    UseMemoryLock = false // Disable to avoid access violations
                };

                _logger.LogInformation("Attempting to load model with parameters: ContextSize={ContextSize}, GpuLayers={GpuLayers}", 
                    parameters.ContextSize, parameters.GpuLayerCount);

                // Load model with timeout and detailed error handling
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
                            _logger.LogWarning("Access violation detected, trying with safer parameters...");
                            
                            // Fallback parameters for problematic models
                            parameters.UseMemorymap = false;
                            parameters.UseMemoryLock = false;
                            var currentContextSize = parameters.ContextSize;
                            var maxContextSize = 2048u;
                            parameters.ContextSize = currentContextSize < maxContextSize ? currentContextSize : maxContextSize;
                            
                            _model = LLamaWeights.LoadFromFile(parameters);
                        }
                        
                        cts.Token.ThrowIfCancellationRequested();
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
                _logger.LogInformation("Enhanced LLM model initialized successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM model initialization timed out after {Timeout} seconds, will use rule-based processing", _options.TimeoutSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize LLM model, will use rule-based processing only");
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
            };

            try
            {
                // Try rule-based approach first for common patterns
                if (TryRuleBasedProcessing(query, response))
                {
                    response.Success = true;
                    response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    _logger.LogInformation("Query processed using rule-based approach in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return response;
                }

                await InitializeAsync();

                if (_executor == null)
                {
                    _logger.LogWarning("LLM executor not available, using rule-based fallback");
                    // Force rule-based processing with generic patterns
                    TryGenericRuleBasedProcessing(query, response);
                    response.Success = true;
                    response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    return response;
                }

                // Build a very focused prompt
                var simplePrompt = $@"Task: Generate BigQuery SQL for email campaigns.

Query: {query}

Respond with only:
INTENT: campaigns
SQL: SELECT StrategyName FROM `PrecisionEmail.EmailOutbox` LIMIT 10

Response:";

                _logger.LogInformation("Processing query with LLM: {Query}", query);

                // Configure inference parameters for focused responses
                var inferenceParams = new InferenceParams()
                {
                    AntiPrompts = new List<string> { "Query:", "Task:", "\n\n", "User:", "Human:" },
                    MaxTokens = 100, // Very limited for focused responses
                    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline()
                    {
                        Temperature = 0.05f // Extremely low temperature
                    }
                };

                // Process the query
                var responseText = "";
                await foreach (var token in _executor.InferAsync(simplePrompt, inferenceParams))
                {
                    responseText += token;
                    if (responseText.Length > 500) // Prevent runaway generation
                        break;
                }

                // Clean up the response
                responseText = responseText.Trim();
                if (string.IsNullOrEmpty(responseText))
                {
                    // Fallback to rule-based if LLM fails
                    TryGenericRuleBasedProcessing(query, response);
                }
                else
                {
                    // Parse the response to extract SQL and explanation
                    ParseLLMResponse(responseText, response);
                }

                // Ensure we have a valid response
                if (string.IsNullOrEmpty(response.Intent))
                {
                    response.Intent = "campaigns";
                }

                response.Success = true;
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Query processed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Query}", query);
                
                // Try rule-based fallback on error
                if (TryRuleBasedProcessing(query, response) || TryGenericRuleBasedProcessing(query, response))
                {
                    response.Success = true;
                }
                else
                {
                    response.Error = $"Error processing query: {ex.Message}";
                }
                
                response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                return response;
            }
        }

        private bool TryRuleBasedProcessing(string query, NaturalLanguageQueryResponseDto response)
        {
            var queryLower = query.ToLowerInvariant();
            
            try
            {
                // Campaign queries
                if (queryLower.Contains("campaign") || queryLower.Contains("strategy"))
                {
                    response.Intent = "campaigns";
                    
                    if (queryLower.Contains("march") || (queryLower.Contains("month") && queryLower.Contains("3")))
                    {
                        response.GeneratedSql = "SELECT StrategyName, COUNT(*) as emails_sent FROM `PrecisionEmail.EmailOutbox` WHERE EXTRACT(MONTH FROM DateCreated) = 3 GROUP BY StrategyName ORDER BY emails_sent DESC";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows campaigns sent in March with email counts" };
                    }
                    else if (queryLower.Contains("performance") || queryLower.Contains("top"))
                    {
                        response.GeneratedSql = "SELECT StrategyName, COUNT(*) as total_sent FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName ORDER BY total_sent DESC LIMIT 10";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows top campaigns by volume" };
                    }
                    else
                    {
                        response.GeneratedSql = "SELECT StrategyName, COUNT(*) as emails_sent, MAX(DateCreated) as last_sent FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName ORDER BY last_sent DESC LIMIT 20";
                        response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recent campaigns with email counts" };
                    }
                    return true;
                }
                
                // Bounce/failure queries
                if (queryLower.Contains("bounce") || queryLower.Contains("fail"))
                {
                    response.Intent = "events";
                    response.GeneratedSql = "SELECT Recipient, Status, Reason FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Failed', 'Bounced') ORDER BY DateCreated_UTC DESC LIMIT 100";
                    response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recent bounced and failed emails" };
                    return true;
                }
                
                // Open/click queries  
                if (queryLower.Contains("open") || queryLower.Contains("click"))
                {
                    response.Intent = "events";
                    response.GeneratedSql = "SELECT Recipient, Status, DateCreated_UTC FROM `PrecisionEmail.EmailStatus` WHERE Status IN ('Opened', 'Clicked') ORDER BY DateCreated_UTC DESC LIMIT 100";
                    response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recent email opens and clicks" };
                    return true;
                }
                
                // Recipient queries
                if (queryLower.Contains("recipient") || queryLower.Contains("customer"))
                {
                    response.Intent = "recipients";
                    response.GeneratedSql = "SELECT EmailTo, FirstName, LastName, COUNT(*) as emails_received FROM `PrecisionEmail.EmailOutbox` GROUP BY EmailTo, FirstName, LastName ORDER BY emails_received DESC LIMIT 50";
                    response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recipients with email counts" };
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in rule-based processing");
                return false;
            }
        }

        private bool TryGenericRuleBasedProcessing(string query, NaturalLanguageQueryResponseDto response)
        {
            // Generic fallback queries
            response.Intent = "campaigns";
            response.GeneratedSql = "SELECT StrategyName, COUNT(*) as emails_sent, MAX(DateCreated) as last_sent FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName ORDER BY last_sent DESC LIMIT 20";
            response.Parameters = new Dictionary<string, object> { ["explanation"] = "Shows recent campaigns (generic query processing)" };
            return true;
        }        public async Task<QueryIntent> ClassifyQueryAsync(string query)
        {
            var response = await ProcessQueryAsync(query, null, false);
            
            var intent = new QueryIntent
            {
                QueryType = response.Intent ?? "campaigns",
                Action = "get",
                Entities = response.Parameters ?? new Dictionary<string, object>()
            };

            return intent;
        }        public Task<bool> ValidateQueryAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length > 1000)
                    return Task.FromResult(false);

                // Basic validation for email campaign related terms
                var queryLower = query.ToLowerInvariant();
                var validTerms = new[] { "campaign", "email", "recipient", "bounce", "click", "open", "send", "deliver", "strategy", "template", "list" };
                
                return Task.FromResult(validTerms.Any(term => queryLower.Contains(term)));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private void ParseLLMResponse(string responseText, NaturalLanguageQueryResponseDto response)
        {
            try
            {
                _logger.LogDebug("Parsing LLM response: {Response}", responseText.Substring(0, Math.Min(500, responseText.Length)));

                // Extract intent
                var intentMatch = System.Text.RegularExpressions.Regex.Match(
                    responseText, 
                    @"INTENT:\s*([^\n\r]+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (intentMatch.Success)
                {
                    response.Intent = intentMatch.Groups[1].Value.Trim().ToLowerInvariant();
                    _logger.LogDebug("Extracted intent: {Intent}", response.Intent);
                }

                // Extract SQL - more robust pattern to handle multi-line SQL
                var sqlMatch = System.Text.RegularExpressions.Regex.Match(
                    responseText, 
                    @"SQL:\s*(SELECT.*?)(?=EXPLANATION:|$)", 
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (sqlMatch.Success)
                {
                    response.GeneratedSql = sqlMatch.Groups[1].Value.Trim();
                    _logger.LogDebug("Extracted SQL: {HasSql}", !string.IsNullOrEmpty(response.GeneratedSql));
                }

                // Extract explanation as parameters
                var explanationMatch = System.Text.RegularExpressions.Regex.Match(
                    responseText, 
                    @"EXPLANATION:\s*([^\n\r]+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (explanationMatch.Success)
                {
                    response.Parameters = new Dictionary<string, object>
                    {
                        ["explanation"] = explanationMatch.Groups[1].Value.Trim()
                    };
                }

                // Fallback: if no structured response, try to identify SQL in the text
                if (string.IsNullOrEmpty(response.GeneratedSql))
                {
                    var selectMatch = System.Text.RegularExpressions.Regex.Match(
                        responseText, 
                        @"(SELECT\s+.*?(?:FROM|$).*?)(?:\n|$)", 
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (selectMatch.Success)
                    {
                        response.GeneratedSql = selectMatch.Groups[1].Value.Trim();
                        response.Intent = response.Intent ?? "campaigns"; // Default intent
                        _logger.LogDebug("Fallback SQL extraction successful");
                    }
                }

                // Final fallback for intent
                if (string.IsNullOrEmpty(response.Intent))
                {
                    response.Intent = "campaigns"; // Default intent
                    _logger.LogDebug("Using default intent: campaigns");
                }

                _logger.LogDebug("Parsing complete - Intent: {Intent}, HasSQL: {HasSql}", response.Intent, !string.IsNullOrEmpty(response.GeneratedSql));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing LLM response, using fallback");
                response.Intent = "campaigns";
                response.GeneratedSql = "";
            }
        }

        public async Task<QueryIntent> ExtractIntentAsync(string query)
        {
            return await ClassifyQueryAsync(query);
        }

        public async Task<string> GenerateSqlAsync(QueryIntent intent)
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
                $"Generate a BigQuery SQL query for: {intent.QueryType}. Only return the SQL query.",
                contextPrompt,
                false);

            return response.GeneratedSql ?? "SELECT StrategyName, COUNT(*) as emails_sent FROM `PrecisionEmail.EmailOutbox` GROUP BY StrategyName LIMIT 10";
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                await InitializeAsync();
                return true; // Always available due to rule-based fallback
            }
            catch
            {
                return true; // Still available with rule-based processing
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
                ["TimeoutSeconds"] = _options.TimeoutSeconds,
                ["HasRuleBasedFallback"] = true
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
        }

        public void Dispose()
        {
            _executor = null; // InteractiveExecutor doesn't implement IDisposable in this version
            _context?.Dispose();
            _model?.Dispose();
            _initializationLock?.Dispose();
        }
    }
}
