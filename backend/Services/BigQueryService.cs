using Google.Cloud.BigQuery.V2;
using EmailCampaignReporting.API.Models;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Configuration;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services
{
    public class BigQueryService : IBigQueryService
    {
        private readonly BigQueryClient _bigQueryClient;
        private readonly BigQueryOptions _options;

        public BigQueryService(IOptions<BigQueryOptions> options)
        {
            _options = options.Value;
            
            if (!string.IsNullOrEmpty(_options.CredentialsPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _options.CredentialsPath);
            }
            
            _bigQueryClient = BigQueryClient.Create(_options.ProjectId);
        }

        public async Task<IEnumerable<Campaign>> GetCampaignsAsync(int pageSize = 50, int offset = 0)
        {
            var sql = $@"
                SELECT 
                    StrategyId as campaign_id,
                    StrategyName as name,
                    EmailType as type,
                    Status as status,
                    MIN(DateCreated) as created_at,
                    MIN(ScheduledDate) as launched_at,
                    MAX(DateCreated) as completed_at,
                    Subject as subject,
                    EmailFrom as from_email,
                    CONCAT(FirstName, ' ', LastName) as from_name,
                    COUNT(DISTINCT EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN StatusId = 1 THEN 1 END) as sent_count,
                    COUNT(CASE WHEN StatusId = 2 THEN 1 END) as delivered_count,
                    0 as opened_count,
                    0 as clicked_count,
                    COUNT(CASE WHEN StatusId = 3 THEN 1 END) as bounced_count,
                    0 as unsubscribed_count,
                    0 as complaints_count,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE StrategyId IS NOT NULL
                GROUP BY StrategyId, StrategyName, EmailType, Status, Subject, EmailFrom, FirstName, LastName
                ORDER BY created_at DESC
                LIMIT {pageSize} OFFSET {offset}";

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters: null);
            
            return result.Select(row => new Campaign
            {
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Type = row["type"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                LaunchedAt = row["launched_at"] != null ? DateTime.Parse(row["launched_at"].ToString()!) : null,
                CompletedAt = row["completed_at"] != null ? DateTime.Parse(row["completed_at"].ToString()!) : null,
                Subject = row["subject"]?.ToString() ?? string.Empty,
                FromEmail = row["from_email"]?.ToString() ?? string.Empty,
                FromName = row["from_name"]?.ToString() ?? string.Empty,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                SentCount = Convert.ToInt32(row["sent_count"] ?? 0),
                DeliveredCount = Convert.ToInt32(row["delivered_count"] ?? 0),
                OpenedCount = Convert.ToInt32(row["opened_count"] ?? 0),
                ClickedCount = Convert.ToInt32(row["clicked_count"] ?? 0),
                BouncedCount = Convert.ToInt32(row["bounced_count"] ?? 0),
                UnsubscribedCount = Convert.ToInt32(row["unsubscribed_count"] ?? 0),
                ComplaintsCount = Convert.ToInt32(row["complaints_count"] ?? 0),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            });
        }

        public async Task<Campaign?> GetCampaignByIdAsync(string campaignId)
        {
            var sql = $@"
                SELECT 
                    StrategyId as campaign_id,
                    StrategyName as name,
                    EmailType as type,
                    Status as status,
                    MIN(DateCreated) as created_at,
                    MIN(ScheduledDate) as launched_at,
                    MAX(DateCreated) as completed_at,
                    Subject as subject,
                    EmailFrom as from_email,
                    CONCAT(FirstName, ' ', LastName) as from_name,
                    COUNT(DISTINCT EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN StatusId = 1 THEN 1 END) as sent_count,
                    COUNT(CASE WHEN StatusId = 2 THEN 1 END) as delivered_count,
                    0 as opened_count,
                    0 as clicked_count,
                    COUNT(CASE WHEN StatusId = 3 THEN 1 END) as bounced_count,
                    0 as unsubscribed_count,
                    0 as complaints_count,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE StrategyId = @campaignId
                GROUP BY StrategyId, StrategyName, EmailType, Status, Subject, EmailFrom, FirstName, LastName";

            var parameters = new[]
            {
                new BigQueryParameter("campaignId", BigQueryDbType.String, campaignId)
            };

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            var row = result.FirstOrDefault();
            
            if (row == null) return null;

            return new Campaign
            {
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Type = row["type"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                LaunchedAt = row["launched_at"] != null ? DateTime.Parse(row["launched_at"].ToString()!) : null,
                CompletedAt = row["completed_at"] != null ? DateTime.Parse(row["completed_at"].ToString()!) : null,
                Subject = row["subject"]?.ToString() ?? string.Empty,
                FromEmail = row["from_email"]?.ToString() ?? string.Empty,
                FromName = row["from_name"]?.ToString() ?? string.Empty,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                SentCount = Convert.ToInt32(row["sent_count"] ?? 0),
                DeliveredCount = Convert.ToInt32(row["delivered_count"] ?? 0),
                OpenedCount = Convert.ToInt32(row["opened_count"] ?? 0),
                ClickedCount = Convert.ToInt32(row["clicked_count"] ?? 0),
                BouncedCount = Convert.ToInt32(row["bounced_count"] ?? 0),
                UnsubscribedCount = Convert.ToInt32(row["unsubscribed_count"] ?? 0),
                ComplaintsCount = Convert.ToInt32(row["complaints_count"] ?? 0),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            };
        }

        public async Task<CampaignStatsDto?> GetCampaignStatsAsync(string campaignId)
        {
            var sql = $@"
                SELECT 
                    o.StrategyId as campaign_id,
                    o.StrategyName as name,
                    'Active' as status,
                    MIN(o.ScheduledDate) as launched_at,
                    COUNT(DISTINCT o.EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN o.StatusId = 1 THEN 1 END) as sent_count,
                    COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END) as delivered_count,
                    COUNT(CASE WHEN s.Status = 'opened' THEN 1 END) as opened_count,
                    COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END) as clicked_count,
                    COUNT(CASE WHEN s.Status = 'bounced' THEN 1 END) as bounced_count,
                    COUNT(CASE WHEN s.Status = 'unsubscribed' THEN 1 END) as unsubscribed_count,
                    COUNT(CASE WHEN s.Status = 'complained' THEN 1 END) as complaints_count
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}` o
                LEFT JOIN `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailStatusTable}` s 
                    ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                WHERE o.StrategyId = @campaignId
                GROUP BY o.StrategyId, o.StrategyName";

            var parameters = new[]
            {
                new BigQueryParameter("campaignId", BigQueryDbType.String, campaignId)
            };

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            var row = result.FirstOrDefault();
            
            if (row == null) return null;

            return new CampaignStatsDto
            {
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                LaunchedAt = row["launched_at"] != null ? DateTime.Parse(row["launched_at"].ToString()!) : null,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                SentCount = Convert.ToInt32(row["sent_count"] ?? 0),
                DeliveredCount = Convert.ToInt32(row["delivered_count"] ?? 0),
                OpenedCount = Convert.ToInt32(row["opened_count"] ?? 0),
                ClickedCount = Convert.ToInt32(row["clicked_count"] ?? 0),
                BouncedCount = Convert.ToInt32(row["bounced_count"] ?? 0),
                UnsubscribedCount = Convert.ToInt32(row["unsubscribed_count"] ?? 0),
                ComplaintsCount = Convert.ToInt32(row["complaints_count"] ?? 0)
            };
        }

        public async Task<IEnumerable<EmailList>> GetEmailListsAsync(int pageSize = 50, int offset = 0)
        {
            var sql = $@"
                SELECT 
                    CAST(EmailTemplateId AS STRING) as list_id,
                    TemplateName as name,
                    CONCAT('Email template for ', TemplateName) as description,
                    'Active' as status,
                    MIN(DateCreated) as created_at,
                    MAX(DateCreated) as updated_at,
                    COUNT(DISTINCT EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN StatusId = 2 THEN 1 END) as active_recipients,
                    COUNT(CASE WHEN StatusId = 3 THEN 1 END) as bounced_recipients,
                    0 as unsubscribed_recipients,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE EmailTemplateId IS NOT NULL
                GROUP BY EmailTemplateId, TemplateName
                ORDER BY created_at DESC
                LIMIT {pageSize} OFFSET {offset}";

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters: null);
            
            return result.Select(row => new EmailList
            {
                ListId = row["list_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Description = row["description"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                UpdatedAt = row["updated_at"] != null ? DateTime.Parse(row["updated_at"].ToString()!) : DateTime.MinValue,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                ActiveRecipients = Convert.ToInt32(row["active_recipients"] ?? 0),
                BouncedRecipients = Convert.ToInt32(row["bounced_recipients"] ?? 0),
                UnsubscribedRecipients = Convert.ToInt32(row["unsubscribed_recipients"] ?? 0),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            });
        }

        public async Task<EmailList?> GetEmailListByIdAsync(string listId)
        {
            var sql = $@"
                SELECT 
                    CAST(EmailTemplateId AS STRING) as list_id,
                    TemplateName as name,
                    CONCAT('Email template for ', TemplateName) as description,
                    'Active' as status,
                    MIN(DateCreated) as created_at,
                    MAX(DateCreated) as updated_at,
                    COUNT(DISTINCT EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN StatusId = 2 THEN 1 END) as active_recipients,
                    COUNT(CASE WHEN StatusId = 3 THEN 1 END) as bounced_recipients,
                    0 as unsubscribed_recipients,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE CAST(EmailTemplateId AS STRING) = @listId
                GROUP BY EmailTemplateId, TemplateName";

            var parameters = new[] { new BigQueryParameter("listId", BigQueryDbType.String, listId) };
            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            
            var row = result.FirstOrDefault();
            if (row == null) return null;

            return new EmailList
            {
                ListId = row["list_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Description = row["description"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                UpdatedAt = row["updated_at"] != null ? DateTime.Parse(row["updated_at"].ToString()!) : DateTime.MinValue,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                ActiveRecipients = Convert.ToInt32(row["active_recipients"] ?? 0),
                BouncedRecipients = Convert.ToInt32(row["bounced_recipients"] ?? 0),
                UnsubscribedRecipients = Convert.ToInt32(row["unsubscribed_recipients"] ?? 0),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            };
        }

        public async Task<IEnumerable<Recipient>> GetRecipientsAsync(string? listId = null, int pageSize = 50, int offset = 0)
        {
            var whereClause = !string.IsNullOrEmpty(listId) 
                ? "WHERE CAST(EmailTemplateId AS STRING) = @listId"
                : "";

            var sql = $@"
                SELECT 
                    CAST(EmailCustomers_SID AS STRING) as recipient_id,
                    EmailTo as email_address,
                    FirstName as first_name,
                    LastName as last_name,
                    Status as status,
                    DateCreated as created_at,
                    DateCreated as last_engagement_at,
                    '' as location,
                    '' as device_type,
                    '' as preferred_language,
                    DateCreated as subscribed_at,
                    NULL as unsubscribed_at,
                    '' as unsubscribe_reason,
                    0 as total_opens,
                    0 as total_clicks,
                    0 as total_bounces,
                    '' as custom_fields,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                {whereClause}
                ORDER BY DateCreated DESC
                LIMIT {pageSize} OFFSET {offset}";

            var parameters = !string.IsNullOrEmpty(listId) 
                ? new[] { new BigQueryParameter("listId", BigQueryDbType.String, listId) }
                : null;

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            
            return result.Select(row => new Recipient
            {
                RecipientId = row["recipient_id"]?.ToString() ?? string.Empty,
                EmailAddress = row["email_address"]?.ToString() ?? string.Empty,
                FirstName = row["first_name"]?.ToString(),
                LastName = row["last_name"]?.ToString(),
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                LastEngagementAt = row["last_engagement_at"] != null ? DateTime.Parse(row["last_engagement_at"].ToString()!) : null,
                Location = row["location"]?.ToString(),
                DeviceType = row["device_type"]?.ToString(),
                PreferredLanguage = row["preferred_language"]?.ToString(),
                SubscribedAt = row["subscribed_at"] != null ? DateTime.Parse(row["subscribed_at"].ToString()!) : null,
                UnsubscribedAt = row["unsubscribed_at"] != null ? DateTime.Parse(row["unsubscribed_at"].ToString()!) : null,
                UnsubscribeReason = row["unsubscribe_reason"]?.ToString(),
                TotalOpens = Convert.ToInt32(row["total_opens"] ?? 0),
                TotalClicks = Convert.ToInt32(row["total_clicks"] ?? 0),
                TotalBounces = Convert.ToInt32(row["total_bounces"] ?? 0),
                CustomFields = row["custom_fields"]?.ToString(),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            });
        }

        public async Task<Recipient?> GetRecipientByIdAsync(string recipientId)
        {
            var sql = $@"
                SELECT 
                    CAST(EmailCustomers_SID AS STRING) as recipient_id,
                    EmailTo as email_address,
                    FirstName as first_name,
                    LastName as last_name,
                    Status as status,
                    DateCreated as created_at,
                    DateCreated as last_engagement_at,
                    '' as location,
                    '' as device_type,
                    '' as preferred_language,
                    DateCreated as subscribed_at,
                    NULL as unsubscribed_at,
                    '' as unsubscribe_reason,
                    0 as total_opens,
                    0 as total_clicks,
                    0 as total_bounces,
                    '' as custom_fields,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE CAST(EmailCustomers_SID AS STRING) = @recipientId
                LIMIT 1";

            var parameters = new[] { new BigQueryParameter("recipientId", BigQueryDbType.String, recipientId) };
            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            
            var row = result.FirstOrDefault();
            if (row == null) return null;

            return new Recipient
            {
                RecipientId = row["recipient_id"]?.ToString() ?? string.Empty,
                EmailAddress = row["email_address"]?.ToString() ?? string.Empty,
                FirstName = row["first_name"]?.ToString(),
                LastName = row["last_name"]?.ToString(),
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                LastEngagementAt = row["last_engagement_at"] != null ? DateTime.Parse(row["last_engagement_at"].ToString()!) : null,
                Location = row["location"]?.ToString(),
                DeviceType = row["device_type"]?.ToString(),
                PreferredLanguage = row["preferred_language"]?.ToString(),
                SubscribedAt = row["subscribed_at"] != null ? DateTime.Parse(row["subscribed_at"].ToString()!) : null,
                UnsubscribedAt = row["unsubscribed_at"] != null ? DateTime.Parse(row["unsubscribed_at"].ToString()!) : null,
                UnsubscribeReason = row["unsubscribe_reason"]?.ToString(),
                TotalOpens = Convert.ToInt32(row["total_opens"] ?? 0),
                TotalClicks = Convert.ToInt32(row["total_clicks"] ?? 0),
                TotalBounces = Convert.ToInt32(row["total_bounces"] ?? 0),
                CustomFields = row["custom_fields"]?.ToString(),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            };
        }

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
        {
            return await GetDashboardMetricsAsync(null, null);
        }

        private async Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? startDate, DateTime? endDate)
        {
            var dateFilter = "";
            var parameters = new List<BigQueryParameter>();

            if (startDate.HasValue && endDate.HasValue)
            {
                dateFilter = "WHERE o.DateCreated BETWEEN @startDate AND @endDate";
                parameters.Add(new BigQueryParameter("startDate", BigQueryDbType.DateTime, startDate.Value));
                parameters.Add(new BigQueryParameter("endDate", BigQueryDbType.DateTime, endDate.Value));
            }

            // Get overall metrics
            var overallSql = $@"
                SELECT 
                    COUNT(DISTINCT o.StrategyId) as total_campaigns,
                    COUNT(DISTINCT CASE WHEN o.Status = 'Active' THEN o.StrategyId END) as active_campaigns,
                    COUNT(DISTINCT o.EmailCustomers_SID) as total_recipients,
                    COUNT(o.EmailOutboxIdentifier) as total_emails_sent,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END), COUNT(o.EmailOutboxIdentifier)) * 100 as overall_delivery_rate,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'opened' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as overall_open_rate,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as overall_click_rate,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'bounced' THEN 1 END), COUNT(o.EmailOutboxIdentifier)) * 100 as overall_bounce_rate
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}` o
                LEFT JOIN `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailStatusTable}` s 
                    ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                {dateFilter}";

            var overallResult = await _bigQueryClient.ExecuteQueryAsync(overallSql, parameters);
            var overallRow = overallResult.FirstOrDefault();

            // Get top performing campaigns
            var topCampaignsSql = $@"
                SELECT 
                    o.StrategyId as campaign_id,
                    o.StrategyName as name,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'opened' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as open_rate,
                    SAFE_DIVIDE(COUNT(CASE WHEN s.Status = 'clicked' THEN 1 END), COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END)) * 100 as click_rate,
                    COUNT(DISTINCT o.EmailCustomers_SID) as total_recipients
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}` o
                LEFT JOIN `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailStatusTable}` s 
                    ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                {(dateFilter.StartsWith("WHERE") ? dateFilter.Replace("WHERE", "WHERE") : (string.IsNullOrEmpty(dateFilter) ? "WHERE" : "AND"))} s.Status = 'delivered'
                GROUP BY o.StrategyId, o.StrategyName
                HAVING COUNT(CASE WHEN s.Status = 'delivered' THEN 1 END) > 0
                ORDER BY open_rate DESC
                LIMIT 5";

            var topCampaignsResult = await _bigQueryClient.ExecuteQueryAsync(topCampaignsSql, parameters);

            return new DashboardMetricsDto
            {
                TotalCampaigns = Convert.ToInt32(overallRow?["total_campaigns"] ?? 0),
                ActiveCampaigns = Convert.ToInt32(overallRow?["active_campaigns"] ?? 0),
                TotalRecipients = Convert.ToInt32(overallRow?["total_recipients"] ?? 0),
                TotalEmailsSent = Convert.ToInt32(overallRow?["total_emails_sent"] ?? 0),
                OverallDeliveryRate = Convert.ToDecimal(overallRow?["overall_delivery_rate"] ?? 0),
                OverallOpenRate = Convert.ToDecimal(overallRow?["overall_open_rate"] ?? 0),
                OverallClickRate = Convert.ToDecimal(overallRow?["overall_click_rate"] ?? 0),
                OverallBounceRate = Convert.ToDecimal(overallRow?["overall_bounce_rate"] ?? 0),
                TopPerformingCampaigns = topCampaignsResult.Select(row => new CampaignPerformanceDto
                {
                    CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                    Name = row["name"]?.ToString() ?? string.Empty,
                    OpenRate = Convert.ToDecimal(row["open_rate"] ?? 0),
                    ClickRate = Convert.ToDecimal(row["click_rate"] ?? 0),
                    TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0)
                }).ToList()
            };
        }

        public async Task<IEnumerable<EmailEvent>> GetEmailEventsAsync(string? campaignId = null, string? eventType = null, int pageSize = 50, int offset = 0)
        {
            var whereConditions = new List<string>();
            var parameters = new List<BigQueryParameter>();

            if (!string.IsNullOrEmpty(campaignId))
            {
                whereConditions.Add("o.StrategyId = @campaignId");
                parameters.Add(new BigQueryParameter("campaignId", BigQueryDbType.String, campaignId));
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                whereConditions.Add("s.Status = @eventType");
                parameters.Add(new BigQueryParameter("eventType", BigQueryDbType.String, eventType));
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var sql = $@"
                SELECT 
                    s.Id as event_id,
                    o.StrategyId as campaign_id,
                    CAST(o.EmailCustomers_SID AS STRING) as recipient_id,
                    s.Status as event_type,
                    s.DateCreated_UTC as timestamp,
                    o.EmailTo as email_address,
                    o.Subject as subject,
                    s.Reason as reason,
                    '' as user_agent,
                    s.IP as ip_address,
                    '' as location,
                    '' as device_type,
                    s.LinkUrl as click_url,
                    s.UserVariables as additional_data
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}` o
                LEFT JOIN `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailStatusTable}` s 
                    ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                {whereClause}
                ORDER BY s.DateCreated_UTC DESC
                LIMIT {pageSize} OFFSET {offset}";

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, parameters);
            
            return result.Select(row => new EmailEvent
            {
                EventId = row["event_id"]?.ToString() ?? string.Empty,
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                RecipientId = row["recipient_id"]?.ToString() ?? string.Empty,
                EventType = row["event_type"]?.ToString() ?? string.Empty,
                Timestamp = row["timestamp"] != null ? DateTime.Parse(row["timestamp"].ToString()!) : DateTime.MinValue,
                EmailAddress = row["email_address"]?.ToString(),
                Subject = row["subject"]?.ToString(),
                Reason = row["reason"]?.ToString(),
                UserAgent = row["user_agent"]?.ToString(),
                IpAddress = row["ip_address"]?.ToString(),
                Location = row["location"]?.ToString(),
                DeviceType = row["device_type"]?.ToString(),
                ClickUrl = row["click_url"]?.ToString(),
                AdditionalData = row["additional_data"]?.ToString()
            });
        }

        public async Task<IEnumerable<Campaign>> GetRecentCampaignsAsync(int limit = 10)
        {
            var sql = $@"
                SELECT 
                    StrategyId as campaign_id,
                    StrategyName as name,
                    EmailType as type,
                    Status as status,
                    MIN(DateCreated) as created_at,
                    MIN(ScheduledDate) as launched_at,
                    MAX(DateCreated) as completed_at,
                    Subject as subject,
                    EmailFrom as from_email,
                    CONCAT(FirstName, ' ', LastName) as from_name,
                    COUNT(DISTINCT EmailOutboxIdentifier) as total_recipients,
                    COUNT(CASE WHEN StatusId = 1 THEN 1 END) as sent_count,
                    COUNT(CASE WHEN StatusId = 2 THEN 1 END) as delivered_count,
                    0 as opened_count,
                    0 as clicked_count,
                    COUNT(CASE WHEN StatusId = 3 THEN 1 END) as bounced_count,
                    0 as unsubscribed_count,
                    0 as complaints_count,
                    '' as tags,
                    '' as notes
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}`
                WHERE StrategyId IS NOT NULL
                GROUP BY StrategyId, StrategyName, EmailType, Status, Subject, EmailFrom, FirstName, LastName
                ORDER BY created_at DESC
                LIMIT {limit}";

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, null);
            
            return result.Select(row => new Campaign
            {
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                Name = row["name"]?.ToString() ?? string.Empty,
                Type = row["type"]?.ToString() ?? string.Empty,
                Status = row["status"]?.ToString() ?? string.Empty,
                CreatedAt = row["created_at"] != null ? DateTime.Parse(row["created_at"].ToString()!) : DateTime.MinValue,
                LaunchedAt = row["launched_at"] != null ? DateTime.Parse(row["launched_at"].ToString()!) : null,
                CompletedAt = row["completed_at"] != null ? DateTime.Parse(row["completed_at"].ToString()!) : null,
                Subject = row["subject"]?.ToString() ?? string.Empty,
                FromEmail = row["from_email"]?.ToString() ?? string.Empty,
                FromName = row["from_name"]?.ToString() ?? string.Empty,
                TotalRecipients = Convert.ToInt32(row["total_recipients"] ?? 0),
                SentCount = Convert.ToInt32(row["sent_count"] ?? 0),
                DeliveredCount = Convert.ToInt32(row["delivered_count"] ?? 0),
                OpenedCount = Convert.ToInt32(row["opened_count"] ?? 0),
                ClickedCount = Convert.ToInt32(row["clicked_count"] ?? 0),
                BouncedCount = Convert.ToInt32(row["bounced_count"] ?? 0),
                UnsubscribedCount = Convert.ToInt32(row["unsubscribed_count"] ?? 0),
                ComplaintsCount = Convert.ToInt32(row["complaints_count"] ?? 0),
                Tags = row["tags"]?.ToString(),
                Notes = row["notes"]?.ToString()
            });
        }

        public async Task<IEnumerable<EmailEvent>> GetRecentEmailEventsAsync(int limit = 100)
        {
            var sql = $@"
                SELECT 
                    s.Id as event_id,
                    o.StrategyId as campaign_id,
                    CAST(o.EmailCustomers_SID AS STRING) as recipient_id,
                    s.Status as event_type,
                    s.DateCreated_UTC as timestamp,
                    o.EmailTo as email_address,
                    o.Subject as subject,
                    s.Reason as reason,
                    '' as user_agent,
                    s.IP as ip_address,
                    '' as location,
                    '' as device_type,
                    s.LinkUrl as click_url,
                    s.UserVariables as additional_data
                FROM `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailOutboxTable}` o
                LEFT JOIN `{_options.ProjectId}.{_options.DatasetId}.{_options.EmailStatusTable}` s 
                    ON o.EmailOutboxIdentifier = s.EmailOutboxIdentifier
                WHERE s.DateCreated_UTC IS NOT NULL
                ORDER BY s.DateCreated_UTC DESC
                LIMIT {limit}";

            var result = await _bigQueryClient.ExecuteQueryAsync(sql, null);
            
            return result.Select(row => new EmailEvent
            {
                EventId = row["event_id"]?.ToString() ?? string.Empty,
                CampaignId = row["campaign_id"]?.ToString() ?? string.Empty,
                RecipientId = row["recipient_id"]?.ToString() ?? string.Empty,
                EventType = row["event_type"]?.ToString() ?? string.Empty,
                Timestamp = row["timestamp"] != null ? DateTime.Parse(row["timestamp"].ToString()!) : DateTime.MinValue,
                EmailAddress = row["email_address"]?.ToString(),
                Subject = row["subject"]?.ToString(),
                Reason = row["reason"]?.ToString(),
                UserAgent = row["user_agent"]?.ToString(),
                IpAddress = row["ip_address"]?.ToString(),
                Location = row["location"]?.ToString(),
                DeviceType = row["device_type"]?.ToString(),
                ClickUrl = row["click_url"]?.ToString(),
                AdditionalData = row["additional_data"]?.ToString()
            });
        }
    }
}