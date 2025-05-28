namespace EmailCampaignReporting.API.Models.DTOs
{    public class DashboardMetricsDto
    {
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public int TotalRecipients { get; set; }
        public int ActiveRecipients { get; set; }
        public int TotalEmailsSent { get; set; }
        public decimal OverallDeliveryRate { get; set; }
        public decimal OverallOpenRate { get; set; }
        public decimal OverallClickRate { get; set; }
        public decimal OverallBounceRate { get; set; }
        public int RecentCampaignsCount { get; set; }
        public int RecentEventsCount { get; set; }
        public List<CampaignPerformanceDto> TopPerformingCampaigns { get; set; } = new();
        public List<DailyMetricsDto> DailyMetrics { get; set; } = new();
    }

    public class CampaignPerformanceDto
    {
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal OpenRate { get; set; }
        public decimal ClickRate { get; set; }
        public int TotalRecipients { get; set; }
    }

    public class DailyMetricsDto
    {
        public DateTime Date { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsDelivered { get; set; }
        public int EmailsOpened { get; set; }
        public int EmailsClicked { get; set; }
        public int EmailsBounced { get; set; }
    }
}
