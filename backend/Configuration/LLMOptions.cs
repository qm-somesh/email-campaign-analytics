namespace EmailCampaignReporting.API.Configuration
{
    public class LLMOptions
    {
        public const string SectionName = "LLM";
        
        /// <summary>
        /// Path to the Llama model file (.gguf)
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 512;
        
        /// <summary>
        /// Temperature for text generation (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;
        
        /// <summary>
        /// Top-p sampling parameter
        /// </summary>
        public float TopP { get; set; } = 0.9f;
        
        /// <summary>
        /// Context size for the model
        /// </summary>
        public uint ContextSize { get; set; } = 2048;
        
        /// <summary>
        /// GPU layers to use (0 for CPU only)
        /// </summary>
        public int GpuLayers { get; set; } = 0;
        
        /// <summary>
        /// Enable verbose logging for debugging
        /// </summary>
        public bool VerboseLogging { get; set; } = false;
        
        /// <summary>
        /// Timeout for LLM operations in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
