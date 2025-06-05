namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Response DTO for email campaign trigger operations
    /// </summary>
    public class EmailTriggerResponseDto
    {
        /// <summary>
        /// Indicates if the trigger operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The campaign ID that was triggered
        /// </summary>
        public string? CampaignId { get; set; }

        /// <summary>
        /// Campaign trigger ID for tracking
        /// </summary>
        public string? CampaignTriggerId { get; set; }

        /// <summary>
        /// Number of recipients targeted in the campaign
        /// </summary>
        public int RecipientsCount { get; set; }

        /// <summary>
        /// Number of recipients targeted (alternative name for compatibility)
        /// </summary>
        public int RecipientCount { get; set; }

        /// <summary>
        /// Strategy name used for the campaign
        /// </summary>
        public string? StrategyName { get; set; }

        /// <summary>
        /// The SQL query used to select recipients
        /// </summary>
        public string? GeneratedSql { get; set; }

        /// <summary>
        /// Explanation of what was triggered
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Estimated delivery time for the campaign
        /// </summary>
        public DateTime? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Any error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error message if operation failed (alternative name for compatibility)
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Timestamp when the trigger was initiated
        /// </summary>
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;        /// <summary>
        /// Additional metadata about the triggered campaign
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
