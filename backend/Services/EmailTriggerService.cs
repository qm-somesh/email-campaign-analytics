using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace EmailCampaignReporting.API.Services
{
    public class EmailTriggerService : ISqlServerTriggerService
    {
        private readonly SqlServerOptions _sqlServerOptions;
        private readonly ILogger<EmailTriggerService> _logger;

        public EmailTriggerService(IOptions<SqlServerOptions> sqlServerOptions, ILogger<EmailTriggerService> logger)
        {
            _sqlServerOptions = sqlServerOptions.Value;
            _logger = logger;
        }

        public async Task<IEnumerable<EmailTriggerReportDto>> GetEmailTriggerReportsAsync(int pageSize = 50, int offset = 0)
        {
            try
            {                const string sql = @"
                    SELECT 
                        et.Description AS StrategyName,
                        COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
                        SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
                        SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount,
                        SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount,
                        SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount,
                        SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount,
                        SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount,
                        MIN(eo.DateCreated) AS FirstEmailSent,
                        MAX(eo.DateCreated) AS LastEmailSent
                    FROM 
                        EmailTrigger et
                        LEFT JOIN EmailOutbox eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs es ON eo.EmailOutboxId = es.EmailOutboxId
                        LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId
                    WHERE 
                        et.IsActive = 1
                    GROUP BY 
                        et.Description
                    ORDER BY 
                        et.Description
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var formattedSql = string.Format(sql, _sqlServerOptions.EmailOutboxTable, _sqlServerOptions.EmailStatusTable);
                var results = new List<EmailTriggerReportDto>();

                using var connection = new SqlConnection(_sqlServerOptions.ConnectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(formattedSql, connection);
                command.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;
                command.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new EmailTriggerReportDto
                    {
                        StrategyName = reader.GetString("StrategyName"),
                        TotalEmails = reader.GetInt32("TotalEmails"),
                        DeliveredCount = reader.GetInt32("DeliveredCount"),
                        BouncedCount = reader.GetInt32("BouncedCount"),
                        OpenedCount = reader.GetInt32("OpenedCount"),
                        ClickedCount = reader.GetInt32("ClickedCount"),
                        ComplainedCount = reader.GetInt32("ComplainedCount"),
                        UnsubscribedCount = reader.GetInt32("UnsubscribedCount"),
                        FirstEmailSent = reader.IsDBNull("FirstEmailSent") ? null : reader.GetDateTime("FirstEmailSent"),
                        LastEmailSent = reader.IsDBNull("LastEmailSent") ? null : reader.GetDateTime("LastEmailSent")
                    });
                }

                _logger.LogInformation("Retrieved {Count} email trigger reports", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email trigger reports");
                throw;
            }
        }

        public async Task<EmailTriggerReportDto?> GetEmailTriggerReportByStrategyNameAsync(string strategyName)
        {
            try
            {                const string sql = @"
                    SELECT 
                        et.Description AS StrategyName,
                        COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
                        SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
                        SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount,
                        SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount,
                        SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount,
                        SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount,
                        SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount,
                        MIN(eo.DateCreated) AS FirstEmailSent,
                        MAX(eo.DateCreated) AS LastEmailSent
                    FROM 
                        EmailTrigger et
                        LEFT JOIN EmailOutbox eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs es ON eo.EmailOutboxId = es.EmailOutboxId
                        LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId
                    WHERE 
                        et.Description = @StrategyName 
                        AND et.IsActive = 1
                    GROUP BY 
                        et.Description
                    ORDER BY 
                        et.Description";

                var formattedSql = string.Format(sql, _sqlServerOptions.EmailOutboxTable, _sqlServerOptions.EmailStatusTable);

                using var connection = new SqlConnection(_sqlServerOptions.ConnectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(formattedSql, connection);
                command.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;
                command.Parameters.Add("@StrategyName", SqlDbType.NVarChar, 100).Value = strategyName;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var result = new EmailTriggerReportDto
                    {
                        StrategyName = reader.GetString("StrategyName"),
                        TotalEmails = reader.GetInt32("TotalEmails"),
                        DeliveredCount = reader.GetInt32("DeliveredCount"),
                        BouncedCount = reader.GetInt32("BouncedCount"),
                        OpenedCount = reader.GetInt32("OpenedCount"),
                        ClickedCount = reader.GetInt32("ClickedCount"),
                        ComplainedCount = reader.GetInt32("ComplainedCount"),
                        UnsubscribedCount = reader.GetInt32("UnsubscribedCount"),
                        FirstEmailSent = reader.IsDBNull("FirstEmailSent") ? null : reader.GetDateTime("FirstEmailSent"),
                        LastEmailSent = reader.IsDBNull("LastEmailSent") ? null : reader.GetDateTime("LastEmailSent")
                    };

                    _logger.LogInformation("Retrieved email trigger report for strategy: {StrategyName}", strategyName);
                    return result;
                }

                _logger.LogWarning("No email trigger report found for strategy: {StrategyName}", strategyName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email trigger report for strategy: {StrategyName}", strategyName);
                throw;
            }
        }

        public async Task<EmailTriggerReportDto> GetEmailTriggerSummaryAsync()
        {
            try
            {                const string sql = @"
                    SELECT 
                        'Summary' AS StrategyName,
                        COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
                        SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
                        SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount,
                        SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount,
                        SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount,
                        SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount,
                        SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount,
                        MIN(eo.DateCreated) AS FirstEmailSent,
                        MAX(eo.DateCreated) AS LastEmailSent
                    FROM 
                        EmailTrigger et
                        LEFT JOIN EmailOutbox eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs es ON eo.EmailOutboxId = es.EmailOutboxId
                        LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId
                    WHERE 
                        et.IsActive = 1";

                var formattedSql = string.Format(sql, _sqlServerOptions.EmailOutboxTable, _sqlServerOptions.EmailStatusTable);

                using var connection = new SqlConnection(_sqlServerOptions.ConnectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(formattedSql, connection);
                command.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var result = new EmailTriggerReportDto
                    {
                        StrategyName = reader.GetString("StrategyName"),
                        TotalEmails = reader.GetInt32("TotalEmails"),
                        DeliveredCount = reader.GetInt32("DeliveredCount"),
                        BouncedCount = reader.GetInt32("BouncedCount"),
                        OpenedCount = reader.GetInt32("OpenedCount"),
                        ClickedCount = reader.GetInt32("ClickedCount"),
                        ComplainedCount = reader.GetInt32("ComplainedCount"),
                        UnsubscribedCount = reader.GetInt32("UnsubscribedCount"),
                        FirstEmailSent = reader.IsDBNull("FirstEmailSent") ? null : reader.GetDateTime("FirstEmailSent"),
                        LastEmailSent = reader.IsDBNull("LastEmailSent") ? null : reader.GetDateTime("LastEmailSent")
                    };

                    _logger.LogInformation("Retrieved email trigger summary");
                    return result;
                }

                _logger.LogWarning("No email trigger summary data found");
                return new EmailTriggerReportDto { StrategyName = "Summary" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email trigger summary");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetStrategyNamesAsync()
        {
            try
            {                const string sql = @"
                    SELECT DISTINCT 
                        et.Description AS StrategyName
                    FROM 
                        EmailTrigger et
                    WHERE 
                        et.IsActive = 1
                    ORDER BY 
                        et.Description";

                var formattedSql = string.Format(sql, _sqlServerOptions.EmailOutboxTable);
                var results = new List<string>();

                using var connection = new SqlConnection(_sqlServerOptions.ConnectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(formattedSql, connection);
                command.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetString("StrategyName"));
                }

                _logger.LogInformation("Retrieved {Count} strategy names", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategy names");
                throw;
            }
        }
    }
}