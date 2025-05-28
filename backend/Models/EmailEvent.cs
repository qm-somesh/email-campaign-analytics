namespace EmailCampaignReporting.API.Models
{
    public class EmailEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? EmailAddress { get; set; }
        public string? Subject { get; set; }
        public string? Reason { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public string? DeviceType { get; set; }
        public string? ClickUrl { get; set; }
        public string? AdditionalData { get; set; }
    }
}
