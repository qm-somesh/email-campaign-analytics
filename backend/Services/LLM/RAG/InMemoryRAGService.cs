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
                },
                new KnowledgeItem
                {
                    Id = "rate-percentage-terminology",
                    Title = "Rate and Percentage Filter Terminology",
                    Content = "When users mention 'rate above X%', 'rate over X%', 'rate greater than X%', or 'rate higher than X%', they mean minRate >= X. When they say 'rate below X%', 'rate under X%', or 'rate less than X%', they mean maxRate <= X. For 'rate between X% and Y%', use minRate >= X and maxRate <= Y. Convert percentages to decimals (e.g., 20% = 0.2).",
                    Category = "Query-Patterns",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "campaign-filtering-keywords",
                    Title = "Campaign Filtering Keywords and Synonyms",
                    Content = "Common filtering terms: 'campaigns' = email campaigns, 'show/find/get/display' = retrieve/filter, 'with/having' = filter condition, 'above/over/greater than/higher than/more than' = minimum threshold, 'below/under/less than/lower than' = maximum threshold, 'between' = range filter, 'good/high/excellent performance' = high rates, 'poor/low/bad performance' = low rates.",
                    Category = "Query-Patterns", 
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "email-metrics-synonyms",
                    Title = "Email Metrics and Their Synonyms",
                    Content = "Open rate synonyms: open rate, opened rate, opening rate, opens. Click rate synonyms: click rate, clicked rate, clicking rate, clicks, CTR, click-through rate. Bounce rate synonyms: bounce rate, bounced rate, bounces, hard bounce, soft bounce. Delivery rate synonyms: delivery rate, delivered rate, deliverability, delivery success.",
                    Category = "Metrics",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "performance-qualifiers",
                    Title = "Performance Qualifier Mapping",
                    Content = "Performance qualifiers and their typical thresholds: 'high performance' = open rate >25%, click rate >5%, bounce rate <2%. 'good performance' = open rate >20%, click rate >3%, bounce rate <3%. 'poor performance' = open rate <15%, click rate <2%, bounce rate >5%. 'excellent/outstanding' = top 10% performers. 'average/standard' = industry benchmarks.",
                    Category = "Performance",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "count-and-volume-filters",
                    Title = "Count and Volume Filter Patterns",
                    Content = "Email count filters: 'more than X emails' = minTotalEmails > X, 'delivered count above X' = minDeliveredCount > X, 'opened more than X times' = minOpenedCount > X, 'clicked more than X times' = minClickedCount > X. Volume qualifiers: 'large campaigns' = >10000 emails, 'small campaigns' = <1000 emails, 'medium campaigns' = 1000-10000 emails.",
                    Category = "Filtering",
                    Source = "System"
                },
                new KnowledgeItem
                {
                    Id = "date-and-time-patterns",
                    Title = "Date and Time Filter Patterns",
                    Content = "Date filtering patterns: 'last month/week/year' = relative date range, 'since X date' = firstEmailSentFrom >= X, 'before X date' = firstEmailSentTo <= X, 'during/in X period' = date range, 'recent campaigns' = last 30 days, 'this month/year' = current period. Always use ISO format YYYY-MM-DDTHH:mm:ssZ for dates.",
                    Category = "Filtering",
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
        }        private double CalculateSimpleRelevanceScore(string queryLower, KnowledgeItem knowledge)
        {
            var contentLower = knowledge.Content.ToLowerInvariant();
            var titleLower = knowledge.Title.ToLowerInvariant();
            var categoryLower = knowledge.Category.ToLowerInvariant();
            
            var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchCount = 0;
            var totalWords = queryWords.Length;
            var exactPhraseBonus = 0.0;

            // Enhanced keyword matching with email campaign specific logic
            foreach (var word in queryWords)
            {
                var wordScore = 0.0;
                
                // Direct word matches
                if (contentLower.Contains(word) || titleLower.Contains(word))
                {
                    wordScore += 1.0;
                }
                
                // Email campaign specific keyword matching
                wordScore += GetEmailCampaignWordScore(word, contentLower, titleLower);
                
                if (wordScore > 0)
                {
                    matchCount++;
                }
            }

            // Check for exact phrase matches (higher relevance)
            if (contentLower.Contains(queryLower) || titleLower.Contains(queryLower))
            {
                exactPhraseBonus = 0.3;
            }

            // Category-based scoring boost
            var categoryBonus = GetCategoryBonus(queryLower, categoryLower);
            
            // Calculate base score
            var baseScore = totalWords > 0 ? (double)matchCount / totalWords : 0.0;
            
            // Apply bonuses
            var finalScore = Math.Min(1.0, baseScore + exactPhraseBonus + categoryBonus);
            
            return finalScore;
        }

        private double GetEmailCampaignWordScore(string word, string contentLower, string titleLower)
        {
            var score = 0.0;
            
            // Percentage and rate related terms
            if ((word.Contains("rate") || word.Contains("percentage") || word.Contains("%")) && 
                (contentLower.Contains("rate") || titleLower.Contains("rate")))
            {
                score += 0.4;
            }
            
            // Performance qualifiers
            if ((word.Contains("high") || word.Contains("good") || word.Contains("excellent") || 
                 word.Contains("poor") || word.Contains("low") || word.Contains("bad")) &&
                (contentLower.Contains("performance") || titleLower.Contains("performance") || 
                 contentLower.Contains("benchmark") || titleLower.Contains("benchmark")))
            {
                score += 0.3;
            }
            
            // Email metrics (open, click, bounce, delivery)
            var emailMetrics = new[] { "open", "click", "bounce", "delivery", "delivered" };
            if (emailMetrics.Any(metric => word.Contains(metric)) &&
                emailMetrics.Any(metric => contentLower.Contains(metric) || titleLower.Contains(metric)))
            {
                score += 0.4;
            }
            
            // Comparative terms
            var comparativeTerms = new[] { "above", "below", "over", "under", "greater", "less", "between", "more", "higher", "lower" };
            if (comparativeTerms.Any(term => word.Contains(term)) &&
                (contentLower.Contains("than") || contentLower.Contains("above") || contentLower.Contains("below") ||
                 titleLower.Contains("filter") || titleLower.Contains("threshold")))
            {
                score += 0.3;
            }
            
            // Count and volume terms
            if ((word.Contains("count") || word.Contains("number") || word.Contains("volume") || word.Contains("emails")) &&
                (contentLower.Contains("count") || contentLower.Contains("emails") || contentLower.Contains("volume")))
            {
                score += 0.3;
            }
            
            return Math.Min(0.5, score); // Cap individual word bonus
        }

        private double GetCategoryBonus(string queryLower, string categoryLower)
        {
            var bonus = 0.0;
            
            // Query pattern matching
            if ((queryLower.Contains("rate") || queryLower.Contains("percentage") || queryLower.Contains("%")) &&
                categoryLower == "query-patterns")
            {
                bonus += 0.2;
            }
            
            // Metrics queries
            if ((queryLower.Contains("open") || queryLower.Contains("click") || queryLower.Contains("bounce") || queryLower.Contains("delivery")) &&
                (categoryLower == "metrics" || categoryLower == "benchmarks"))
            {
                bonus += 0.2;
            }
            
            // Performance queries
            if ((queryLower.Contains("performance") || queryLower.Contains("high") || queryLower.Contains("good") || 
                 queryLower.Contains("excellent") || queryLower.Contains("poor") || queryLower.Contains("low")) &&
                categoryLower == "performance")
            {
                bonus += 0.2;
            }
            
            // Filtering queries
            if ((queryLower.Contains("filter") || queryLower.Contains("where") || queryLower.Contains("with") || 
                 queryLower.Contains("above") || queryLower.Contains("below") || queryLower.Contains("between")) &&
                categoryLower == "filtering")
            {
                bonus += 0.15;
            }
            
            return bonus;
        }        private string BuildEnhancedQuery(string originalQuery, List<ContextItem> context)
        {
            if (!context.Any())
            {
                return originalQuery;
            }

            // Group context by category for better organization
            var contextByCategory = context
                .GroupBy(c => c.Metadata.TryGetValue("Category", out var cat) ? cat?.ToString() ?? "General" : "General")
                .ToDictionary(g => g.Key ?? "General", g => g.ToList());

            var contextSections = new List<string>();
            
            // Add context sections in order of relevance
            var priorityOrder = new[] { "Query-Patterns", "Metrics", "Performance", "Benchmarks", "Filtering", "Deliverability" };
            
            foreach (var category in priorityOrder)
            {
                if (contextByCategory.TryGetValue(category, out var items))
                {
                    var categoryContent = string.Join("\n", items.Select(item => $"• {item.Content}"));
                    contextSections.Add($"**{category} Guidelines:**\n{categoryContent}");
                }
            }
            
            // Add any remaining categories
            foreach (var kvp in contextByCategory.Where(kvp => !priorityOrder.Contains(kvp.Key)))
            {
                var categoryContent = string.Join("\n", kvp.Value.Select(item => $"• {item.Content}"));
                contextSections.Add($"**{kvp.Key}:**\n{categoryContent}");
            }

            var enhancedContext = string.Join("\n\n", contextSections);
            
            return $@"Email Campaign Knowledge Context:
{enhancedContext}

Based on this knowledge, please extract filter parameters from the following query:
{originalQuery}";
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
