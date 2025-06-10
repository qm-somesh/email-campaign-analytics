using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Extensions.Options;
using LLama;
using LLama.Common;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Service that uses LLM model exclusively to extract EmailTriggerReportFilterDto filters from natural language queries
    /// </summary>
    public class EmailTriggerFilterService : IEmailTriggerFilterService, IDisposable
    {
        private readonly LLMOptions _options;
        private readonly ILogger<EmailTriggerFilterService> _logger;
        private LLamaWeights? _model;
        private LLamaContext? _context;
        private InteractiveExecutor? _executor;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public EmailTriggerFilterService(IOptions<LLMOptions> options, ILogger<EmailTriggerFilterService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Extract filter parameters from natural language query using LLM exclusively
        /// </summary>
        public async Task<EmailTriggerFilterExtractionResult> ExtractFiltersAsync(string query, string? context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new EmailTriggerFilterExtractionResult();

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    result.Success = false;
                    result.Error = "Query cannot be empty";
                    return result;
                }

                _logger.LogInformation("Extracting filters from query: {Query}", query);

                // Initialize LLM if needed
                await InitializeAsync();

                if (_executor == null)
                {
                    result.Success = false;
                    result.Error = "LLM service not available";
                    return result;
                }

                // Build the specialized prompt for filter extraction
                var systemPrompt = BuildFilterExtractionPrompt(context);
                var fullPrompt = $"{systemPrompt}\n\nUser Query: {query}\n\nExtracted Filters (JSON):";

                _logger.LogInformation("Processing filter extraction with LLM...");

                // Configure inference parameters for focused responses
                var inferenceParams = new InferenceParams
                {
                    Temperature = 0.1f, // Low temperature for more deterministic results
                    AntiPrompts = new[] { "\n\n", "User:", "Query:", "Human:", "Assistant:" },
                    MaxTokens = 512, // Focused response
                    TopP = 0.9f,
                    TopK = 40
                };

                var responseText = "";
                await foreach (var token in _executor.InferAsync(fullPrompt, inferenceParams))
                {
                    responseText += token;
                    if (responseText.Length > 2048) // Prevent runaway generation
                        break;
                }

                result.RawLLMResponse = responseText.Trim();
                _logger.LogDebug("LLM Response: {Response}", result.RawLLMResponse);

                // Parse the LLM response to extract filters
                await ParseFilterResponse(result.RawLLMResponse, result);

                _logger.LogInformation("Filter extraction completed successfully. Extracted {Count} parameters", 
                    result.ExtractedParameters.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting filters from query: {Query}", query);
                result.Success = false;
                result.Error = $"Error processing query: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Build the specialized prompt for filter extraction
        /// </summary>
        private string BuildFilterExtractionPrompt(string? context = null)
        {
            var prompt = @"You are an expert filter extraction system for email campaign reporting. Your task is to analyze natural language queries and extract appropriate filter parameters for EmailTriggerReportFilterDto.

AVAILABLE FILTER PARAMETERS:
- StrategyName: string (campaign name, partial match)
- FirstEmailSentFrom: DateTime (minimum first email sent date)
- FirstEmailSentTo: DateTime (maximum first email sent date)
- MinTotalEmails: int (minimum total emails count)
- MaxTotalEmails: int (maximum total emails count)
- MinDeliveredCount: int (minimum delivered count)
- MinOpenedCount: int (minimum opened count)
- MinClickedCount: int (minimum clicked count)
- MinClickRatePercentage: decimal (minimum click rate %, 0-100)
- MaxClickRatePercentage: decimal (maximum click rate %, 0-100)
- MinOpenRatePercentage: decimal (minimum open rate %, 0-100)
- MaxOpenRatePercentage: decimal (maximum open rate %, 0-100)
- MinDeliveryRatePercentage: decimal (minimum delivery rate %, 0-100)
- MaxDeliveryRatePercentage: decimal (maximum delivery rate %, 0-100)
- MinBounceRatePercentage: decimal (minimum bounce rate %, 0-100)
- MaxBounceRatePercentage: decimal (maximum bounce rate %, 0-100)
- PageNumber: int (page number, default 1)
- PageSize: int (items per page, default 50)
- SortBy: string (sort field)
- SortDirection: string (""asc"" or ""desc"")

EXTRACTION RULES:
1. Extract only relevant filters based on the user's query
2. Use reasonable defaults and interpretations
3. For percentage values, use 0-100 scale
4. For date ranges, interpret relative terms (""last month"", ""this year"", etc.)
5. Set pagination defaults: PageNumber=1, PageSize=50
6. Return ONLY valid JSON format
7. Include ""explanation"" field describing what was extracted

RESPONSE FORMAT:
{
  ""filters"": {
    ""StrategyName"": ""campaign name"",
    ""MinOpenRatePercentage"": 5.0,
    ""PageNumber"": 1,
    ""PageSize"": 50
  },
  ""explanation"": ""Extracted campaign name filter and minimum open rate of 5%"",
  ""confidence"": 0.9,
  ""extractedParameters"": [""StrategyName"", ""MinOpenRatePercentage""]
}

EXAMPLES:

Query: ""Show campaigns with high open rates""
{
  ""filters"": {
    ""MinOpenRatePercentage"": 20.0,
    ""PageNumber"": 1,
    ""PageSize"": 50,
    ""SortBy"": ""MinOpenRatePercentage"",
    ""SortDirection"": ""desc""
  },
  ""explanation"": ""Filtered for campaigns with open rate above 20% (high performance threshold)"",
  ""confidence"": 0.85,
  ""extractedParameters"": [""MinOpenRatePercentage"", ""SortBy"", ""SortDirection""]
}

Query: ""Find Black Friday campaigns from last month with more than 1000 emails""
{
  ""filters"": {
    ""StrategyName"": ""Black Friday"",
    ""MinTotalEmails"": 1000,
    ""FirstEmailSentFrom"": ""2024-11-01T00:00:00Z"",
    ""FirstEmailSentTo"": ""2024-11-30T23:59:59Z"",
    ""PageNumber"": 1,
    ""PageSize"": 50
  },
  ""explanation"": ""Filtered for Black Friday campaigns from November 2024 with minimum 1000 emails sent"",
  ""confidence"": 0.95,
  ""extractedParameters"": [""StrategyName"", ""MinTotalEmails"", ""FirstEmailSentFrom"", ""FirstEmailSentTo""]
}";

            if (!string.IsNullOrEmpty(context))
            {
                prompt += $"\n\nADDITIONAL CONTEXT:\n{context}";
            }

            return prompt;
        }

        /// <summary>
        /// Parse the LLM response to extract filter parameters
        /// </summary>
        private async Task ParseFilterResponse(string response, EmailTriggerFilterExtractionResult result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Success = false;
                    result.Error = "Empty response from LLM";
                    return;
                }

                // Extract JSON from response
                var jsonContent = ExtractJsonFromResponse(response);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    result.Success = false;
                    result.Error = "No valid JSON found in LLM response";
                    return;
                }

                _logger.LogDebug("Extracted JSON: {Json}", jsonContent);

                // Parse the JSON response
                var jsonDocument = JsonDocument.Parse(jsonContent);
                var root = jsonDocument.RootElement;

                // Extract filters
                if (root.TryGetProperty("filters", out var filtersElement))
                {
                    await MapJsonToFilters(filtersElement, result.Filters, result);
                }

                // Extract explanation
                if (root.TryGetProperty("explanation", out var explanationElement))
                {
                    result.Explanation = explanationElement.GetString() ?? "";
                }

                // Extract confidence
                if (root.TryGetProperty("confidence", out var confidenceElement))
                {
                    if (confidenceElement.TryGetDecimal(out var confidence))
                    {
                        result.Confidence = confidence;
                    }
                }

                // Extract parameters list
                if (root.TryGetProperty("extractedParameters", out var parametersElement) && 
                    parametersElement.ValueKind == JsonValueKind.Array)
                {
                    result.ExtractedParameters = parametersElement.EnumerateArray()
                        .Select(x => x.GetString() ?? "")
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                }

                result.Success = true;

                _logger.LogInformation("Successfully parsed filter response. Confidence: {Confidence}, Parameters: {Parameters}", 
                    result.Confidence, string.Join(", ", result.ExtractedParameters));

            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error in filter response");
                result.Success = false;
                result.Error = $"Invalid JSON in response: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing filter response");
                result.Success = false;
                result.Error = $"Error parsing response: {ex.Message}";
            }
        }

        /// <summary>
        /// Extract JSON content from LLM response
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            // Try to find JSON object in the response
            var jsonMatch = Regex.Match(response, @"\{.*\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (jsonMatch.Success)
            {
                return jsonMatch.Value;
            }

            // If no JSON found, try to extract content between certain markers
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var jsonLines = new List<string>();
            bool inJson = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("{"))
                {
                    inJson = true;
                    jsonLines.Add(trimmed);
                }
                else if (inJson)
                {
                    jsonLines.Add(trimmed);
                    if (trimmed.EndsWith("}"))
                    {
                        break;
                    }
                }
            }

            return jsonLines.Count > 0 ? string.Join("", jsonLines) : response.Trim();
        }

        /// <summary>
        /// Map JSON properties to EmailTriggerReportFilterDto
        /// </summary>
        private async Task MapJsonToFilters(JsonElement filtersElement, EmailTriggerReportFilterDto filters, EmailTriggerFilterExtractionResult result)
        {
            foreach (var property in filtersElement.EnumerateObject())
            {
                try
                {
                    switch (property.Name)
                    {
                        case "StrategyName":
                            if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                filters.StrategyName = property.Value.GetString();
                                result.ExtractedParameters.Add("StrategyName");
                            }
                            break;

                        case "FirstEmailSentFrom":
                            if (property.Value.ValueKind == JsonValueKind.String && 
                                DateTime.TryParse(property.Value.GetString(), out var fromDate))
                            {
                                filters.FirstEmailSentFrom = fromDate;
                                result.ExtractedParameters.Add("FirstEmailSentFrom");
                            }
                            break;

                        case "FirstEmailSentTo":
                            if (property.Value.ValueKind == JsonValueKind.String && 
                                DateTime.TryParse(property.Value.GetString(), out var toDate))
                            {
                                filters.FirstEmailSentTo = toDate;
                                result.ExtractedParameters.Add("FirstEmailSentTo");
                            }
                            break;

                        case "MinTotalEmails":
                            if (property.Value.TryGetInt32(out var minTotalEmails))
                            {
                                filters.MinTotalEmails = minTotalEmails;
                                result.ExtractedParameters.Add("MinTotalEmails");
                            }
                            break;

                        case "MaxTotalEmails":
                            if (property.Value.TryGetInt32(out var maxTotalEmails))
                            {
                                filters.MaxTotalEmails = maxTotalEmails;
                                result.ExtractedParameters.Add("MaxTotalEmails");
                            }
                            break;

                        case "MinDeliveredCount":
                            if (property.Value.TryGetInt32(out var minDeliveredCount))
                            {
                                filters.MinDeliveredCount = minDeliveredCount;
                                result.ExtractedParameters.Add("MinDeliveredCount");
                            }
                            break;

                        case "MinOpenedCount":
                            if (property.Value.TryGetInt32(out var minOpenedCount))
                            {
                                filters.MinOpenedCount = minOpenedCount;
                                result.ExtractedParameters.Add("MinOpenedCount");
                            }
                            break;

                        case "MinClickedCount":
                            if (property.Value.TryGetInt32(out var minClickedCount))
                            {
                                filters.MinClickedCount = minClickedCount;
                                result.ExtractedParameters.Add("MinClickedCount");
                            }
                            break;

                        case "MinClickRatePercentage":
                            if (property.Value.TryGetDecimal(out var minClickRate))
                            {
                                filters.MinClickRatePercentage = minClickRate;
                                result.ExtractedParameters.Add("MinClickRatePercentage");
                            }
                            break;

                        case "MaxClickRatePercentage":
                            if (property.Value.TryGetDecimal(out var maxClickRate))
                            {
                                filters.MaxClickRatePercentage = maxClickRate;
                                result.ExtractedParameters.Add("MaxClickRatePercentage");
                            }
                            break;

                        case "MinOpenRatePercentage":
                            if (property.Value.TryGetDecimal(out var minOpenRate))
                            {
                                filters.MinOpenRatePercentage = minOpenRate;
                                result.ExtractedParameters.Add("MinOpenRatePercentage");
                            }
                            break;

                        case "MaxOpenRatePercentage":
                            if (property.Value.TryGetDecimal(out var maxOpenRate))
                            {
                                filters.MaxOpenRatePercentage = maxOpenRate;
                                result.ExtractedParameters.Add("MaxOpenRatePercentage");
                            }
                            break;

                        case "MinDeliveryRatePercentage":
                            if (property.Value.TryGetDecimal(out var minDeliveryRate))
                            {
                                filters.MinDeliveryRatePercentage = minDeliveryRate;
                                result.ExtractedParameters.Add("MinDeliveryRatePercentage");
                            }
                            break;

                        case "MaxDeliveryRatePercentage":
                            if (property.Value.TryGetDecimal(out var maxDeliveryRate))
                            {
                                filters.MaxDeliveryRatePercentage = maxDeliveryRate;
                                result.ExtractedParameters.Add("MaxDeliveryRatePercentage");
                            }
                            break;

                        case "MinBounceRatePercentage":
                            if (property.Value.TryGetDecimal(out var minBounceRate))
                            {
                                filters.MinBounceRatePercentage = minBounceRate;
                                result.ExtractedParameters.Add("MinBounceRatePercentage");
                            }
                            break;

                        case "MaxBounceRatePercentage":
                            if (property.Value.TryGetDecimal(out var maxBounceRate))
                            {
                                filters.MaxBounceRatePercentage = maxBounceRate;
                                result.ExtractedParameters.Add("MaxBounceRatePercentage");
                            }
                            break;

                        case "PageNumber":
                            if (property.Value.TryGetInt32(out var pageNumber) && pageNumber > 0)
                            {
                                filters.PageNumber = pageNumber;
                                result.ExtractedParameters.Add("PageNumber");
                            }
                            break;

                        case "PageSize":
                            if (property.Value.TryGetInt32(out var pageSize) && pageSize > 0)
                            {
                                filters.PageSize = pageSize;
                                result.ExtractedParameters.Add("PageSize");
                            }
                            break;

                        case "SortBy":
                            if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                filters.SortBy = property.Value.GetString();
                                result.ExtractedParameters.Add("SortBy");
                            }
                            break;

                        case "SortDirection":
                            if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                var sortDirection = property.Value.GetString()?.ToLowerInvariant();
                                if (sortDirection == "asc" || sortDirection == "desc")
                                {
                                    filters.SortDirection = sortDirection;
                                    result.ExtractedParameters.Add("SortDirection");
                                }
                            }
                            break;

                        default:
                            _logger.LogWarning("Unknown filter property: {PropertyName}", property.Name);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error mapping property {PropertyName}", property.Name);
                }
            }

            // Set defaults if not provided
            if (filters.PageNumber <= 0) filters.PageNumber = 1;
            if (filters.PageSize <= 0) filters.PageSize = 50;
            if (string.IsNullOrEmpty(filters.SortBy)) filters.SortBy = "StrategyName";
            if (string.IsNullOrEmpty(filters.SortDirection)) filters.SortDirection = "asc";

            await Task.CompletedTask;
        }

        /// <summary>
        /// Initialize the LLM model and context
        /// </summary>
        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                _logger.LogInformation("Initializing EmailTriggerFilterService LLM model from {ModelPath}", _options.ModelPath);

                if (string.IsNullOrEmpty(_options.ModelPath) || !File.Exists(_options.ModelPath))
                {
                    throw new FileNotFoundException($"Model file not found: {_options.ModelPath}");
                }

                // Use timeout for model loading
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                // Configure model parameters for filter extraction
                var parameters = new ModelParams(_options.ModelPath)
                {
                    ContextSize = (uint)Math.Min(_options.ContextSize, 2048), // Smaller context for filter extraction
                    GpuLayerCount = 0, // CPU only for stability
                    UseMemorymap = true,
                    UseMemoryLock = false
                };

                _logger.LogInformation("Loading model for filter extraction with ContextSize={ContextSize}", parameters.ContextSize);

                // Load model with timeout
                await Task.Run(() =>
                {
                    try
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        _model = LLamaWeights.LoadFromFile(parameters);

                        cts.Token.ThrowIfCancellationRequested();
                        _context = _model.CreateContext(parameters);

                        cts.Token.ThrowIfCancellationRequested();
                        _executor = new InteractiveExecutor(_context);

                        _logger.LogInformation("EmailTriggerFilterService LLM initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during model loading: {ErrorType}", ex.GetType().Name);
                        throw;
                    }
                }, cts.Token);

                _isInitialized = true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM model initialization timed out after {Timeout} seconds", _options.TimeoutSeconds);
                throw new TimeoutException($"Model initialization timed out after {_options.TimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize EmailTriggerFilterService LLM model");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
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
                ["ServiceType"] = "EmailTriggerFilterService",
                ["IsInitialized"] = _isInitialized,
                ["ModelPath"] = _options.ModelPath ?? "Not set",
                ["ContextSize"] = _options.ContextSize,
                ["MaxTokens"] = _options.MaxTokens,
                ["Temperature"] = _options.Temperature,
                ["TimeoutSeconds"] = _options.TimeoutSeconds,
                ["Purpose"] = "Email Trigger Filter Extraction"
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
            _executor = null;
            _context?.Dispose();
            _model?.Dispose();
            _initializationLock?.Dispose();
        }
    }
}
