using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{
    /// <summary>
    /// Mock embedding service for demonstration purposes.
    /// In production, replace this with a real embedding service like OpenAI, Azure OpenAI, or Google AI.
    /// </summary>
    public class MockEmbeddingService : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly Random _random = new Random(42); // Fixed seed for consistent results        public EmbeddingGeneratorMetadata Metadata => new("mock-embeddings");

        public void Dispose()
        {
            // No resources to dispose
        }

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, 
            EmbeddingGenerationOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken); // Simulate API call delay

            var embeddings = new List<Embedding<float>>();
            
            foreach (var value in values)
            {
                // Generate a simple hash-based embedding for consistent results
                var embedding = GenerateEmbeddingForText(value);
                embeddings.Add(new Embedding<float>(embedding));
            }

            return new GeneratedEmbeddings<Embedding<float>>(embeddings);
        }

        private ReadOnlyMemory<float> GenerateEmbeddingForText(string text)
        {
            // Simple hash-based embedding generation
            var hash = text.GetHashCode();
            var rng = new Random(hash);
            
            var embedding = new float[384];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(rng.NextDouble() * 2.0 - 1.0); // Values between -1 and 1
            }

            // Normalize the vector
            var norm = 0f;
            for (int i = 0; i < embedding.Length; i++)
            {
                norm += embedding[i] * embedding[i];
            }
            norm = MathF.Sqrt(norm);

            if (norm > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= norm;
                }
            }

            return new ReadOnlyMemory<float>(embedding);
        }        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return serviceType.IsAssignableFrom(GetType()) ? this : null;
        }
    }
}
