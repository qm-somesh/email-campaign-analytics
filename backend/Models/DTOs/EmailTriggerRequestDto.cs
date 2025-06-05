using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// Request DTO for triggering email campaigns via natural language
    /// </summary>
    public class EmailTriggerRequestDto
    {
        /// <summary>
        /// Natural language command for triggering email campaign
        /// </summary>
        [Required]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Optional context for the command
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Optional parameters for the campaign
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// Response DTO for email campaign trigger operations
    /// </summary>
    public class EmailTriggerResponseDto
    {
        /// <summary>
        /// Whether the trigger operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The campaign ID that was triggered
        /// </summary>
        public string? CampaignId { get; set; }

        /// <summary>
        /// Number of recipients targeted
        /// </summary>
        public int RecipientCount { get; set; }

        /// <summary>
        /// The SQL query used to select recipients
        /// </summary>
        public string? GeneratedSql { get; set; }

        /// <summary>
        /// Explanation of what was triggered
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Timestamp when the trigger was initiated
        /// </summary>
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    }
}
