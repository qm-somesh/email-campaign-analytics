using EmailCampaignReporting.API.Models.DTOs;

namespace EmailCampaignReporting.API.Services
{
    public class MockEmailTriggerService : ISqlServerTriggerService
    {
        private readonly ILogger<MockEmailTriggerService> _logger;
        private readonly List<EmailTriggerReportDto> _mockData;

        public MockEmailTriggerService(ILogger<MockEmailTriggerService> logger)
        {
            _logger = logger;
            _mockData = GenerateMockData();
        }

        public async Task<IEnumerable<EmailTriggerReportDto>> GetEmailTriggerReportsAsync(int pageSize = 50, int offset = 0)
        {
            _logger.LogInformation("Mock: Getting email trigger reports with pageSize: {PageSize}, offset: {Offset}", pageSize, offset);
            
            await Task.Delay(100); // Simulate async operation
            
            return _mockData
                .Skip(offset)
                .Take(pageSize)
                .ToList();
        }

        public async Task<EmailTriggerReportDto?> GetEmailTriggerReportByStrategyNameAsync(string strategyName)
        {
            _logger.LogInformation("Mock: Getting email trigger report for strategy: {StrategyName}", strategyName);
            
            await Task.Delay(50); // Simulate async operation
            
            return _mockData.FirstOrDefault(x => 
                x.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<EmailTriggerReportDto> GetEmailTriggerSummaryAsync()
        {
            _logger.LogInformation("Mock: Getting email trigger summary");
            
            await Task.Delay(100); // Simulate async operation
            
            return new EmailTriggerReportDto
            {
                StrategyName = "ALL_STRATEGIES_SUMMARY",
                TotalEmails = _mockData.Sum(x => x.TotalEmails),
                DeliveredCount = _mockData.Sum(x => x.DeliveredCount),
                BouncedCount = _mockData.Sum(x => x.BouncedCount),
                OpenedCount = _mockData.Sum(x => x.OpenedCount),
                ClickedCount = _mockData.Sum(x => x.ClickedCount),
                ComplainedCount = _mockData.Sum(x => x.ComplainedCount),
                UnsubscribedCount = _mockData.Sum(x => x.UnsubscribedCount),
                FirstEmailSent = _mockData.Min(x => x.FirstEmailSent),
                LastEmailSent = _mockData.Max(x => x.LastEmailSent)
            };
        }

        public async Task<IEnumerable<string>> GetStrategyNamesAsync()
        {
            _logger.LogInformation("Mock: Getting strategy names");
            
            await Task.Delay(50); // Simulate async operation
            
            return _mockData.Select(x => x.StrategyName).ToList();
        }

        public async Task<(IEnumerable<EmailTriggerReportDto> Reports, int TotalCount)> GetEmailTriggerReportsFilteredAsync(EmailTriggerReportFilterDto filter)
        {
            _logger.LogInformation("Mock: Getting filtered email trigger reports");
            
            await Task.Delay(100); // Simulate async operation
            
            var filteredData = _mockData.AsQueryable();

            // Apply strategy name filter
            if (!string.IsNullOrWhiteSpace(filter.StrategyName))
            {
                filteredData = filteredData.Where(x => x.StrategyName.Contains(filter.StrategyName, StringComparison.OrdinalIgnoreCase));
            }

            // Apply date range filters
            if (filter.FirstEmailSentFrom.HasValue)
            {
                filteredData = filteredData.Where(x => x.FirstEmailSent >= filter.FirstEmailSentFrom.Value);
            }            if (filter.FirstEmailSentTo.HasValue)
            {
                filteredData = filteredData.Where(x => x.FirstEmailSent <= filter.FirstEmailSentTo.Value);
            }

            // Apply numeric filters
            if (filter.MinTotalEmails.HasValue)
            {
                filteredData = filteredData.Where(x => x.TotalEmails >= filter.MinTotalEmails.Value);
            }

            if (filter.MaxTotalEmails.HasValue)
            {
                filteredData = filteredData.Where(x => x.TotalEmails <= filter.MaxTotalEmails.Value);
            }

            if (filter.MinDeliveredCount.HasValue)
            {
                filteredData = filteredData.Where(x => x.DeliveredCount >= filter.MinDeliveredCount.Value);
            }

            if (filter.MinOpenedCount.HasValue)
            {
                filteredData = filteredData.Where(x => x.OpenedCount >= filter.MinOpenedCount.Value);
            }

            if (filter.MinClickedCount.HasValue)
            {
                filteredData = filteredData.Where(x => x.ClickedCount >= filter.MinClickedCount.Value);
            }            // Apply sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                var isDescending = filter.SortDirection?.ToLower() == "desc";
                
                switch (filter.SortBy.ToLower())
                {
                    case "strategyname":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.StrategyName) : filteredData.OrderBy(x => x.StrategyName);
                        break;
                    case "totalemails":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.TotalEmails) : filteredData.OrderBy(x => x.TotalEmails);
                        break;
                    case "deliveredcount":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.DeliveredCount) : filteredData.OrderBy(x => x.DeliveredCount);
                        break;
                    case "bouncedcount":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.BouncedCount) : filteredData.OrderBy(x => x.BouncedCount);
                        break;
                    case "openedcount":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.OpenedCount) : filteredData.OrderBy(x => x.OpenedCount);
                        break;
                    case "clickedcount":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.ClickedCount) : filteredData.OrderBy(x => x.ClickedCount);
                        break;
                    case "firstemailsent":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.FirstEmailSent) : filteredData.OrderBy(x => x.FirstEmailSent);
                        break;
                    case "lastemailsent":
                        filteredData = isDescending ? filteredData.OrderByDescending(x => x.LastEmailSent) : filteredData.OrderBy(x => x.LastEmailSent);
                        break;
                    default:
                        filteredData = filteredData.OrderBy(x => x.StrategyName);
                        break;
                }
            }

            var totalCount = filteredData.Count();
            var offset = (filter.PageNumber - 1) * filter.PageSize;
            
            var pagedResults = filteredData
                .Skip(offset)
                .Take(filter.PageSize)
                .ToList();

            _logger.LogInformation("Mock: Returning {ResultCount} filtered results out of {TotalCount} total", pagedResults.Count, totalCount);
            
            return (pagedResults, totalCount);
        }

        private List<EmailTriggerReportDto> GenerateMockData()
        {
            var baseDate = DateTime.UtcNow.AddDays(-30);
            
            return new List<EmailTriggerReportDto>
            {
                new EmailTriggerReportDto
                {
                    StrategyName = "Welcome Series",
                    TotalEmails = 1500,
                    DeliveredCount = 1425,
                    BouncedCount = 75,
                    OpenedCount = 712,
                    ClickedCount = 285,
                    ComplainedCount = 5,
                    UnsubscribedCount = 12,
                    FirstEmailSent = baseDate,
                    LastEmailSent = DateTime.UtcNow.AddHours(-2)
                },
                new EmailTriggerReportDto
                {
                    StrategyName = "Promotional Campaign",
                    TotalEmails = 2800,
                    DeliveredCount = 2664,
                    BouncedCount = 136,
                    OpenedCount = 1065,
                    ClickedCount = 532,
                    ComplainedCount = 8,
                    UnsubscribedCount = 23,
                    FirstEmailSent = baseDate.AddDays(5),
                    LastEmailSent = DateTime.UtcNow.AddHours(-1)
                },
                new EmailTriggerReportDto
                {
                    StrategyName = "Newsletter Monthly",
                    TotalEmails = 5200,
                    DeliveredCount = 4940,
                    BouncedCount = 260,
                    OpenedCount = 1976,
                    ClickedCount = 494,
                    ComplainedCount = 12,
                    UnsubscribedCount = 35,
                    FirstEmailSent = baseDate.AddDays(10),
                    LastEmailSent = DateTime.UtcNow.AddHours(-3)
                },
                new EmailTriggerReportDto
                {
                    StrategyName = "Abandoned Cart",
                    TotalEmails = 920,
                    DeliveredCount = 874,
                    BouncedCount = 46,
                    OpenedCount = 437,
                    ClickedCount = 175,
                    ComplainedCount = 3,
                    UnsubscribedCount = 8,
                    FirstEmailSent = baseDate.AddDays(7),
                    LastEmailSent = DateTime.UtcNow.AddMinutes(-45)
                },
                new EmailTriggerReportDto
                {
                    StrategyName = "Customer Feedback",
                    TotalEmails = 650,
                    DeliveredCount = 617,
                    BouncedCount = 33,
                    OpenedCount = 309,
                    ClickedCount = 123,
                    ComplainedCount = 2,
                    UnsubscribedCount = 4,
                    FirstEmailSent = baseDate.AddDays(12),
                    LastEmailSent = DateTime.UtcNow.AddHours(-6)
                },
                new EmailTriggerReportDto
                {
                    StrategyName = "Re-engagement",
                    TotalEmails = 1100,
                    DeliveredCount = 1045,
                    BouncedCount = 55,
                    OpenedCount = 418,
                    ClickedCount = 104,                    ComplainedCount = 7,
                    UnsubscribedCount = 18,
                    FirstEmailSent = baseDate.AddDays(15),
                    LastEmailSent = DateTime.UtcNow.AddHours(-4)
                }
            };
        }

        public async Task<EmailTriggerResponseDto> TriggerCampaignAsync(object recipientList, Dictionary<string, object>? parameters)
        {
            await Task.Delay(100); // Simulate processing delay
            
            var campaignId = $"MOCK-{Guid.NewGuid().ToString()[..8]}";
            var recipientCount = 0;
            
            // Simulate recipient count calculation
            if (recipientList is IEnumerable<object> enumerable)
            {
                recipientCount = enumerable.Count();
            }
            else if (recipientList is string)
            {
                // Mock: Random recipient count for demo
                var random = new Random();
                recipientCount = random.Next(50, 500);
            }
            else
            {
                recipientCount = 100; // Default mock count
            }
            
            var explanation = parameters?.GetValueOrDefault("explanation")?.ToString() ?? "Mock email campaign triggered";
            
            return new EmailTriggerResponseDto
            {
                Success = true,
                CampaignId = campaignId,
                RecipientCount = recipientCount,
                Message = $"Mock campaign '{campaignId}' triggered successfully for {recipientCount} recipients. {explanation}",
                TriggeredAt = DateTime.UtcNow
            };
        }
    }
}
