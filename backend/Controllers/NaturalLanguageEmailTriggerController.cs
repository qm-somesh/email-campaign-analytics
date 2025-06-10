using Microsoft.AspNetCore.Mvc;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services;
using System.Diagnostics;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Controllers
{
    /// <summary>
    /// Controller for handling natural language queries for email trigger reports
    /// Uses LLM to convert natural language into filter parameters
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]    public class NaturalLanguageEmailTriggerController : ControllerBase
    {
        private readonly ISqlServerTriggerService _triggerService;
        private readonly IEmailTriggerFilterService _filterService;
        private readonly ILogger<NaturalLanguageEmailTriggerController> _logger;

        public NaturalLanguageEmailTriggerController(
            ISqlServerTriggerService triggerService,
            IEmailTriggerFilterService filterService,
            ILogger<NaturalLanguageEmailTriggerController> logger)
        {
            _triggerService = triggerService;
            _filterService = filterService;
            _logger = logger;
        }

        /// <summary>
        /// Process a natural language query for email trigger reports
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>Filtered email trigger reports based on the natural language query</returns>
        [HttpPost("query")]
        [ProducesResponseType(typeof(NaturalLanguageEmailTriggerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]        public async Task<ActionResult<NaturalLanguageEmailTriggerResponseDto>> ProcessNaturalLanguageQuery(
            [FromBody, Required] NaturalLanguageEmailTriggerQueryDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var debugInfo = new NaturalLanguageEmailTriggerDebugInfoDto();
            var response = new NaturalLanguageEmailTriggerResponseDto
            {
                OriginalQuery = request.Query,
                ProcessedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Processing natural language query: {Query}", request.Query);                // Step 1: Extract filters using dedicated LLM filter service
                var filterStopwatch = Stopwatch.StartNew();
                var filterResult = await _filterService.ExtractFiltersAsync(request.Query);
                filterStopwatch.Stop();

                debugInfo.LlmProcessingTimeMs = filterStopwatch.ElapsedMilliseconds;
                debugInfo.RawLlmResponse = filterResult.RawLLMResponse ?? "";
                debugInfo.JsonParsingSuccessful = filterResult.Success;
                debugInfo.JsonParsingError = filterResult.Error;
                debugInfo.ConfidenceScore = (double)filterResult.Confidence;

                var filters = filterResult.Filters;
                if (!filterResult.Success || filters == null)
                {
                    response.FilterExtractionSuccessful = false;
                    response.HasWarnings = true;
                    response.Warnings.Add(filterResult.Error ?? "Could not extract meaningful filters from the query");
                    filters = new EmailTriggerReportFilterDto(); // Use empty filter
                }
                else
                {
                    response.FilterExtractionSuccessful = true;
                }

                response.AppliedFilters = filters;
                response.FilterSummary = filterResult.Explanation;

                // Step 2: Apply pagination from request
                filters.PageNumber = request.PageNumber;
                filters.PageSize = request.PageSize;

                // Step 3: Execute the filtered query
                var queryStopwatch = Stopwatch.StartNew();
                var results = await _triggerService.GetEmailTriggerReportsFilteredAsync(filters);
                queryStopwatch.Stop();

                debugInfo.DatabaseQueryTimeMs = queryStopwatch.ElapsedMilliseconds;
                debugInfo.TotalRecordsAfterFiltering = results.TotalCount;

                // Convert results to PaginatedResponse<EmailTriggerReportDto>
                var reportsList = results.Reports.ToList();
                response.Results = new PaginatedResponse<EmailTriggerReportDto>(reportsList, results.TotalCount, request.PageNumber, request.PageSize);

                // Step 4: Add debug information if requested
                if (request.IncludeDebugInfo)
                {
                    PopulateDebugInfo(debugInfo, filters, filterResult);
                    response.DebugInfo = debugInfo;
                }

                stopwatch.Stop();
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation(
                    "Natural language query processed successfully. Query: {Query}, Results: {Count}, Time: {TimeMs}ms",
                    request.Query, results.TotalCount, stopwatch.ElapsedMilliseconds);

                return Ok(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                response.HasWarnings = true;
                response.Warnings.Add($"Error processing query: {ex.Message}");

                if (request.IncludeDebugInfo)
                {
                    debugInfo.ProcessingErrors.Add(ex.ToString());
                    response.DebugInfo = debugInfo;
                }

                _logger.LogError(ex, "Error processing natural language query: {Query}", request.Query);
                return StatusCode(500, $"An error occurred while processing your query: {ex.Message}");
            }
        }        /// <summary>
        /// Populate additional debug information
        /// </summary>
        private void PopulateDebugInfo(NaturalLanguageEmailTriggerDebugInfoDto debugInfo, EmailTriggerReportFilterDto filters, EmailTriggerFilterExtractionResult filterResult)
        {
            // Track which filter fields were extracted
            debugInfo.ExtractedFilterFields = filterResult.ExtractedParameters;

            // Add filter result debug info
            debugInfo.SystemPrompt = "Using dedicated EmailTriggerFilterService";
            debugInfo.UserPrompt = filterResult.Explanation;
            debugInfo.ParsedLlmJson = filterResult.Filters;

            // Generate SQL parameters representation
            debugInfo.GeneratedSqlParameters = new
            {
                MinOpenedCount = filters.MinOpenedCount,
                MinClickedCount = filters.MinClickedCount,
                MinClickRatePercentage = filters.MinClickRatePercentage,
                MaxClickRatePercentage = filters.MaxClickRatePercentage,
                MinOpenRatePercentage = filters.MinOpenRatePercentage,
                MaxOpenRatePercentage = filters.MaxOpenRatePercentage,
                MinDeliveryRatePercentage = filters.MinDeliveryRatePercentage,
                MaxDeliveryRatePercentage = filters.MaxDeliveryRatePercentage,
                MinBounceRatePercentage = filters.MinBounceRatePercentage,
                MaxBounceRatePercentage = filters.MaxBounceRatePercentage,
                StrategyName = filters.StrategyName,
                FirstEmailSentFrom = filters.FirstEmailSentFrom,
                FirstEmailSentTo = filters.FirstEmailSentTo,
                MinTotalEmails = filters.MinTotalEmails,
                MaxTotalEmails = filters.MaxTotalEmails,
                MinDeliveredCount = filters.MinDeliveredCount,
                SortBy = filters.SortBy,
                SortDirection = filters.SortDirection,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            };

            // Add service info
            debugInfo.DebugMessages.Add($"Using EmailTriggerFilterService for filter extraction");
            debugInfo.DebugMessages.Add($"Confidence: {filterResult.Confidence}");
            debugInfo.DebugMessages.Add($"Processing time: {filterResult.ProcessingTimeMs}ms");
        }
    }
}
