using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services.LLM.Providers
{
    /// <summary>
    /// Google Gemini 1.5 Flash LLM provider implementation using REST API
    /// </summary>
    public class GeminiLLMProvider : ILLMProvider, IDisposable
    {
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiLLMProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _rateLimitSemaphore;

        public GeminiLLMProvider(
            IOptions<LLMProviderOptions> options,
            ILogger<GeminiLLMProvider> logger,
            HttpClient httpClient)
        {
            _options = options.Value.Gemini;
            _logger = logger;
            _httpClient = httpClient;
            _baseUrl = $"https://generativelanguage.googleapis.com/v1/models/{_options.ModelName}:generateContent";
            _rateLimitSemaphore = new SemaphoreSlim(1, 1); // Rate limiting
        }

        public string ProviderName => "GEMINI";

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

                _logger.LogInformation("Extracting filters using Gemini from query: {Query}", query);

                await _rateLimitSemaphore.WaitAsync();

                try
                {
                    // Build enhanced prompt with context if available
                    var prompt = BuildFilterExtractionPrompt(query, context);
                    
                    // Call Gemini API
                    var llmResponse = await CallGeminiApiAsync(prompt);
                    result.RawLLMResponse = llmResponse;

                    // Parse the JSON response
                    var parseResult = ParseFilterResponse(llmResponse);
                    result.Filters = parseResult.filters;
                    result.Success = parseResult.success;
                    result.Error = parseResult.error;
                    result.Explanation = parseResult.explanation;
                    result.ExtractedParameters = parseResult.extractedParameters;
                    result.Confidence = parseResult.confidence;

                    stopwatch.Stop();
                    result.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

                    _logger.LogInformation(
                        "Gemini filter extraction completed. Success: {Success}, Time: {TimeMs}ms, Confidence: {Confidence}",
                        result.Success, result.ProcessingTimeMs, result.Confidence);

                    return result;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Error = $"Error during filter extraction: {ex.Message}";
                
                _logger.LogError(ex, "Error extracting filters with Gemini from query: {Query}", query);
                return result;
            }
        }        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_options.ApiKey))
                {
                    _logger.LogWarning("Gemini API key is not configured");
                    return false;
                }

                // Simple availability check with a basic prompt
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = "Hello, respond with 'Hi'" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1f,
                        maxOutputTokens = 10
                    }
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{_baseUrl}?key={_options.ApiKey}";
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.PostAsync(url, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogInformation("Gemini API availability check successful");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogWarning("Gemini API availability check failed: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini provider availability check failed");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetModelInfoAsync()
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                ["provider"] = ProviderName,
                ["model"] = _options.ModelName,
                ["maxTokens"] = _options.MaxTokens,
                ["temperature"] = _options.Temperature,
                ["topP"] = _options.TopP,
                ["apiVersion"] = "v1"
            });
        }

        public Dictionary<string, object> GetProviderConfig()
        {
            return new Dictionary<string, object>
            {
                ["provider"] = ProviderName,
                ["model"] = _options.ModelName,
                ["maxTokens"] = _options.MaxTokens,
                ["temperature"] = _options.Temperature,
                ["topP"] = _options.TopP,
                ["apiKeyConfigured"] = !string.IsNullOrEmpty(_options.ApiKey)
            };
        }

        private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = _options.Temperature,
                    topP = _options.TopP,
                    maxOutputTokens = _options.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"{_baseUrl}?key={_options.ApiKey}";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {errorContent}");
            }            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Gemini API Response received. Length: {Length} chars", responseContent.Length);
            _logger.LogDebug("Gemini API Response Content: {Response}", responseContent);
              try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, options);

                if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Length > 0)
                {
                    var candidate = geminiResponse.Candidates.FirstOrDefault();
                    if (candidate?.Content?.Parts != null && candidate.Content.Parts.Length > 0)
                    {
                        var part = candidate.Content.Parts.FirstOrDefault();
                        if (!string.IsNullOrEmpty(part?.Text))
                        {
                            var textResponse = part.Text;
                            _logger.LogInformation("Successfully extracted text from Gemini response. Length: {Length} chars", textResponse.Length);
                            return textResponse;
                        }
                    }
                }

                _logger.LogWarning("Gemini response structure invalid. Candidates={CandidatesCount}, Response={ResponseJson}", 
                    geminiResponse?.Candidates?.Length ?? 0, responseContent);
                throw new InvalidOperationException("Gemini response did not contain valid text content");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Gemini API response: {Response}", responseContent);
                throw new InvalidOperationException($"Invalid JSON response from Gemini API: {ex.Message}");
            }
        }        
        private string BuildFilterExtractionPrompt(string query, string? context)
        {
            var systemPrompt = """
                You are an expert at extracting email campaign filter parameters from natural language queries.
                
                Extract filter parameters from the user's query and return them as valid JSON.
                
                Return ONLY the JSON object with this exact structure (no markdown, no code blocks, no additional text):
                {
                  "minOpenedCount": null,
                  "minClickedCount": null,
                  "minClickRatePercentage": null,
                  "maxClickRatePercentage": null,
                  "minOpenRatePercentage": null,
                  "maxOpenRatePercentage": null,
                  "minDeliveryRatePercentage": null,
                  "maxDeliveryRatePercentage": null,
                  "minBounceRatePercentage": null,
                  "maxBounceRatePercentage": null,
                  "strategyName": null,
                  "firstEmailSentFrom": null,
                  "firstEmailSentTo": null,
                  "minTotalEmails": null,
                  "maxTotalEmails": null,
                  "minDeliveredCount": null,
                  "sortBy": "FirstEmailSentDate",
                  "sortDirection": "desc",
                  "explanation": "Brief explanation of extracted filters"
                }
                
                Rules:
                - Only set fields that are explicitly mentioned or clearly implied in the query
                - Use null for unspecified fields
                - For date ranges, use ISO format: "2024-01-01T00:00:00Z"
                - For strategy names, extract exact text mentioned
                - Return ONLY valid JSON - no markdown formatting, no code blocks, no explanatory text
                - Start your response directly with { and end with }
                
                Example valid response:
                {"minOpenRatePercentage": 10, "strategyName": "Newsletter", "explanation": "Campaigns with open rate above 10% for Newsletter strategy"}
                """;

            var fullPrompt = systemPrompt;
            
            if (!string.IsNullOrEmpty(context))
            {
                fullPrompt += $"\n\nAdditional Context:\n{context}";
            }
            
            fullPrompt += $"\n\nUser Query: {query}\n\nJSON Response:";
            
            return fullPrompt;
        }private (EmailTriggerReportFilterDto filters, bool success, string? error, string explanation, List<string> extractedParameters, decimal confidence) ParseFilterResponse(string response)
        {
            try
            {
                _logger.LogInformation("Parsing Gemini response: {Response}", response);
                
                // Extract JSON from response using multiple strategies
                var jsonStr = ExtractJsonFromResponse(response);
                if (string.IsNullOrEmpty(jsonStr))
                {
                    _logger.LogWarning("No valid JSON found in Gemini response");
                    return (new EmailTriggerReportFilterDto(), false, "No JSON found in response", "", new List<string>(), 0.0m);
                }

                _logger.LogInformation("Extracted JSON: {Json}", jsonStr);

                var jsonDoc = JsonDocument.Parse(jsonStr);
                var root = jsonDoc.RootElement;

                var filters = new EmailTriggerReportFilterDto();
                var extractedParams = new List<string>();

                // Extract all possible filter fields with detailed logging
                if (root.TryGetProperty("minOpenedCount", out var minOpened) && minOpened.ValueKind != JsonValueKind.Null)
                {
                    filters.MinOpenedCount = minOpened.GetInt32();
                    extractedParams.Add("MinOpenedCount");
                }

                if (root.TryGetProperty("minClickedCount", out var minClicked) && minClicked.ValueKind != JsonValueKind.Null)
                {
                    filters.MinClickedCount = minClicked.GetInt32();
                    extractedParams.Add("MinClickedCount");
                }

                if (root.TryGetProperty("minClickRatePercentage", out var minClickRate) && minClickRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MinClickRatePercentage = minClickRate.GetDecimal();
                    extractedParams.Add("MinClickRatePercentage");
                }

                if (root.TryGetProperty("maxClickRatePercentage", out var maxClickRate) && maxClickRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MaxClickRatePercentage = maxClickRate.GetDecimal();
                    extractedParams.Add("MaxClickRatePercentage");
                }

                if (root.TryGetProperty("minOpenRatePercentage", out var minOpenRate) && minOpenRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MinOpenRatePercentage = minOpenRate.GetDecimal();
                    extractedParams.Add("MinOpenRatePercentage");
                }

                if (root.TryGetProperty("maxOpenRatePercentage", out var maxOpenRate) && maxOpenRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MaxOpenRatePercentage = maxOpenRate.GetDecimal();
                    extractedParams.Add("MaxOpenRatePercentage");
                }

                if (root.TryGetProperty("minDeliveryRatePercentage", out var minDeliveryRate) && minDeliveryRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MinDeliveryRatePercentage = minDeliveryRate.GetDecimal();
                    extractedParams.Add("MinDeliveryRatePercentage");
                }

                if (root.TryGetProperty("maxDeliveryRatePercentage", out var maxDeliveryRate) && maxDeliveryRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MaxDeliveryRatePercentage = maxDeliveryRate.GetDecimal();
                    extractedParams.Add("MaxDeliveryRatePercentage");
                }

                if (root.TryGetProperty("minBounceRatePercentage", out var minBounceRate) && minBounceRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MinBounceRatePercentage = minBounceRate.GetDecimal();
                    extractedParams.Add("MinBounceRatePercentage");
                }

                if (root.TryGetProperty("maxBounceRatePercentage", out var maxBounceRate) && maxBounceRate.ValueKind != JsonValueKind.Null)
                {
                    filters.MaxBounceRatePercentage = maxBounceRate.GetDecimal();
                    extractedParams.Add("MaxBounceRatePercentage");
                }

                if (root.TryGetProperty("strategyName", out var strategyName) && strategyName.ValueKind != JsonValueKind.Null)
                {
                    filters.StrategyName = strategyName.GetString();
                    extractedParams.Add("StrategyName");
                }

                if (root.TryGetProperty("firstEmailSentFrom", out var firstEmailFrom) && firstEmailFrom.ValueKind != JsonValueKind.Null)
                {
                    if (DateTime.TryParse(firstEmailFrom.GetString(), out var fromDate))
                    {
                        filters.FirstEmailSentFrom = fromDate;
                        extractedParams.Add("FirstEmailSentFrom");
                    }
                }

                if (root.TryGetProperty("firstEmailSentTo", out var firstEmailTo) && firstEmailTo.ValueKind != JsonValueKind.Null)
                {
                    if (DateTime.TryParse(firstEmailTo.GetString(), out var toDate))
                    {
                        filters.FirstEmailSentTo = toDate;
                        extractedParams.Add("FirstEmailSentTo");
                    }
                }

                if (root.TryGetProperty("minTotalEmails", out var minTotal) && minTotal.ValueKind != JsonValueKind.Null)
                {
                    filters.MinTotalEmails = minTotal.GetInt32();
                    extractedParams.Add("MinTotalEmails");
                }

                if (root.TryGetProperty("maxTotalEmails", out var maxTotal) && maxTotal.ValueKind != JsonValueKind.Null)
                {
                    filters.MaxTotalEmails = maxTotal.GetInt32();
                    extractedParams.Add("MaxTotalEmails");
                }

                if (root.TryGetProperty("minDeliveredCount", out var minDelivered) && minDelivered.ValueKind != JsonValueKind.Null)
                {
                    filters.MinDeliveredCount = minDelivered.GetInt32();
                    extractedParams.Add("MinDeliveredCount");
                }

                if (root.TryGetProperty("sortBy", out var sortBy) && sortBy.ValueKind != JsonValueKind.Null)
                {
                    filters.SortBy = sortBy.GetString();
                    extractedParams.Add("SortBy");
                }

                if (root.TryGetProperty("sortDirection", out var sortDirection) && sortDirection.ValueKind != JsonValueKind.Null)
                {
                    filters.SortDirection = sortDirection.GetString();
                    extractedParams.Add("SortDirection");
                }
                
                var explanation = root.TryGetProperty("explanation", out var explanationElement) 
                    ? explanationElement.GetString() ?? "Filters extracted successfully" 
                    : "Filters extracted successfully";

                var confidence = extractedParams.Count > 0 ? 0.9m : 0.5m;

                _logger.LogInformation("Successfully parsed {Count} filter parameters: {Parameters}", 
                    extractedParams.Count, string.Join(", ", extractedParams));

                return (filters, true, null, explanation, extractedParams, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini filter response: {Response}", response);
                return (new EmailTriggerReportFilterDto(), false, $"JSON parsing error: {ex.Message}", "", new List<string>(), 0.0m);
            }
        }

        private string? ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Strategy 1: Try to parse the entire response as JSON (if it's already clean JSON)
            try
            {
                using var doc = JsonDocument.Parse(response);
                return response;
            }
            catch
            {
                // Not clean JSON, continue with extraction strategies
            }

            // Strategy 2: Look for JSON in code blocks (```json ... ``` or ``` ... ```)
            var codeBlockPatterns = new[]
            {
                @"```json\s*(.*?)\s*```",  // ```json ... ```
                @"```\s*(.*?)\s*```",      // ``` ... ```
                @"`(.*?)`"                 // ` ... `
            };

            foreach (var pattern in codeBlockPatterns)
            {
                var match = Regex.Match(response, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var content = match.Groups[1].Value.Trim();
                    if (IsValidJson(content))
                    {
                        _logger.LogInformation("Found JSON in code block using pattern: {Pattern}", pattern);
                        return content;
                    }
                }
            }

            // Strategy 3: Look for JSON objects anywhere in the response
            var jsonObjectPattern = @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}";
            var matches = Regex.Matches(response, jsonObjectPattern, RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                var content = match.Value.Trim();
                if (IsValidJson(content))
                {
                    _logger.LogInformation("Found JSON object in response");
                    return content;
                }
            }

            // Strategy 4: Look for nested JSON with proper bracket matching
            var bracketPattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
            var bracketMatch = Regex.Match(response, bracketPattern, RegexOptions.Singleline);
            if (bracketMatch.Success)
            {
                var content = bracketMatch.Value.Trim();
                if (IsValidJson(content))
                {
                    _logger.LogInformation("Found JSON using bracket matching");
                    return content;
                }
            }

            _logger.LogWarning("No valid JSON found in response using any extraction strategy");
            return null;
        }

        private bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _rateLimitSemaphore?.Dispose();
        }

        // Response DTOs for Gemini API
        private class GeminiResponse
        {
            public GeminiCandidate[]? Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiContent? Content { get; set; }
        }

        private class GeminiContent
        {
            public GeminiPart[]? Parts { get; set; }
        }

        private class GeminiPart
        {
            public string? Text { get; set; }
        }
    }
}
