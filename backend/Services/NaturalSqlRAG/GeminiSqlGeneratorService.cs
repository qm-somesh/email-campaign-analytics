using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EmailCampaignReporting.API.Configuration;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{
    /// <summary>
    /// Service for calling Gemini Flash to generate SQL queries from prompt/context.
    /// </summary>
    public class GeminiSqlGeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiSqlGeneratorService> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl;

        public GeminiSqlGeneratorService(HttpClient httpClient, ILogger<GeminiSqlGeneratorService> logger, IOptions<EmailCampaignReporting.API.Configuration.GeminiOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = options.Value.ApiKey;
            _modelName = options.Value.ModelName;
            _baseUrl = $"https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent";
        }

        public async Task<string> GenerateSqlAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1f,
                    maxOutputTokens = 512
                }
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {responseContent}");
            }
            // Parse Gemini response (expecting plain SQL or code block)
            var sql = ExtractSqlFromResponse(responseContent);
            return sql;
        }

        private string ExtractSqlFromResponse(string response)
        {
            // Gemini Flash may return SQL in a code block or as plain text
            if (string.IsNullOrWhiteSpace(response)) return string.Empty;
            // Try to extract SQL from code block
            var match = Regex.Match(response, @"```sql(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            // Fallback: return as-is
            return response.Trim();
        }
    }
}
