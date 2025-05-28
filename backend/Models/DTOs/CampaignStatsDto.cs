namespace EmailCampaignReporting.API.Models.DTOs
{
    public class CampaignStatsDto
    {
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LaunchedAt { get; set; }
        public int TotalRecipients { get; set; }
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int OpenedCount { get; set; }
        public int ClickedCount { get; set; }
        public int BouncedCount { get; set; }
        public int UnsubscribedCount { get; set; }
        public int ComplaintsCount { get; set; }
        
        // Calculated metrics
        public decimal DeliveryRate => TotalRecipients > 0 ? (decimal)DeliveredCount / TotalRecipients * 100 : 0;
        public decimal OpenRate => DeliveredCount > 0 ? (decimal)OpenedCount / DeliveredCount * 100 : 0;
        public decimal ClickRate => DeliveredCount > 0 ? (decimal)ClickedCount / DeliveredCount * 100 : 0;
        public decimal ClickToOpenRate => OpenedCount > 0 ? (decimal)ClickedCount / OpenedCount * 100 : 0;
        public decimal BounceRate => TotalRecipients > 0 ? (decimal)BouncedCount / TotalRecipients * 100 : 0;
        public decimal UnsubscribeRate => DeliveredCount > 0 ? (decimal)UnsubscribedCount / DeliveredCount * 100 : 0;
        public decimal ComplaintRate => DeliveredCount > 0 ? (decimal)ComplaintsCount / DeliveredCount * 100 : 0;
    }
}
