namespace EmailCampaignReporting.API.Configuration
{
    /// <summary>
    /// Configuration options for LLM providers
    /// </summary>
    public class LLMProviderOptions
    {
        public const string SectionName = "LLMProvider";

        /// <summary>
        /// Default LLM provider to use (LLAMA, GEMINI, etc.)
        /// </summary>
        public string DefaultProvider { get; set; } = "LLAMA";

        /// <summary>
        /// Whether to enable RAG (Retrieval-Augmented Generation)
        /// </summary>
        public bool EnableRAG { get; set; } = false;

        /// <summary>
        /// Fallback provider if the default fails
        /// </summary>
        public string? FallbackProvider { get; set; } = "LLAMA";

        /// <summary>
        /// LLAMA provider configuration (existing)
        /// </summary>
        public LLMOptions LLAMA { get; set; } = new();

        /// <summary>
        /// Gemini provider configuration
        /// </summary>
        public GeminiOptions Gemini { get; set; } = new();

        /// <summary>
        /// RAG configuration
        /// </summary>
        public RAGOptions RAG { get; set; } = new();

        /// <summary>
        /// Global timeout for all LLM operations in seconds
        /// </summary>
        public int GlobalTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum retry attempts for failed requests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Enable detailed logging for debugging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }

    /// <summary>
    /// Configuration options for Google Gemini LLM
    /// </summary>
    public class GeminiOptions
    {
        /// <summary>
        /// Google AI API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gemini model name (e.g., "gemini-1.5-flash")
        /// </summary>
        public string ModelName { get; set; } = "gemini-1.5-flash";

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 1024;

        /// <summary>
        /// Temperature for text generation (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Top-p sampling parameter
        /// </summary>
        public float TopP { get; set; } = 0.9f;

        /// <summary>
        /// Top-k sampling parameter
        /// </summary>
        public int TopK { get; set; } = 40;

        /// <summary>
        /// API endpoint URL (if using custom endpoint)
        /// </summary>
        public string? ApiEndpoint { get; set; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of candidate responses to generate
        /// </summary>
        public int CandidateCount { get; set; } = 1;

        /// <summary>
        /// Safety settings for content filtering
        /// </summary>
        public Dictionary<string, string> SafetySettings { get; set; } = new();

        /// <summary>
        /// Enable response validation
        /// </summary>
        public bool EnableResponseValidation { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for RAG (Retrieval-Augmented Generation)
    /// </summary>
    public class RAGOptions
    {
        /// <summary>
        /// Type of vector store to use (InMemory, Pinecone, Weaviate, etc.)
        /// </summary>
        public string VectorStoreType { get; set; } = "InMemory";

        /// <summary>
        /// Embedding model to use for vectorization
        /// </summary>
        public string EmbeddingModel { get; set; } = "text-embedding-004";

        /// <summary>
        /// Maximum number of documents to retrieve for context
        /// </summary>
        public int MaxRetrievedDocs { get; set; } = 5;

        /// <summary>
        /// Minimum similarity threshold for retrieved documents
        /// </summary>
        public double SimilarityThreshold { get; set; } = 0.7;

        /// <summary>
        /// Maximum length of context to include in query
        /// </summary>
        public int MaxContextLength { get; set; } = 2000;

        /// <summary>
        /// Vector store connection string (if applicable)
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// API key for embedding service (if applicable)
        /// </summary>
        public string? EmbeddingApiKey { get; set; }

        /// <summary>
        /// Enable caching of embeddings
        /// </summary>
        public bool EnableEmbeddingCache { get; set; } = true;

        /// <summary>
        /// Cache expiration time in hours
        /// </summary>
        public int CacheExpirationHours { get; set; } = 24;

        /// <summary>
        /// Knowledge base initialization data file path
        /// </summary>
        public string? KnowledgeBaseDataPath { get; set; }
    }
}
