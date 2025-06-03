using EmailCampaignReporting.API.Models;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Service interface for campaign-specific queries that can be called by natural language processing
    /// </summary>
    public interface ICampaignQueryService
    {
        // Campaign queries
        Task<IEnumerable<Campaign>> GetCampaignsByMonthAsync(int month, int year = 0);
        Task<IEnumerable<Campaign>> GetTopPerformingCampaignsAsync(int limit = 10);
        Task<IEnumerable<Campaign>> GetRecentCampaignsAsync(int days = 30, int limit = 20);
        Task<IEnumerable<Campaign>> GetCampaignsByNameAsync(string namePattern, int limit = 50);
        Task<IEnumerable<Campaign>> GetCampaignsByTypeAsync(string type, int limit = 50);
        
        // Metrics and dashboard queries
        Task<DashboardMetricsDto> GetDashboardMetricsAsync(int days = 30);
        Task<object> GetEmailMetricsSummaryAsync(int days = 30);
        Task<IEnumerable<object>> GetCampaignPerformanceMetricsAsync(int limit = 10);
        
        // Event queries
        Task<IEnumerable<EmailEvent>> GetBouncedEmailsAsync(int? month = null, int limit = 100);
        Task<IEnumerable<EmailEvent>> GetEmailEngagementAsync(int? month = null, int limit = 100);
        Task<IEnumerable<EmailEvent>> GetEmailEventsByTypeAsync(string eventType, int limit = 100);
        
        // Recipient queries
        Task<IEnumerable<Recipient>> GetTopRecipientsAsync(int limit = 50);
        Task<IEnumerable<Recipient>> GetRecipientsByMonthAsync(int month, int year = 0, int limit = 50);
        Task<IEnumerable<Recipient>> GetEngagedRecipientsAsync(int limit = 50);
        
        // Email list queries
        Task<IEnumerable<EmailList>> GetEmailListsSummaryAsync(int limit = 20);
        Task<IEnumerable<object>> GetListPerformanceMetricsAsync(int limit = 10);
    }
}
