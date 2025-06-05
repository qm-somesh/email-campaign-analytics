using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    public interface ISqlServerTriggerService
    {        /// <summary>
        /// Get all email trigger reports with pagination
        /// </summary>
        Task<IEnumerable<EmailTriggerReportDto>> GetEmailTriggerReportsAsync(int pageSize = 50, int offset = 0);

        /// <summary>
        /// Get email trigger report by strategy name
        /// </summary>
        Task<EmailTriggerReportDto?> GetEmailTriggerReportByStrategyNameAsync(string strategyName);

        /// <summary>
        /// Get summary statistics for all email triggers
        /// </summary>
        Task<EmailTriggerReportDto> GetEmailTriggerSummaryAsync();
        
        /// <summary>
        /// Get all available strategy names
        /// </summary>
        Task<IEnumerable<string>> GetStrategyNamesAsync();        /// <summary>
        /// Get email trigger reports with flexible filtering and pagination
        /// </summary>
        Task<(IEnumerable<EmailTriggerReportDto> Reports, int TotalCount)> GetEmailTriggerReportsFilteredAsync(EmailTriggerReportFilterDto filter);

        /// <summary>
        /// Trigger an email campaign based on recipient list and parameters
        /// </summary>
        Task<EmailTriggerResponseDto> TriggerCampaignAsync(object recipientList, Dictionary<string, object>? parameters);
    }
}