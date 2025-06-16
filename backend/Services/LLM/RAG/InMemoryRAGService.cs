using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace EmailCampaignReporting.API.Services.LLM.RAG
{
    /// <summary>
    /// In-memory RAG service implementation for email campaign knowledge
    /// </summary>
    public class InMemoryRAGService : IRAGService
    {
        private readonly RAGOptions _options;
        private readonly ILogger<InMemoryRAGService> _logger;
        private readonly ConcurrentDictionary<string, KnowledgeItem> _knowledgeBase;
        private readonly ConcurrentDictionary<string, float[]> _embeddings;
        private bool _isInitialized = false;

        public InMemoryRAGService(
            IOptions<LLMProviderOptions> options,
            ILogger<InMemoryRAGService> logger)
        {
            _options = options.Value.RAG;
            _logger = logger;
            _knowledgeBase = new ConcurrentDictionary<string, KnowledgeItem>();
            _embeddings = new ConcurrentDictionary<string, float[]>();
            
            _ = Task.Run(InitializeKnowledgeBaseAsync);
        }

        public async Task<RAGEnhancedQuery> EnhanceQueryAsync(string query, int maxContextItems = 5)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (!_isInitialized)
                {
                    await InitializeKnowledgeBaseAsync();
                }

                _logger.LogInformation("Enhancing query with RAG: {Query}", query);

                // For this basic implementation, we'll use simple keyword matching
                // In a production environment, you would use proper embeddings
                var retrievedContext = await RetrieveRelevantContextAsync(query, maxContextItems);
                
                var enhancedQuery = BuildEnhancedQuery(query, retrievedContext);
                
                stopwatch.Stop();

                return new RAGEnhancedQuery
                {
                    OriginalQuery = query,
                    EnhancedQuery = enhancedQuery,
                    RetrievedContext = retrievedContext,
                    ConfidenceScore = CalculateConfidenceScore(retrievedContext),
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing query with RAG");
                stopwatch.Stop();
                
                return new RAGEnhancedQuery
                {
                    OriginalQuery = query,
                    EnhancedQuery = query, // Fallback to original query
                    RetrievedContext = new List<ContextItem>(),
                    ConfidenceScore = 0.0,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<bool> AddKnowledgeAsync(KnowledgeItem knowledge)
        {
            try
            {
                if (string.IsNullOrEmpty(knowledge.Id))
                {
                    knowledge.Id = Guid.NewGuid().ToString();
                }

                _knowledgeBase.AddOrUpdate(knowledge.Id, knowledge, (key, old) => knowledge);
                
                // In a real implementation, you would generate embeddings here
                // For now, we'll use a simple placeholder
                _embeddings.AddOrUpdate(knowledge.Id, new float[384], (key, old) => new float[384]);
                
                _logger.LogInformation("Added knowledge item: {Id} - {Title}", knowledge.Id, knowledge.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding knowledge item");
                return false;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return _isInitialized;
        }

        public async Task<Dictionary<string, object>> GetKnowledgeBaseStatsAsync()
        {
            return new Dictionary<string, object>
            {
                ["TotalKnowledgeItems"] = _knowledgeBase.Count,
                ["TotalEmbeddings"] = _embeddings.Count,
                ["IsInitialized"] = _isInitialized,
                ["VectorStoreType"] = _options.VectorStoreType,
                ["MaxRetrievedDocs"] = _options.MaxRetrievedDocs,
                ["SimilarityThreshold"] = _options.SimilarityThreshold,
                ["Categories"] = _knowledgeBase.Values.GroupBy(k => k.Category).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private async Task InitializeKnowledgeBaseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing RAG knowledge base");

                // Add default email campaign knowledge
                await AddDefaultKnowledgeAsync();

                // Load from file if specified
                if (!string.IsNullOrEmpty(_options.KnowledgeBaseDataPath) && File.Exists(_options.KnowledgeBaseDataPath))
                {
                    await LoadKnowledgeFromFileAsync(_options.KnowledgeBaseDataPath);
                }

                _isInitialized = true;
                _logger.LogInformation("RAG knowledge base initialized with {Count} items", _knowledgeBase.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RAG knowledge base");
            }
        }

        private async Task AddDefaultKnowledgeAsync()
        {
            var defaultKnowledge = new[]
            {
                new KnowledgeItem
                {
                    Id = "email-metrics-basics",
                    Title = "Email Campaign Metrics Basics",
                    Content = "Key email metrics include open rate (percentage of emails opened), click rate (percentage of emails clicked), delivery rate (percentage of emails delivered), bounce rate (percentage of emails that bounced), and conversion rate.",
                    Category = "Metrics",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "open-rate-benchmarks",
                    Title = "Open Rate Benchmarks",
                    Content = "Average email open rates typically range from 15-25% across industries. Good open rates are generally considered 20%+, while excellent rates are 30%+.",
                    Category = "Benchmarks",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "click-rate-benchmarks",
                    Title = "Click Rate Benchmarks",
                    Content = "Average email click rates typically range from 2-5% across industries. Good click rates are generally considered 3%+, while excellent rates are 5%+.",
                    Category = "Benchmarks",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "bounce-rate-info",
                    Title = "Bounce Rate Guidelines",
                    Content = "Bounce rates should ideally be below 2%. High bounce rates (5%+) can harm sender reputation. Soft bounces are temporary, hard bounces are permanent delivery failures.",
                    Category = "Deliverability",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "delivery-rate-info",
                    Title = "Delivery Rate Standards",
                    Content = "Good delivery rates should be 95%+ for healthy email lists. Poor delivery rates may indicate list quality issues, authentication problems, or reputation issues.",
                    Category = "Deliverability",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "date-filters",
                    Title = "Date Filtering for Email Campaigns",
                    Content = "When filtering by dates, FirstEmailSentFrom and FirstEmailSentTo refer to the date range when the first email in a campaign was sent. Use YYYY-MM-DD format for date queries.",
                    Category = "Filtering",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "performance-queries",
                    Title = "Common Performance Queries",
                    Content = "High-performing campaigns typically have: open rates >20%, click rates >3%, low bounce rates <2%, and good delivery rates >95%. Use these benchmarks when filtering for top performers.",
                    Category = "Performance",
                    Source = "System"
                }
            };

            foreach (var knowledge in defaultKnowledge)
            {
                await AddKnowledgeAsync(knowledge);
            }
        }

        private async Task LoadKnowledgeFromFileAsync(string filePath)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var knowledgeItems = JsonSerializer.Deserialize<KnowledgeItem[]>(jsonContent);
                
                if (knowledgeItems != null)
                {
                    foreach (var item in knowledgeItems)
                    {
                        await AddKnowledgeAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load knowledge from file: {FilePath}", filePath);
            }
        }

        private async Task<List<ContextItem>> RetrieveRelevantContextAsync(string query, int maxItems)
        {
            // Simple keyword-based retrieval for this basic implementation
            // In production, you would use proper vector similarity search
            
            var queryLower = query.ToLowerInvariant();
            var relevantItems = new List<(KnowledgeItem item, double score)>();

            foreach (var knowledge in _knowledgeBase.Values)
            {
                var score = CalculateSimpleRelevanceScore(queryLower, knowledge);
                if (score >= _options.SimilarityThreshold)
                {
                    relevantItems.Add((knowledge, score));
                }
            }

            return relevantItems
                .OrderByDescending(x => x.score)
                .Take(maxItems)
                .Select(x => new ContextItem
                {
                    Content = x.item.Content,
                    RelevanceScore = x.score,
                    Source = x.item.Source,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Id"] = x.item.Id,
                        ["Title"] = x.item.Title,
                        ["Category"] = x.item.Category,
                        ["CreatedAt"] = x.item.CreatedAt
                    }
                })
                .ToList();
        }

        private double CalculateSimpleRelevanceScore(string queryLower, KnowledgeItem knowledge)
        {
            var contentLower = knowledge.Content.ToLowerInvariant();
            var titleLower = knowledge.Title.ToLowerInvariant();
            
            var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchCount = 0;
            var totalWords = queryWords.Length;

            foreach (var word in queryWords)
            {
                if (contentLower.Contains(word) || titleLower.Contains(word))
                {
                    matchCount++;
                }
            }

            // Simple scoring based on word matches
            var baseScore = totalWords > 0 ? (double)matchCount / totalWords : 0.0;
            
            // Boost score for title matches
            if (titleLower.Contains(queryLower) || queryWords.Any(w => titleLower.Contains(w)))
            {
                baseScore += 0.2;
            }

            return Math.Min(1.0, baseScore);
        }

        private string BuildEnhancedQuery(string originalQuery, List<ContextItem> context)
        {
            if (!context.Any())
            {
                return originalQuery;
            }

            var contextText = string.Join("\n", context.Select(c => $"- {c.Content}"));
            
            return $@"Based on the following context about email campaigns:

{contextText}

User query: {originalQuery}

Please extract filter parameters considering this context.";
        }

        private double CalculateConfidenceScore(List<ContextItem> context)
        {
            if (!context.Any())
                return 0.0;

            // Average relevance score of retrieved context
            return context.Average(c => c.RelevanceScore);
        }
    }
}
