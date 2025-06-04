namespace EmailCampaignReporting.API.Models.DTOs
{
    public class EmailTriggerReportDto
    {
        public string StrategyName { get; set; } = string.Empty;
        public int TotalEmails { get; set; }
        public int DeliveredCount { get; set; }
        public int BouncedCount { get; set; }
        public int OpenedCount { get; set; }
        public int ClickedCount { get; set; }
        public int ComplainedCount { get; set; }
        public int UnsubscribedCount { get; set; }
        public DateTime? FirstEmailSent { get; set; }
        public DateTime? LastEmailSent { get; set; }

        // Calculated properties for better reporting
        public decimal DeliveryRate => TotalEmails > 0 ? (decimal)DeliveredCount / TotalEmails * 100 : 0;
        public decimal OpenRate => DeliveredCount > 0 ? (decimal)OpenedCount / DeliveredCount * 100 : 0;
        public decimal ClickRate => DeliveredCount > 0 ? (decimal)ClickedCount / DeliveredCount * 100 : 0;
        public decimal BounceRate => TotalEmails > 0 ? (decimal)BouncedCount / TotalEmails * 100 : 0;
        public decimal ComplaintRate => DeliveredCount > 0 ? (decimal)ComplainedCount / DeliveredCount * 100 : 0;
        public decimal UnsubscribeRate => DeliveredCount > 0 ? (decimal)UnsubscribedCount / DeliveredCount * 100 : 0;
    }
}
