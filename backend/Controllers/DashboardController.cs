using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IBigQueryService _bigQueryService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IBigQueryService bigQueryService, ILogger<DashboardController> logger)
        {
            _bigQueryService = bigQueryService;
            _logger = logger;
        }        /// <summary>
        /// Get dashboard metrics and overview data
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            try
            {
                var metrics = await _bigQueryService.GetDashboardMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard metrics");
                return StatusCode(500, "An error occurred while retrieving dashboard metrics");
            }
        }

        /// <summary>
        /// Get recent campaigns
        /// </summary>
        [HttpGet("recent-campaigns")]
        public async Task<IActionResult> GetRecentCampaigns([FromQuery] int limit = 10)
        {
            try
            {
                var campaigns = await _bigQueryService.GetRecentCampaignsAsync(limit);
                return Ok(campaigns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent campaigns");
                return StatusCode(500, "An error occurred while retrieving recent campaigns");
            }
        }

        /// <summary>
        /// Get recent email events
        /// </summary>
        [HttpGet("recent-events")]
        public async Task<IActionResult> GetRecentEmailEvents([FromQuery] int limit = 100)
        {
            try
            {
                var events = await _bigQueryService.GetRecentEmailEventsAsync(limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent email events");
                return StatusCode(500, "An error occurred while retrieving recent email events");
            }
        }
    }
}
