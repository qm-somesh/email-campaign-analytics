using System.Text.Json.Serialization;

namespace EmailCampaignReporting.API.Models
{
    public class EmailTrigger
    {
        public long CommunicationId { get; set; }
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Strategy Name - Alias for Description field
        /// </summary>
        [JsonPropertyName("strategyName")]
        public string StrategyName 
        { 
            get => Description; 
            set => Description = value; 
        }
        
        public string TemplateIdentifier { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
