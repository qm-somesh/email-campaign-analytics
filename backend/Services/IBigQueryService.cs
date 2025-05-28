using EmailCampaignReporting.API.Models;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    public interface IBigQueryService
    {
        Task<IEnumerable<Campaign>> GetCampaignsAsync(int pageSize = 50, int offset = 0);
        Task<Campaign?> GetCampaignByIdAsync(string campaignId);        Task<CampaignStatsDto?> GetCampaignStatsAsync(string campaignId);
        Task<IEnumerable<EmailEvent>> GetEmailEventsAsync(string? campaignId = null, string? eventType = null, int pageSize = 50, int offset = 0);
        Task<IEnumerable<EmailList>> GetEmailListsAsync(int pageSize = 50, int offset = 0);
        Task<EmailList?> GetEmailListByIdAsync(string listId);
        Task<IEnumerable<Recipient>> GetRecipientsAsync(string? listId = null, int pageSize = 50, int offset = 0);
        Task<Recipient?> GetRecipientByIdAsync(string recipientId);
        Task<DashboardMetricsDto> GetDashboardMetricsAsync();
        Task<IEnumerable<Campaign>> GetRecentCampaignsAsync(int limit = 10);
        Task<IEnumerable<EmailEvent>> GetRecentEmailEventsAsync(int limit = 100);
    }
}
