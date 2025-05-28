namespace EmailCampaignReporting.API.Configuration
{
    public class BigQueryOptions
    {
        public const string SectionName = "BigQuery";
        
        public string ProjectId { get; set; } = string.Empty;
        public string DatasetId { get; set; } = string.Empty;
        public string CredentialsPath { get; set; } = string.Empty;
          // Table names
        public string EmailOutboxTable { get; set; } = "EmailOutbox";
        public string EmailStatusTable { get; set; } = "EmailStatus";
    }
}
