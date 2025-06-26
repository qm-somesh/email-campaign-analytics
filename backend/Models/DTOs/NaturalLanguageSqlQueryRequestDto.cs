using System.ComponentModel.DataAnnotations;

namespace EmailCampaignReporting.API.Models.DTOs
{
    /// <summary>
    /// DTO for natural language SQL query requests
    /// </summary>
    public class NaturalLanguageSqlQueryRequestDto
    {
        [Required]
        public string Query { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
