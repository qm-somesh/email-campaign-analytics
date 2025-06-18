using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{
    /// <summary>
    /// Interface for RAG service dedicated to SQL query generation context.
    /// </summary>
    public interface INaturalSqlRagService
    {
        /// <summary>
        /// Given a user query, returns a list of context strings relevant for SQL generation.
        /// </summary>
        /// <param name="userQuery">The user's natural language query.</param>
        /// <param name="maxContextItems">Maximum number of context items to retrieve.</param>
        /// <returns>List of context strings.</returns>
        Task<List<string>> GetContextForSqlAsync(string userQuery, int maxContextItems = 5);
    }
}
