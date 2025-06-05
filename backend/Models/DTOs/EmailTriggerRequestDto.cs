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
}
