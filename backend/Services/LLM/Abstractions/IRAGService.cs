namespace EmailCampaignReporting.API.Services.LLM.Abstractions
{
    /// <summary>
    /// Interface for Retrieval-Augmented Generation (RAG) services
    /// </summary>
    public interface IRAGService
    {
        /// <summary>
        /// Enhance a query with relevant context from the knowledge base
        /// </summary>
        /// <param name="query">Original user query</param>
        /// <param name="maxContextItems">Maximum number of context items to retrieve</param>
        /// <returns>Enhanced query with retrieved context</returns>
        Task<RAGEnhancedQuery> EnhanceQueryAsync(string query, int maxContextItems = 5);

        /// <summary>
        /// Add knowledge to the RAG knowledge base
        /// </summary>
        /// <param name="knowledge">Knowledge item to add</param>
        /// <returns>Success status</returns>
        Task<bool> AddKnowledgeAsync(KnowledgeItem knowledge);

        /// <summary>
        /// Check if RAG service is available and initialized
        /// </summary>
        /// <returns>True if service is ready</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get statistics about the knowledge base
        /// </summary>
        /// <returns>Knowledge base statistics</returns>
        Task<Dictionary<string, object>> GetKnowledgeBaseStatsAsync();
    }

    /// <summary>
    /// Enhanced query with retrieved context
    /// </summary>
    public class RAGEnhancedQuery
    {
        /// <summary>
        /// Original user query
        /// </summary>
        public string OriginalQuery { get; set; } = string.Empty;

        /// <summary>
        /// Enhanced query with context
        /// </summary>
        public string EnhancedQuery { get; set; } = string.Empty;

        /// <summary>
        /// Retrieved context items
        /// </summary>
        public List<ContextItem> RetrievedContext { get; set; } = new();

        /// <summary>
        /// Confidence score for the enhancement
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Context item retrieved from knowledge base
    /// </summary>
    public class ContextItem
    {
        /// <summary>
        /// Content of the context item
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score (0-1)
        /// </summary>
        public double RelevanceScore { get; set; }

        /// <summary>
        /// Source of the context item
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Metadata about the context item
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Knowledge item for the RAG knowledge base
    /// </summary>
    public class KnowledgeItem
    {
        /// <summary>
        /// Unique identifier for the knowledge item
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Content of the knowledge item
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Title or summary of the knowledge item
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Category or type of knowledge
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Source of the knowledge item
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// When this knowledge item was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
