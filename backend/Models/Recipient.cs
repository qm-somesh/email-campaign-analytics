namespace EmailCampaignReporting.API.Models
{
    public class Recipient
    {
        public string RecipientId { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastEngagementAt { get; set; }
        public string? Location { get; set; }
        public string? DeviceType { get; set; }
        public string? PreferredLanguage { get; set; }
        public DateTime? SubscribedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public string? UnsubscribeReason { get; set; }
        public int TotalOpens { get; set; }
        public int TotalClicks { get; set; }
        public int TotalBounces { get; set; }
        public string? CustomFields { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }
    }
}
