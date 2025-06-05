namespace EmailCampaignReporting.API.Models.DTOs
{
    public class EmailTriggerReportFilterDto
    {
        /// <summary>
        /// Filter by strategy name (partial match)
        /// </summary>
        public string? StrategyName { get; set; }        /// <summary>
        /// Filter by minimum first email sent date
        /// </summary>
        public DateTime? FirstEmailSentFrom { get; set; }

        /// <summary>
        /// Filter by maximum first email sent date
        /// </summary>
        public DateTime? FirstEmailSentTo { get; set; }

        /// <summary>
        /// Filter by minimum total emails count
        /// </summary>
        public int? MinTotalEmails { get; set; }

        /// <summary>
        /// Filter by maximum total emails count
        /// </summary>
        public int? MaxTotalEmails { get; set; }

        /// <summary>
        /// Filter by minimum delivered count
        /// </summary>
        public int? MinDeliveredCount { get; set; }

        /// <summary>
        /// Filter by minimum opened count
        /// </summary>
        public int? MinOpenedCount { get; set; }

        /// <summary>
        /// Filter by minimum clicked count
        /// </summary>
        public int? MinClickedCount { get; set; }

        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Sort field name (e.g., "StrategyName", "TotalEmails", "FirstEmailSent")
        /// </summary>
        public string? SortBy { get; set; } = "StrategyName";

        /// <summary>
        /// Sort direction (asc or desc)
        /// </summary>
        public string? SortDirection { get; set; } = "asc";
    }
}
