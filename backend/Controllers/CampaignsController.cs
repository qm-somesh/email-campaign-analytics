using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Models;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly IBigQueryService _bigQueryService;
        private readonly ILogger<CampaignsController> _logger;

        public CampaignsController(IBigQueryService bigQueryService, ILogger<CampaignsController> logger)
        {
            _bigQueryService = bigQueryService;
            _logger = logger;
        }        /// <summary>
        /// Get all campaigns with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCampaigns([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Convert to offset for the service call
                int offset = (pageNumber - 1) * pageSize;
                
                var campaigns = await _bigQueryService.GetCampaignsAsync(pageSize, offset);
                var campaignsList = campaigns.ToList();
                
                // For mock service, we'll simulate total count
                int totalCount = campaignsList.Count + offset; // Simple simulation
                
                var response = new 
                {
                    Items = campaignsList,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = pageNumber > 1
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaigns");
                return StatusCode(500, "An error occurred while retrieving campaigns");
            }
        }

        /// <summary>
        /// Get a specific campaign by ID
        /// </summary>
        [HttpGet("{campaignId}")]
        public async Task<IActionResult> GetCampaign(string campaignId)
        {
            try
            {
                var campaign = await _bigQueryService.GetCampaignByIdAsync(campaignId);
                if (campaign == null)
                {
                    return NotFound($"Campaign with ID {campaignId} not found");
                }

                return Ok(campaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign {CampaignId}", campaignId);
                return StatusCode(500, "An error occurred while retrieving the campaign");
            }
        }

        /// <summary>
        /// Get campaign statistics and metrics
        /// </summary>
        [HttpGet("{campaignId}/stats")]
        public async Task<IActionResult> GetCampaignStats(string campaignId)
        {
            try
            {
                var stats = await _bigQueryService.GetCampaignStatsAsync(campaignId);
                if (stats == null)
                {
                    return NotFound($"Campaign with ID {campaignId} not found");
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign stats for {CampaignId}", campaignId);
                return StatusCode(500, "An error occurred while retrieving campaign statistics");
            }
        }        /// <summary>
        /// Get email events for a specific campaign
        /// </summary>
        [HttpGet("{campaignId}/events")]
        public async Task<IActionResult> GetCampaignEvents(string campaignId, [FromQuery] string? eventType = null, [FromQuery] int pageSize = 50, [FromQuery] int offset = 0)
        {
            try
            {
                var events = await _bigQueryService.GetEmailEventsAsync(campaignId, eventType, pageSize, offset);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for campaign {CampaignId}", campaignId);
                return StatusCode(500, "An error occurred while retrieving campaign events");
            }
        }
    }
}
