using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailListsController : ControllerBase
    {
        private readonly IBigQueryService _bigQueryService;
        private readonly ILogger<EmailListsController> _logger;

        public EmailListsController(IBigQueryService bigQueryService, ILogger<EmailListsController> logger)
        {
            _bigQueryService = bigQueryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all email lists with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEmailLists([FromQuery] int pageSize = 50, [FromQuery] int offset = 0)
        {
            try
            {
                var emailLists = await _bigQueryService.GetEmailListsAsync(pageSize, offset);
                return Ok(emailLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email lists");
                return StatusCode(500, "An error occurred while retrieving email lists");
            }
        }

        /// <summary>
        /// Get recipients for a specific email list
        /// </summary>
        [HttpGet("{listId}/recipients")]
        public async Task<IActionResult> GetListRecipients(string listId, [FromQuery] int pageSize = 50, [FromQuery] int offset = 0)
        {
            try
            {
                var recipients = await _bigQueryService.GetRecipientsAsync(listId, pageSize, offset);
                return Ok(recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipients for list {ListId}", listId);
                return StatusCode(500, "An error occurred while retrieving list recipients");
            }
        }

        /// <summary>
        /// Get a specific email list by ID
        /// </summary>
        [HttpGet("{listId}")]
        public async Task<IActionResult> GetEmailList(string listId)
        {
            try
            {
                var emailList = await _bigQueryService.GetEmailListByIdAsync(listId);
                if (emailList == null)
                {
                    return NotFound($"Email list with ID {listId} not found");
                }

                return Ok(emailList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email list {ListId}", listId);
                return StatusCode(500, "An error occurred while retrieving the email list");
            }
        }
    }
}
