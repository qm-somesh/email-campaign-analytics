using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailCampaignReporting.API.Services.LLM.RAG
{
    /// <summary>
    /// In-memory RAG service implementation for email campaign knowledge
    /// </summary>
    public class InMemoryRAGService : IRAGService
    {
        private readonly LLMProviderOptions _llmProviderOptions; // Store the whole options
        private readonly RAGOptions _ragOptions; // Specific RAG options
        private readonly ILogger<InMemoryRAGService> _logger;
        
        private readonly ConcurrentDictionary<string, KnowledgeItem> _knowledgeBase;
        private readonly ConcurrentDictionary<string, double[]> _embeddings; // Storing L2 normalized vectors

        private bool _isInitialized = false;
        private const int VectorDimension = 50; // Example dimension, real embeddings are larger (e.g., 384, 768)

        public InMemoryRAGService(
            IOptions<LLMProviderOptions> llmProviderOptions, // Corrected to use LLMProviderOptions
            ILogger<InMemoryRAGService> logger)
        {
            _llmProviderOptions = llmProviderOptions.Value;
            _ragOptions = _llmProviderOptions.RAG ?? new RAGOptions(); // RAG specific options
            _logger = logger;
            _knowledgeBase = new ConcurrentDictionary<string, KnowledgeItem>();
            _embeddings = new ConcurrentDictionary<string, double[]>();
            
            _ = Task.Run(InitializeKnowledgeBaseAsync);
        }

        // Simple text to vector conversion (character frequency based - placeholder for real embeddings)
        // In a real system, this would call an embedding model (e.g., Sentence Transformers, OpenAI Embeddings API)
        private double[] TextToVector(string text, string idForLogging = "")
        {
            var vector = new double[VectorDimension];
            if (string.IsNullOrEmpty(text))
            {
                _logger.LogWarning("TextToVector: Input text is null or empty for ID '{ItemId}'. Returning zero vector.", idForLogging);
                return Normalize(vector); // Normalize even zero vectors for consistency
            }

            var lowerText = text.ToLowerInvariant();
            // Simple character frequency for demonstration.
            // A real model would produce dense vectors capturing semantic meaning.
            for (int i = 0; i < Math.Min(lowerText.Length, VectorDimension); i++) // Simplified
            {
                vector[i % VectorDimension] += (double)lowerText[i];
            }
            
            if (vector.All(v => v == 0))
            {
                 _logger.LogWarning("TextToVector: Generated a zero vector for non-empty text ID '{ItemId}'. Text: {TextSample}", idForLogging, text.Substring(0, Math.Min(text.Length, 50)));
            }

            return Normalize(vector);
        }

        // Vector normalization (L2 norm)
        private double[] Normalize(double[] vector)
        {
            if (vector == null || vector.Length == 0) return Array.Empty<double>();
            double sumSq = vector.Sum(v => v * v);
            if (sumSq == 0) return vector; // Already a zero vector or will remain zero
            double magnitude = Math.Sqrt(sumSq);
            if (magnitude == 0) return vector; // Should be caught by sumSq == 0, but defensive
            return vector.Select(v => v / magnitude).ToArray();
        }

        // Cosine Similarity for L2 normalized vectors (dot product)
        private double CalculateCosineSimilarity(double[] vecA, double[] vecB)
        {
            if (vecA == null || vecB == null || vecA.Length != vecB.Length || vecA.Length == 0)
            {
                // _logger.LogWarning("CalculateCosineSimilarity: Invalid vectors provided. VecA Length: {LenA}, VecB Length: {LenB}", vecA?.Length, vecB?.Length);
                return 0.0; // Or throw, but returning 0 is safer for non-critical RAG
            }
            double dotProduct = 0.0;
            for (int i = 0; i < vecA.Length; i++)
            {
                dotProduct += vecA[i] * vecB[i];
            }
            // Clamp to [-1, 1] due to potential floating point inaccuracies
            return Math.Max(-1.0, Math.Min(1.0, dotProduct));
        }

        public async Task<RAGEnhancedQuery> EnhanceQueryAsync(string query, int maxContextItems = 0)
        {
            if (maxContextItems <= 0) maxContextItems = _ragOptions.MaxRetrievedDocs;

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogWarning("RAG service not yet initialized. Attempting to initialize now.");
                    await InitializeKnowledgeBaseAsync(); // Ensure initialization
                    if (!_isInitialized)
                    {
                         _logger.LogError("RAG service failed to initialize. Returning original query.");
                         // Ensure RAGEnhancedQuery uses Abstractions.ContextItem
                         return new RAGEnhancedQuery { OriginalQuery = query, EnhancedQuery = query, RetrievedContext = new List<Abstractions.ContextItem>(), ConfidenceScore = 0.0, ProcessingTimeMs = stopwatch.ElapsedMilliseconds };
                    }
                }
                
                // Use _llmProviderOptions.EnableRAG for the global RAG switch
                if (!_llmProviderOptions.EnableRAG)
                {
                    _logger.LogInformation("RAG is disabled globally in configuration. Skipping context retrieval.");
                    return new RAGEnhancedQuery { OriginalQuery = query, EnhancedQuery = query, RetrievedContext = new List<Abstractions.ContextItem>(), ConfidenceScore = 0.0, ProcessingTimeMs = stopwatch.ElapsedMilliseconds };
                }

                _logger.LogInformation("Enhancing query with RAG (Vector Similarity): '{Query}'", query);

                // RetrieveRelevantContextAsync now returns List<Abstractions.ContextItem>
                var retrievedContextItems = RetrieveRelevantContext(query, maxContextItems); // Made synchronous
                
                var enhancedQuery = BuildEnhancedQuery(query, retrievedContextItems);
                
                stopwatch.Stop();

                return new RAGEnhancedQuery
                {
                    OriginalQuery = query,
                    EnhancedQuery = enhancedQuery,
                    RetrievedContext = retrievedContextItems, // This is now correctly typed
                    ConfidenceScore = CalculateConfidenceScore(retrievedContextItems),
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
                    RetrievedContext = new List<Abstractions.ContextItem>(), // Correctly typed
                    ConfidenceScore = 0.0,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        // AddKnowledgeAsync now correctly uses Abstractions.KnowledgeItem
        // Made synchronous as current implementation is in-memory
        public Task<bool> AddKnowledgeAsync(Abstractions.KnowledgeItem knowledge)
        {
            try
            {
                if (string.IsNullOrEmpty(knowledge.Id))
                {
                    knowledge.Id = Guid.NewGuid().ToString();
                }

                // Generate vector from content + title for better matching
                var combinedTextForVector = $"{knowledge.Title} {knowledge.Content}";
                var vector = TextToVector(combinedTextForVector, knowledge.Id);

                if (vector.All(v => v == 0) && !string.IsNullOrEmpty(combinedTextForVector))
                {
                    _logger.LogWarning("AddKnowledgeAsync: Generated zero vector for knowledge item ID '{KnowledgeId}'. This item may not be effectively retrieved.", knowledge.Id);
                }

                _knowledgeBase.AddOrUpdate(knowledge.Id, knowledge, (key, old) => knowledge);
                _embeddings.AddOrUpdate(knowledge.Id, vector, (key, old) => vector);
                
                _logger.LogInformation("Added/Updated knowledge item: {Id} - {Title}. Vector generated.", knowledge.Id, knowledge.Title);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding knowledge item ID '{KnowledgeId}'", knowledge.Id);
                return Task.FromResult(false);
            }
        }

        public Task<bool> IsAvailableAsync()
        {
            // Use _llmProviderOptions.EnableRAG for the global RAG switch
            return Task.FromResult(_isInitialized && _llmProviderOptions.EnableRAG);
        }

        public Task<Dictionary<string, object>> GetKnowledgeBaseStatsAsync()
        {
            return Task.FromResult(new Dictionary<string, object>
            {
                ["TotalKnowledgeItems"] = _knowledgeBase.Count,
                ["TotalEmbeddings"] = _embeddings.Count,
                ["IsInitialized"] = _isInitialized,
                ["RAGEnabledGlobally"] = _llmProviderOptions.EnableRAG, // Reflects global RAG switch
                ["VectorDimension"] = VectorDimension,
                ["SimilarityThreshold"] = _ragOptions.SimilarityThreshold,
                ["MaxRetrievedDocs"] = _ragOptions.MaxRetrievedDocs,
                ["Categories"] = _knowledgeBase.Values.GroupBy(k => k.Category ?? "Uncategorized").ToDictionary(g => g.Key, g => g.Count())
            });
        }

        private async Task InitializeKnowledgeBaseAsync()
        {
            if (_isInitialized) return; // Prevent re-initialization

            // Ensure this runs only once, even with concurrent calls from constructor
            // A more robust approach might use SemaphoreSlim for locking initialization.
            // For now, simple flag check.
            lock(_knowledgeBase) // Simple lock to guard initialization
            {
                if (_isInitialized) return;
            }

            try
            {
                _logger.LogInformation("Initializing RAG knowledge base with vector embeddings...");

                await AddDefaultKnowledgeItemsAsync(); // Renamed for clarity

                if (!string.IsNullOrEmpty(_ragOptions.KnowledgeBaseDataPath) && File.Exists(_ragOptions.KnowledgeBaseDataPath))
                {
                    await LoadKnowledgeFromFileAsync(_ragOptions.KnowledgeBaseDataPath);
                }
                
                lock(_knowledgeBase) // Lock again before setting _isInitialized
                {
                    _isInitialized = true;
                }
                _logger.LogInformation("RAG knowledge base initialized with {Count} items. Embeddings generated.", _knowledgeBase.Count);

                if (_knowledgeBase.Any() && !_embeddings.Any())
                {
                     _logger.LogWarning("Knowledge base has items but no embeddings were generated. Check AddKnowledgeAsync and TextToVector logic.");
                }
                 if (_embeddings.Any(kvp => kvp.Value.Length != VectorDimension))
                {
                    _logger.LogError("CRITICAL: Some embeddings have incorrect dimensions. Expected: {DimExpected}, Found: {ActualDims}",
                        VectorDimension,
                        string.Join(", ", _embeddings.Where(kvp => kvp.Value.Length != VectorDimension).Select(kvp => $"ID {kvp.Key}: Dim {kvp.Value.Length}")));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RAG knowledge base");
                _isInitialized = false; // Ensure it's marked as not initialized on failure
            }
        }

        // Renamed and still async as AddKnowledgeAsync is async
        private async Task AddDefaultKnowledgeItemsAsync()
        {
            var defaultKnowledge = new[]
            {
                // Ensure these are Abstractions.KnowledgeItem
                new Abstractions.KnowledgeItem { Id = "email-metrics-basics", Title = "Email Campaign Metrics Basics", Content = "Key email metrics include open rate (percentage of emails opened), click rate (percentage of emails clicked), delivery rate (percentage of emails delivered), bounce rate (percentage of emails that bounced), and conversion rate.", Category = "Metrics", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "open-rate-benchmarks", Title = "Open Rate Benchmarks", Content = "Average email open rates typically range from 15-25% across industries. Good open rates are generally considered 20%+, while excellent rates are 30%+.", Category = "Benchmarks", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "click-rate-benchmarks", Title = "Click Rate Benchmarks", Content = "Average email click rates typically range from 2-5% across industries. Good click rates are generally considered 3%+, while excellent rates are 5%+.", Category = "Benchmarks", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "bounce-rate-info", Title = "Bounce Rate Guidelines", Content = "Bounce rates should ideally be below 2%. High bounce rates (5%+) can harm sender reputation. Soft bounces are temporary, hard bounces are permanent delivery failures.", Category = "Deliverability", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "delivery-rate-info", Title = "Delivery Rate Standards", Content = "Good delivery rates should be 95%+ for healthy email lists. Poor delivery rates may indicate list quality issues, authentication problems, or reputation issues.", Category = "Deliverability", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "date-filters", Title = "Date Filtering for Email Campaigns", Content = "When filtering by dates, FirstEmailSentFrom and FirstEmailSentTo refer to the date range when the first email in a campaign was sent. Use YYYY-MM-DD format for date queries. 'FirstEmailSentFrom' is the start of the period, 'FirstEmailSentTo' is the end.", Category = "Filtering", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "performance-queries", Title = "Common Performance Queries", Content = "High-performing campaigns typically have: open rates >20%, click rates >3%, low bounce rates <2%, and good delivery rates >95%. Use these benchmarks when filtering for top performers.", Category = "Performance", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "rate-percentage-terminology", Title = "Rate and Percentage Filter Terminology", Content = "When users mention 'rate above X%', 'rate over X%', 'rate greater than X%', or 'rate higher than X%', they mean minRate >= X. When they say 'rate below X%', 'rate under X%', or 'rate less than X%', they mean maxRate <= X. For 'rate between X% and Y%', use minRate >= X and maxRate <= Y. Convert percentages to whole numbers (e.g., 20% = 20).", Category = "Query-Patterns", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "campaign-filtering-keywords", Title = "Campaign Filtering Keywords and Synonyms", Content = "Common filtering terms: 'campaigns' = email campaigns, 'show/find/get/display' = retrieve/filter, 'with/having' = filter condition, 'above/over/greater than/higher than/more than' = minimum threshold, 'below/under/less than/lower than' = maximum threshold, 'between' = range filter, 'good/high/excellent performance' = high rates, 'poor/low/bad performance' = low rates.", Category = "Query-Patterns", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "email-metrics-synonyms", Title = "Email Metrics and Their Synonyms", Content = "Open rate synonyms: open rate, opened rate, opening rate, opens. Click rate synonyms: click rate, clicked rate, clicking rate, clicks, CTR, click-through rate. Bounce rate synonyms: bounce rate, bounced rate, bounces, hard bounce, soft bounce. Delivery rate synonyms: delivery rate, delivered rate, deliverability, delivery success.", Category = "Metrics", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "performance-qualifiers", Title = "Performance Qualifier Mapping", Content = "Performance qualifiers and their typical thresholds: 'high performance' = open rate >25%, click rate >5%, bounce rate <2%. 'good performance' = open rate >20%, click rate >3%, bounce rate <3%. 'poor performance' = open rate <15%, click rate <2%, bounce rate >5%. 'excellent/outstanding' = top 10% performers. 'average/standard' = industry benchmarks.", Category = "Performance", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "count-and-volume-filters", Title = "Count and Volume Filter Patterns", Content = "Email count filters: 'more than X emails' = minTotalEmails > X, 'delivered count above X' = minDeliveredCount > X, 'opened more than X times' = minOpenedCount > X, 'clicked more than X times' = minClickedCount > X. Volume qualifiers: 'large campaigns' = >10000 emails, 'small campaigns' = <1000 emails, 'medium campaigns' = 1000-10000 emails.", Category = "Filtering", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem { Id = "date-and-time-patterns", Title = "Date and Time Filter Patterns", Content = "Date filtering patterns: 'last month/week/year' or 'past month/week/year' imply a relative date range ending today. For example, 'last 2 months' means from two months ago until today. 'since X date' means FirstEmailSentFrom >= X. 'before X date' means FirstEmailSentTo <= X. 'during/in X period' implies a specific date range. 'recent campaigns' usually means the last 30 days. 'this month/year' refers to the current calendar month/year. Always use ISO format YYYY-MM-DDTHH:mm:ssZ for dates when possible, but the LLM should infer relative dates correctly based on current date if not specified in ISO.", Category = "Filtering", Source = "System", CreatedAt = DateTime.UtcNow },
                new Abstractions.KnowledgeItem
                {
                    Id = "relative-date-interpretation",
                    Title = "Relative Date Interpretation for Filters",
                    Content = "When interpreting relative date queries like 'last X days', 'last X weeks', or 'last X months': " +
                              "The 'FirstEmailSentTo' date should typically be interpreted as the current date (today). " +
                              "The 'FirstEmailSentFrom' date should be calculated as (current date - X days/weeks/months). " +
                              "For example, 'last 2 months' on 2025-06-17 means FirstEmailSentFrom is approximately 2025-04-17 and FirstEmailSentTo is 2025-06-17. " +
                              "Similarly, 'last 7 days' means FirstEmailSentFrom is 7 days ago from today, and FirstEmailSentTo is today. " +
                              "Queries like 'stats for campaigns sent in the past 30 days' also follow this pattern. " +
                              "The system understands the current date to make these calculations.",
                    Category = "Query-Patterns",
                    Source = "System",
                    CreatedAt = DateTime.UtcNow
                }
            };

            foreach (var knowledge in defaultKnowledge)
            {
                await AddKnowledgeAsync(knowledge); // This is Task<bool>, so await it.
            }
        }

        private async Task LoadKnowledgeFromFileAsync(string filePath)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                // Ensure deserialization to Abstractions.KnowledgeItem
                var knowledgeItems = JsonSerializer.Deserialize<Abstractions.KnowledgeItem[]>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (knowledgeItems != null)
                {
                    foreach (var item in knowledgeItems)
                    {
                        if (item.CreatedAt == DateTime.MinValue) item.CreatedAt = DateTime.UtcNow; // Ensure CreatedAt is set
                        await AddKnowledgeAsync(item); // This is Task<bool>, so await it.
                    }
                    _logger.LogInformation("Successfully loaded {Count} knowledge items from file: {FilePath}", knowledgeItems.Length, filePath);
                }
                else
                {
                    _logger.LogWarning("No knowledge items deserialized from file or file was empty: {FilePath}", filePath);
                }
            }
            catch (JsonException jsonEx)
            {
                 _logger.LogError(jsonEx, "JSON Deserialization error loading knowledge from file: {FilePath}. Content snippet: {Snippet}", filePath, File.ReadAllText(filePath).Substring(0, Math.Min(500, File.ReadAllText(filePath).Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load knowledge from file: {FilePath}", filePath);
            }
        }

        // Made synchronous as current implementation is in-memory
        private List<Abstractions.ContextItem> RetrieveRelevantContext(string query, int maxItems)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("RetrieveRelevantContext: Query is null or whitespace. Returning empty context.");
                return new List<Abstractions.ContextItem>();
            }

            var queryVector = TextToVector(query, "UserQuery");

            if (queryVector.All(v => v == 0))
            {
                _logger.LogWarning("RetrieveRelevantContext: Query vector is zero for query '{Query}'. Cannot compute similarity effectively. Returning empty context.", query);
                return new List<Abstractions.ContextItem>();
            }
             if (queryVector.Length != VectorDimension)
            {
                _logger.LogError("RetrieveRelevantContext: Query vector dimension mismatch. Expected {ExpDim}, Got {ActDim} for query '{Query}'. Returning empty context.", VectorDimension, queryVector.Length, query);
                return new List<Abstractions.ContextItem>();
            }

            var scoredItems = new List<(Abstractions.KnowledgeItem item, double score)>();

            foreach (var kvp in _knowledgeBase)
            {
                var knowledgeItem = kvp.Value;
                if (_embeddings.TryGetValue(knowledgeItem.Id, out var itemVector))
                {
                    if (itemVector.Length != VectorDimension)
                    {
                        _logger.LogWarning("Skipping item ID '{ItemId}' due to vector dimension mismatch. Expected {ExpDim}, Got {ActDim}.", knowledgeItem.Id, VectorDimension, itemVector.Length);
                        continue;
                    }
                    var similarity = CalculateCosineSimilarity(queryVector, itemVector);
                    if (similarity >= _ragOptions.SimilarityThreshold)
                    {
                        scoredItems.Add((knowledgeItem, similarity));
                    }
                }
                else
                {
                    _logger.LogWarning("No embedding found for knowledge item ID: {KnowledgeId}. It will not be considered for RAG context.", knowledgeItem.Id);
                }
            }

            if (!scoredItems.Any())
            {
                 _logger.LogInformation("No relevant context found above similarity threshold {Threshold} for query: '{Query}'", _ragOptions.SimilarityThreshold, query);
                return new List<Abstractions.ContextItem>();
            }

            // Ensure mapping to Abstractions.ContextItem
            var relevantContext = scoredItems
                .OrderByDescending(x => x.score)
                .Take(maxItems)
                .Select(x => new Abstractions.ContextItem // Explicitly use Abstractions.ContextItem
                {
                    Content = x.item.Content,
                    RelevanceScore = x.score, 
                    Source = x.item.Source,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Id"] = x.item.Id,
                        ["Title"] = x.item.Title,
                        ["Category"] = x.item.Category ?? "Uncategorized",
                        ["CreatedAt"] = x.item.CreatedAt,
                        ["SimilarityScore"] = x.score 
                    }
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} context items for query '{Query}' using vector similarity. Top score: {TopScore}", relevantContext.Count, query, relevantContext.FirstOrDefault()?.RelevanceScore ?? 0.0);
            return relevantContext;
        }      

        // BuildEnhancedQuery now takes List<Abstractions.ContextItem>
        private string BuildEnhancedQuery(string originalQuery, List<Abstractions.ContextItem> context)
        {
            if (!context.Any())
            {
                return originalQuery;
            }

            var contextByCategory = context
                .GroupBy(c => c.Metadata.TryGetValue("Category", out var cat) ? cat?.ToString() ?? "General" : "General")
                .ToDictionary(g => g.Key ?? "General", g => g.ToList());

            var contextSections = new List<string>();
            var priorityOrder = new[] { "Query-Patterns", "Metrics", "Performance", "Benchmarks", "Filtering", "Deliverability" }; 
            
            foreach (var category in priorityOrder)
            {
                if (contextByCategory.TryGetValue(category, out var items))
                {
                    var categoryContent = string.Join("\\n", items.Select(item => $"• {item.Content} (Similarity: {item.RelevanceScore:F2})"));
                    contextSections.Add($"**{category} Guidelines:**\\n{categoryContent}");
                }
            }
            
            foreach (var kvp in contextByCategory.Where(kvp => !priorityOrder.Contains(kvp.Key)))
            {
                var categoryContent = string.Join("\\n", kvp.Value.Select(item => $"• {item.Content} (Similarity: {item.RelevanceScore:F2})"));
                contextSections.Add($"**{kvp.Key}:**\\n{categoryContent}");
            }

            var enhancedContext = string.Join("\\\\n\\\\n", contextSections);
            
            // Get current date to pass to the LLM for relative date calculations
            string currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); // Or pass it in if available from a more central source

            return $@"You are an AI assistant helping to translate natural language queries about email campaign reports into structured filter parameters.
The current date is {currentDate}. Use this date to resolve any relative date expressions in the user's query (e.g., 'last 2 months', 'past week').

Use the following contextual knowledge, ranked by relevance (cosine similarity score), to better understand the user's intent, especially for date interpretations.
Higher similarity scores indicate more relevant context.

Contextual Knowledge:
{enhancedContext}

---
Original User Query: ""{originalQuery}""

Based on the query, the current date ({currentDate}), and the provided context, extract the filter parameters.
For relative date queries like 'last X months', 'FirstEmailSentTo' should be today ({currentDate}), and 'FirstEmailSentFrom' should be X months before today.
If the query is ambiguous, use the context and current date to infer the most likely intent.
If the context provides specific benchmarks for terms like """"high performing"""", use them.
Output ONLY the JSON filter object.
";
        }

        // CalculateConfidenceScore now takes List<Abstractions.ContextItem>
        private double CalculateConfidenceScore(List<Abstractions.ContextItem> context)
        {
            if (!context.Any())
                return 0.0;
            return context.Average(c => c.RelevanceScore);
        }
    }
}
