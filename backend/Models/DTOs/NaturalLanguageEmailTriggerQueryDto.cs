using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// DTO for natural language email trigger report queries
    /// </summary>
    public class NaturalLanguageEmailTriggerQueryDto
    {
        /// <summary>
        /// Natural language query describing the desired email trigger report filtering
        /// </summary>
        /// <example>Show me campaigns with more than 1000 emails sent last month</example>
        [Required(ErrorMessage = "Query is required")]
        [StringLength(1000, ErrorMessage = "Query cannot exceed 1000 characters")]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        /// <example>50</example>
        [Range(1, 1000, ErrorMessage = "Page size must be between 1 and 1000")]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Whether to include debug information in the response
        /// </summary>
        /// <example>false</example>
        public bool IncludeDebugInfo { get; set; } = false;
    }
}
