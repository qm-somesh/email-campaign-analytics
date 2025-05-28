namespace EmailCampaignReporting.API.Models
{
    public class EmailList
    {
        public string ListId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalRecipients { get; set; }
        public int ActiveRecipients { get; set; }
        public int BouncedRecipients { get; set; }
        public int UnsubscribedRecipients { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }
    }
}
