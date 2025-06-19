using System.Threading;
using System.Threading.Tasks;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services.NaturalSqlRAG;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NaturalLanguageSqlQueryController : ControllerBase
    {
        private readonly NaturalLanguageSqlQueryOrchestrator _orchestrator;
        private readonly ILogger<NaturalLanguageSqlQueryController> _logger;

        public NaturalLanguageSqlQueryController(
            NaturalLanguageSqlQueryOrchestrator orchestrator,
            ILogger<NaturalLanguageSqlQueryController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [HttpPost("sql-query")]
        [ProducesResponseType(typeof(PaginatedResponse<EmailTriggerReportDto>), 200)]
        public async Task<IActionResult> PostSqlQuery(
            [FromBody] NaturalLanguageSqlQueryRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest("Query cannot be empty");
            try
            {
                var result = await _orchestrator.RunAsync(request.Query, request.PageNumber, request.PageSize, cancellationToken);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language SQL query: {Query}", request.Query);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("debug-context")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> GetDebugContext(
            [FromBody] string query,
            CancellationToken cancellationToken)
        {
            try
            {
                // Access the RAG service through the orchestrator's private field using reflection
                var ragServiceField = typeof(NaturalLanguageSqlQueryOrchestrator)
                    .GetField("_ragService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (ragServiceField?.GetValue(_orchestrator) is INaturalSqlRagService ragService)
                {
                    var context = await ragService.GetContextForSqlAsync(query, 10); // Get top 10 items
                    return Ok(new { Query = query, Context = context });
                }
                
                return BadRequest("Could not access RAG service");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting debug context for query: {Query}", query);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
