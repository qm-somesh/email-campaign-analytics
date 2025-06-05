using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTriggerReportController : ControllerBase
    {
        private readonly ISqlServerTriggerService _sqlServerTriggerService;
        private readonly ILogger<EmailTriggerReportController> _logger;

        public EmailTriggerReportController(
            ISqlServerTriggerService sqlServerTriggerService, 
            ILogger<EmailTriggerReportController> logger)
        {
            _sqlServerTriggerService = sqlServerTriggerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all email trigger reports with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of email trigger reports</returns>
        [HttpGet]
        public async Task<IActionResult> GetEmailTriggerReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Getting email trigger reports - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
                
                // Convert to offset for the service call
                int offset = (pageNumber - 1) * pageSize;
                
                var reports = await _sqlServerTriggerService.GetEmailTriggerReportsAsync(pageSize, offset);
                var reportsList = reports.ToList();
                  var response = new PaginatedResponse<EmailTriggerReportDto>
                {
                    Items = reportsList,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = reportsList.Count + offset, // Simple simulation for mock
                    TotalPages = (int)Math.Ceiling((reportsList.Count + offset) / (double)pageSize)
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email trigger reports");
                return StatusCode(500, new { error = "An error occurred while retrieving email trigger reports." });
            }
        }

        /// <summary>
        /// Get email trigger report by strategy name
        /// </summary>
        /// <param name="strategyName">The strategy name to get report for</param>
        /// <returns>Email trigger report for the specified strategy</returns>
        [HttpGet("{strategyName}")]
        public async Task<IActionResult> GetEmailTriggerReportByStrategyName(string strategyName)
        {
            try
            {
                _logger.LogInformation("Getting email trigger report for strategy: {StrategyName}", strategyName);
                
                var report = await _sqlServerTriggerService.GetEmailTriggerReportByStrategyNameAsync(strategyName);
                
                if (report == null)
                {
                    return NotFound(new { error = $"No email trigger report found for strategy '{strategyName}'" });
                }
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email trigger report for strategy: {StrategyName}", strategyName);
                return StatusCode(500, new { error = "An error occurred while retrieving the email trigger report." });
            }
        }

        /// <summary>
        /// Get summary statistics for all email triggers
        /// </summary>
        /// <returns>Summary of all email trigger statistics</returns>
        [HttpGet("summary")]
        public async Task<IActionResult> GetEmailTriggerSummary()
        {
            try
            {
                _logger.LogInformation("Getting email trigger summary");
                
                var summary = await _sqlServerTriggerService.GetEmailTriggerSummaryAsync();
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email trigger summary");
                return StatusCode(500, new { error = "An error occurred while retrieving email trigger summary." });
            }
        }

        /// <summary>
        /// Get list of all available strategy names
        /// </summary>
        /// <returns>List of strategy names</returns>
        [HttpGet("strategy-names")]
        public async Task<IActionResult> GetStrategyNames()
        {
            try
            {
                _logger.LogInformation("Getting strategy names");
                
                var strategyNames = await _sqlServerTriggerService.GetStrategyNamesAsync();
                
                return Ok(strategyNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategy names");
                return StatusCode(500, new { error = "An error occurred while retrieving strategy names." });
            }
        }

        /// <summary>
        /// Get email trigger reports with flexible filtering and pagination
        /// </summary>        /// <param name="strategyName">Filter by strategy name (partial match)</param>
        /// <param name="firstEmailSentFrom">Filter by minimum first email sent date</param>
        /// <param name="firstEmailSentTo">Filter by maximum first email sent date</param>
        /// <param name="minTotalEmails">Filter by minimum total emails count</param>
        /// <param name="maxTotalEmails">Filter by maximum total emails count</param>
        /// <param name="minDeliveredCount">Filter by minimum delivered count</param>
        /// <param name="minOpenedCount">Filter by minimum opened count</param>
        /// <param name="minClickedCount">Filter by minimum clicked count</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="sortBy">Sort field name (StrategyName, TotalEmails, DeliveredCount, etc.)</param>
        /// <param name="sortDirection">Sort direction (asc or desc)</param>
        /// <returns>Filtered and paginated list of email trigger reports</returns>
        [HttpGet("filtered")]        public async Task<IActionResult> GetEmailTriggerReportsFiltered(
            [FromQuery] string? strategyName = null,
            [FromQuery] DateTime? firstEmailSentFrom = null,
            [FromQuery] DateTime? firstEmailSentTo = null,
            [FromQuery] int? minTotalEmails = null,
            [FromQuery] int? maxTotalEmails = null,
            [FromQuery] int? minDeliveredCount = null,
            [FromQuery] int? minOpenedCount = null,
            [FromQuery] int? minClickedCount = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = "StrategyName",
            [FromQuery] string? sortDirection = "asc")
        {            try
            {
                _logger.LogInformation("Getting filtered email trigger reports - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

                // Validate pagination parameters
                if (pageNumber < 1)
                {
                    return BadRequest(new { error = "Page number must be greater than 0." });
                }

                if (pageSize < 1 || pageSize > 1000)
                {
                    return BadRequest(new { error = "Page size must be between 1 and 1000." });
                }                // Validate date range
                if (firstEmailSentFrom.HasValue && firstEmailSentTo.HasValue && firstEmailSentFrom > firstEmailSentTo)
                {
                    return BadRequest(new { error = "First email sent 'from' date cannot be later than 'to' date." });
                }

                // Validate numeric ranges
                if (minTotalEmails.HasValue && maxTotalEmails.HasValue && minTotalEmails > maxTotalEmails)
                {
                    return BadRequest(new { error = "Minimum total emails cannot be greater than maximum total emails." });
                }                // Create filter object
                var filter = new EmailTriggerReportFilterDto
                {
                    StrategyName = strategyName,
                    FirstEmailSentFrom = firstEmailSentFrom,
                    FirstEmailSentTo = firstEmailSentTo,
                    MinTotalEmails = minTotalEmails,
                    MaxTotalEmails = maxTotalEmails,
                    MinDeliveredCount = minDeliveredCount,
                    MinOpenedCount = minOpenedCount,
                    MinClickedCount = minClickedCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                var (reports, totalCount) = await _sqlServerTriggerService.GetEmailTriggerReportsFilteredAsync(filter);
                var reportsList = reports.ToList();

                var response = new PaginatedResponse<EmailTriggerReportDto>(reportsList, totalCount, pageNumber, pageSize);

                _logger.LogInformation("Retrieved {Count} filtered email trigger reports out of {TotalCount} total", 
                    reportsList.Count, totalCount);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered email trigger reports");
                return StatusCode(500, new { error = "An error occurred while retrieving filtered email trigger reports." });
            }
        }
    }
}
