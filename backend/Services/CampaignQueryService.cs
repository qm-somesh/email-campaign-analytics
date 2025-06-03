using EmailCampaignReporting.API.Models;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Campaign query service implementation for natural language query processing
    /// </summary>
    public class CampaignQueryService : ICampaignQueryService
    {
        private readonly IBigQueryService _bigQueryService;
        private readonly ILogger<CampaignQueryService> _logger;

        public CampaignQueryService(IBigQueryService bigQueryService, ILogger<CampaignQueryService> logger)
        {
            _bigQueryService = bigQueryService;
            _logger = logger;
        }

        public async Task<IEnumerable<Campaign>> GetCampaignsByMonthAsync(int month, int year = 0)
        {
            _logger.LogInformation("Getting campaigns for month {Month}, year {Year}", month, year);
            
            // For now, get all campaigns and filter by month (in production, this would be a more targeted query)
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            if (year == 0) year = DateTime.Now.Year;
            
            return campaigns.Where(c => c.CreatedAt.Month == month && c.CreatedAt.Year == year);
        }

        public async Task<IEnumerable<Campaign>> GetTopPerformingCampaignsAsync(int limit = 10)
        {
            _logger.LogInformation("Getting top {Limit} performing campaigns", limit);
            
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            // Sort by a combination of open rate and click rate
            return campaigns
                .Where(c => c.SentCount > 0)
                .OrderByDescending(c => (c.OpenedCount * 1.0 / c.SentCount) + (c.ClickedCount * 2.0 / c.SentCount))
                .Take(limit);
        }

        public async Task<IEnumerable<Campaign>> GetRecentCampaignsAsync(int days = 30, int limit = 20)
        {
            _logger.LogInformation("Getting campaigns from last {Days} days, limit {Limit}", days, limit);
            
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            var cutoffDate = DateTime.Now.AddDays(-days);
            
            return campaigns
                .Where(c => c.CreatedAt >= cutoffDate)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit);
        }

        public async Task<IEnumerable<Campaign>> GetCampaignsByNameAsync(string namePattern, int limit = 50)
        {
            _logger.LogInformation("Getting campaigns matching name pattern: {Pattern}", namePattern);
            
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            return campaigns
                .Where(c => c.Name?.Contains(namePattern, StringComparison.OrdinalIgnoreCase) == true)
                .Take(limit);
        }

        public async Task<IEnumerable<Campaign>> GetCampaignsByTypeAsync(string type, int limit = 50)
        {
            _logger.LogInformation("Getting campaigns of type: {Type}", type);
            
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            return campaigns
                .Where(c => c.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true)
                .Take(limit);
        }

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(int days = 30)
        {
            _logger.LogInformation("Getting dashboard metrics for last {Days} days", days);
            
            return await _bigQueryService.GetDashboardMetricsAsync();
        }

        public async Task<object> GetEmailMetricsSummaryAsync(int days = 30)
        {
            _logger.LogInformation("Getting email metrics summary for last {Days} days", days);
            
            var metrics = await _bigQueryService.GetDashboardMetricsAsync();
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            var cutoffDate = DateTime.Now.AddDays(-days);
            var recentCampaigns = campaigns.Where(c => c.CreatedAt >= cutoffDate).ToList();
            
            return new
            {
                period_days = days,
                total_campaigns = recentCampaigns.Count,
                total_emails_sent = recentCampaigns.Sum(c => c.SentCount),
                total_delivered = recentCampaigns.Sum(c => c.DeliveredCount),
                total_opened = recentCampaigns.Sum(c => c.OpenedCount),
                total_clicked = recentCampaigns.Sum(c => c.ClickedCount),
                total_bounced = recentCampaigns.Sum(c => c.BouncedCount),
                overall_delivery_rate = recentCampaigns.Sum(c => c.SentCount) > 0 ? 
                    (double)recentCampaigns.Sum(c => c.DeliveredCount) / recentCampaigns.Sum(c => c.SentCount) * 100 : 0,
                overall_open_rate = recentCampaigns.Sum(c => c.DeliveredCount) > 0 ? 
                    (double)recentCampaigns.Sum(c => c.OpenedCount) / recentCampaigns.Sum(c => c.DeliveredCount) * 100 : 0,
                overall_click_rate = recentCampaigns.Sum(c => c.DeliveredCount) > 0 ? 
                    (double)recentCampaigns.Sum(c => c.ClickedCount) / recentCampaigns.Sum(c => c.DeliveredCount) * 100 : 0
            };
        }

        public async Task<IEnumerable<object>> GetCampaignPerformanceMetricsAsync(int limit = 10)
        {
            _logger.LogInformation("Getting campaign performance metrics, limit {Limit}", limit);
            
            var campaigns = await _bigQueryService.GetCampaignsAsync(1000, 0);
            
            return campaigns
                .Where(c => c.SentCount > 0)
                .Select(c => new
                {
                    campaign_id = c.CampaignId,
                    name = c.Name,
                    type = c.Type,
                    sent_count = c.SentCount,
                    delivered_count = c.DeliveredCount,
                    opened_count = c.OpenedCount,
                    clicked_count = c.ClickedCount,
                    delivery_rate = c.SentCount > 0 ? (double)c.DeliveredCount / c.SentCount * 100 : 0,
                    open_rate = c.DeliveredCount > 0 ? (double)c.OpenedCount / c.DeliveredCount * 100 : 0,
                    click_rate = c.DeliveredCount > 0 ? (double)c.ClickedCount / c.DeliveredCount * 100 : 0,
                    created_at = c.CreatedAt
                })
                .OrderByDescending(c => c.open_rate + c.click_rate)
                .Take(limit);
        }

        public async Task<IEnumerable<EmailEvent>> GetBouncedEmailsAsync(int? month = null, int limit = 100)
        {
            _logger.LogInformation("Getting bounced emails for month {Month}, limit {Limit}", month, limit);
            
            var events = await _bigQueryService.GetEmailEventsAsync(eventType: "bounced", pageSize: limit);
            
            if (month.HasValue)
            {
                events = events.Where(e => e.Timestamp.Month == month.Value);
            }
            
            return events.Take(limit);
        }

        public async Task<IEnumerable<EmailEvent>> GetEmailEngagementAsync(int? month = null, int limit = 100)
        {
            _logger.LogInformation("Getting email engagement for month {Month}, limit {Limit}", month, limit);
            
            var openEvents = await _bigQueryService.GetEmailEventsAsync(eventType: "opened", pageSize: limit / 2);
            var clickEvents = await _bigQueryService.GetEmailEventsAsync(eventType: "clicked", pageSize: limit / 2);
            
            var allEvents = openEvents.Concat(clickEvents);
            
            if (month.HasValue)
            {
                allEvents = allEvents.Where(e => e.Timestamp.Month == month.Value);
            }
            
            return allEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(limit);
        }

        public async Task<IEnumerable<EmailEvent>> GetEmailEventsByTypeAsync(string eventType, int limit = 100)
        {
            _logger.LogInformation("Getting email events of type {EventType}, limit {Limit}", eventType, limit);
            
            return await _bigQueryService.GetEmailEventsAsync(eventType: eventType, pageSize: limit);
        }

        public async Task<IEnumerable<Recipient>> GetTopRecipientsAsync(int limit = 50)
        {
            _logger.LogInformation("Getting top {Limit} recipients", limit);
            
            var recipients = await _bigQueryService.GetRecipientsAsync(pageSize: 1000);
            
            // For now, return all recipients (in production, this would rank by engagement)
            return recipients.Take(limit);
        }

        public async Task<IEnumerable<Recipient>> GetRecipientsByMonthAsync(int month, int year = 0, int limit = 50)
        {
            _logger.LogInformation("Getting recipients for month {Month}, year {Year}, limit {Limit}", month, year, limit);
            
            var recipients = await _bigQueryService.GetRecipientsAsync(pageSize: 1000);
            
            if (year == 0) year = DateTime.Now.Year;
              // Filter by creation date month (this would be more sophisticated in production)
            return recipients
                .Where(r => r.LastEngagementAt?.Month == month && r.LastEngagementAt?.Year == year)
                .Take(limit);
        }

        public async Task<IEnumerable<Recipient>> GetEngagedRecipientsAsync(int limit = 50)
        {
            _logger.LogInformation("Getting engaged recipients, limit {Limit}", limit);
            
            var recipients = await _bigQueryService.GetRecipientsAsync(pageSize: 1000);
            
            // Filter by engagement metrics (opens + clicks > 0)
            return recipients
                .Where(r => (r.TotalOpens + r.TotalClicks) > 0)
                .OrderByDescending(r => r.TotalOpens + r.TotalClicks)
                .Take(limit);
        }

        public async Task<IEnumerable<EmailList>> GetEmailListsSummaryAsync(int limit = 20)
        {
            _logger.LogInformation("Getting email lists summary, limit {Limit}", limit);
            
            return await _bigQueryService.GetEmailListsAsync(pageSize: limit);
        }

        public async Task<IEnumerable<object>> GetListPerformanceMetricsAsync(int limit = 10)
        {
            _logger.LogInformation("Getting list performance metrics, limit {Limit}", limit);
            
            var lists = await _bigQueryService.GetEmailListsAsync(pageSize: limit);
              return lists.Select(l => new
            {
                list_id = l.ListId,
                name = l.Name,
                total_subscribers = l.TotalRecipients,
                active_subscribers = l.ActiveRecipients,
                engagement_rate = l.TotalRecipients > 0 ? (double)l.ActiveRecipients / l.TotalRecipients * 100 : 0,
                created_at = l.CreatedAt
            });
        }
    }
}
