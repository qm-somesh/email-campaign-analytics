using EmailCampaignReporting.API.Models;
using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    public class MockBigQueryService : IBigQueryService
    {
        public async Task<IEnumerable<Campaign>> GetCampaignsAsync(int pageSize = 50, int offset = 0)
        {
            await Task.Delay(100); // Simulate async operation
            
            return new List<Campaign>
            {
                new Campaign
                {
                    CampaignId = "campaign-001",
                    Name = "Black Friday Email Campaign",
                    Type = "promotional",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddDays(-10),
                    LaunchedAt = DateTime.Now.AddDays(-9),
                    Subject = "Huge Black Friday Savings - Up to 70% Off!",
                    FromEmail = "marketing@company.com",
                    FromName = "Marketing Team",
                    TotalRecipients = 15000,
                    SentCount = 14850,
                    DeliveredCount = 14200,
                    OpenedCount = 4260,
                    ClickedCount = 852,
                    BouncedCount = 650,
                    UnsubscribedCount = 25,
                    ComplaintsCount = 3
                },
                new Campaign
                {
                    CampaignId = "campaign-002",
                    Name = "Holiday Newsletter",
                    Type = "newsletter",
                    Status = "Completed",
                    CreatedAt = DateTime.Now.AddDays(-20),
                    LaunchedAt = DateTime.Now.AddDays(-18),
                    CompletedAt = DateTime.Now.AddDays(-17),
                    Subject = "Holiday Update & Year-End Review",
                    FromEmail = "newsletter@company.com",
                    FromName = "Company Newsletter",
                    TotalRecipients = 8500,
                    SentCount = 8350,
                    DeliveredCount = 8100,
                    OpenedCount = 3240,
                    ClickedCount = 486,
                    BouncedCount = 250,
                    UnsubscribedCount = 12,
                    ComplaintsCount = 1
                },
                new Campaign
                {
                    CampaignId = "campaign-003",
                    Name = "Product Launch Announcement",
                    Type = "announcement",
                    Status = "Scheduled",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Subject = "Introducing Our Revolutionary New Product!",
                    FromEmail = "announcements@company.com",
                    FromName = "Product Team",
                    TotalRecipients = 12000,
                    SentCount = 0,
                    DeliveredCount = 0,
                    OpenedCount = 0,
                    ClickedCount = 0,
                    BouncedCount = 0,
                    UnsubscribedCount = 0,
                    ComplaintsCount = 0
                }
            };
        }

        public async Task<Campaign?> GetCampaignByIdAsync(string campaignId)
        {
            await Task.Delay(50);
            
            var campaigns = await GetCampaignsAsync();
            return campaigns.FirstOrDefault(c => c.CampaignId == campaignId);
        }        public async Task<CampaignStatsDto?> GetCampaignStatsAsync(string campaignId)
        {
            await Task.Delay(100);
            
            return new CampaignStatsDto
            {
                CampaignId = campaignId,
                Name = "Black Friday Email Campaign",
                Status = "Active",
                LaunchedAt = DateTime.Now.AddDays(-9),
                TotalRecipients = 15000,
                SentCount = 14850,
                DeliveredCount = 14200,
                OpenedCount = 4260,
                ClickedCount = 852,
                BouncedCount = 650,
                UnsubscribedCount = 25,
                ComplaintsCount = 3
            };
        }        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
        {
            await Task.Delay(150);
            
            return new DashboardMetricsDto
            {
                TotalCampaigns = 15,
                ActiveCampaigns = 5,
                TotalRecipients = 125000,
                ActiveRecipients = 118500,
                TotalEmailsSent = 456000,
                OverallDeliveryRate = 94.2m,
                OverallOpenRate = 28.5m,
                OverallClickRate = 18.7m,
                OverallBounceRate = 5.8m,
                RecentCampaignsCount = 8,
                RecentEventsCount = 1247,
                TopPerformingCampaigns = new List<CampaignPerformanceDto>
                {
                    new CampaignPerformanceDto
                    {
                        CampaignId = "campaign-001",
                        Name = "Black Friday Email Campaign",
                        OpenRate = 30.0m,
                        ClickRate = 20.0m,
                        TotalRecipients = 15000
                    },
                    new CampaignPerformanceDto
                    {
                        CampaignId = "campaign-002",
                        Name = "Holiday Newsletter",
                        OpenRate = 40.0m,
                        ClickRate = 15.0m,
                        TotalRecipients = 8500
                    },
                    new CampaignPerformanceDto
                    {
                        CampaignId = "campaign-004",
                        Name = "Summer Sale",
                        OpenRate = 35.5m,
                        ClickRate = 22.1m,
                        TotalRecipients = 18000
                    }
                }
            };
        }

        public async Task<IEnumerable<EmailList>> GetEmailListsAsync(int pageSize = 50, int offset = 0)
        {
            await Task.Delay(100);
            
            return new List<EmailList>
            {
                new EmailList
                {
                    ListId = "list-001",
                    Name = "VIP Customers",
                    Description = "Premium customers with high engagement",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    UpdatedAt = DateTime.Now.AddDays(-1),
                    TotalRecipients = 5000,
                    ActiveRecipients = 4850,
                    BouncedRecipients = 100,
                    UnsubscribedRecipients = 50
                },
                new EmailList
                {
                    ListId = "list-002",
                    Name = "Newsletter Subscribers",
                    Description = "Monthly newsletter subscriber list",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddMonths(-12),
                    UpdatedAt = DateTime.Now.AddDays(-3),
                    TotalRecipients = 25000,
                    ActiveRecipients = 23500,
                    BouncedRecipients = 800,
                    UnsubscribedRecipients = 700
                },
                new EmailList
                {
                    ListId = "list-003",
                    Name = "Product Updates",
                    Description = "Users interested in product announcements",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    UpdatedAt = DateTime.Now.AddDays(-2),
                    TotalRecipients = 12000,
                    ActiveRecipients = 11200,
                    BouncedRecipients = 400,
                    UnsubscribedRecipients = 400
                }
            };
        }

        public async Task<EmailList?> GetEmailListByIdAsync(string listId)
        {
            await Task.Delay(50);
            
            var lists = await GetEmailListsAsync();
            return lists.FirstOrDefault(l => l.ListId == listId);
        }

        public async Task<IEnumerable<Recipient>> GetRecipientsAsync(string? listId = null, int pageSize = 50, int offset = 0)
        {
            await Task.Delay(100);
            
            return new List<Recipient>
            {
                new Recipient
                {
                    RecipientId = "recipient-001",
                    EmailAddress = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    LastEngagementAt = DateTime.Now.AddDays(-2),
                    Location = "New York, NY",
                    DeviceType = "Desktop",
                    PreferredLanguage = "en",
                    SubscribedAt = DateTime.Now.AddMonths(-6),
                    TotalOpens = 45,
                    TotalClicks = 12
                },
                new Recipient
                {
                    RecipientId = "recipient-002",
                    EmailAddress = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Status = "Active",
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    LastEngagementAt = DateTime.Now.AddDays(-1),
                    Location = "Los Angeles, CA",
                    DeviceType = "Mobile",
                    PreferredLanguage = "en",
                    SubscribedAt = DateTime.Now.AddMonths(-4),
                    TotalOpens = 62,
                    TotalClicks = 18
                },
                new Recipient
                {
                    RecipientId = "recipient-003",
                    EmailAddress = "bob.wilson@example.com",
                    FirstName = "Bob",
                    LastName = "Wilson",
                    Status = "Bounced",
                    CreatedAt = DateTime.Now.AddMonths(-8),
                    LastEngagementAt = DateTime.Now.AddMonths(-2),
                    Location = "Chicago, IL",
                    DeviceType = "Desktop",
                    PreferredLanguage = "en",
                    SubscribedAt = DateTime.Now.AddMonths(-8),
                    TotalOpens = 15,
                    TotalClicks = 3
                }
            };
        }

        public async Task<Recipient?> GetRecipientByIdAsync(string recipientId)
        {
            await Task.Delay(50);
            
            var recipients = await GetRecipientsAsync();
            return recipients.FirstOrDefault(r => r.RecipientId == recipientId);
        }

        public async Task<IEnumerable<EmailEvent>> GetRecentEmailEventsAsync(int limit = 100)
        {
            await Task.Delay(100);
            
            return new List<EmailEvent>
            {
                new EmailEvent
                {
                    EventId = "event-001",
                    CampaignId = "campaign-001",
                    RecipientId = "recipient-001",
                    EventType = "opened",
                    Timestamp = DateTime.Now.AddHours(-2),
                    EmailAddress = "john.doe@example.com",
                    Subject = "Black Friday Email Campaign",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                    IpAddress = "192.168.1.100",
                    Location = "New York, NY",
                    DeviceType = "Desktop"
                },
                new EmailEvent
                {
                    EventId = "event-002",
                    CampaignId = "campaign-001",
                    RecipientId = "recipient-002",
                    EventType = "clicked",
                    Timestamp = DateTime.Now.AddHours(-1),
                    EmailAddress = "jane.smith@example.com",
                    Subject = "Black Friday Email Campaign",
                    UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0)",
                    IpAddress = "192.168.1.101",
                    Location = "Los Angeles, CA",
                    DeviceType = "Mobile",
                    ClickUrl = "https://company.com/black-friday-deals"
                },
                new EmailEvent
                {
                    EventId = "event-003",
                    CampaignId = "campaign-002",
                    RecipientId = "recipient-003",
                    EventType = "bounced",
                    Timestamp = DateTime.Now.AddHours(-3),
                    EmailAddress = "invalid@example.com",
                    Subject = "Holiday Newsletter",
                    Reason = "mailbox_full"
                }
            };
        }

        public async Task<IEnumerable<EmailEvent>> GetEmailEventsAsync(string? campaignId = null, string? eventType = null, int pageSize = 50, int offset = 0)
        {
            await Task.Delay(100);
            
            var allEvents = await GetRecentEmailEventsAsync(1000);
            
            var filteredEvents = allEvents.AsQueryable();
            
            if (!string.IsNullOrEmpty(campaignId))
            {
                filteredEvents = filteredEvents.Where(e => e.CampaignId == campaignId);
            }
            
            if (!string.IsNullOrEmpty(eventType))
            {
                filteredEvents = filteredEvents.Where(e => e.EventType == eventType);
            }
            
            return filteredEvents.Skip(offset).Take(pageSize).ToList();
        }

        public async Task<IEnumerable<Campaign>> GetRecentCampaignsAsync(int limit = 10)
        {
            await Task.Delay(50);
            
            var campaigns = await GetCampaignsAsync();
            return campaigns.Take(limit);
        }
    }
}
