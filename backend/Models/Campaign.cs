namespace EmailCampaignReporting.API.Models
{
    public class Campaign
    {
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LaunchedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public int TotalRecipients { get; set; }
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int OpenedCount { get; set; }
        public int ClickedCount { get; set; }
        public int BouncedCount { get; set; }
        public int UnsubscribedCount { get; set; }
        public int ComplaintsCount { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }
    }
}
