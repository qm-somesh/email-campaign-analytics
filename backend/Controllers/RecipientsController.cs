using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipientsController : ControllerBase
    {
        private readonly IBigQueryService _bigQueryService;
        private readonly ILogger<RecipientsController> _logger;

        public RecipientsController(IBigQueryService bigQueryService, ILogger<RecipientsController> logger)
        {
            _bigQueryService = bigQueryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all recipients with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecipients([FromQuery] int pageSize = 50, [FromQuery] int offset = 0)
        {
            try
            {
                var recipients = await _bigQueryService.GetRecipientsAsync(null, pageSize, offset);
                return Ok(recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipients");
                return StatusCode(500, "An error occurred while retrieving recipients");
            }
        }

        /// <summary>
        /// Get a specific recipient by ID
        /// </summary>
        [HttpGet("{recipientId}")]
        public async Task<IActionResult> GetRecipient(string recipientId)
        {
            try
            {
                var recipient = await _bigQueryService.GetRecipientByIdAsync(recipientId);
                if (recipient == null)
                {
                    return NotFound($"Recipient with ID {recipientId} not found");
                }

                return Ok(recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipient {RecipientId}", recipientId);
                return StatusCode(500, "An error occurred while retrieving the recipient");
            }
        }
    }
}
