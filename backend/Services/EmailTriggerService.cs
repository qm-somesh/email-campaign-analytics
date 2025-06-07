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
                        LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId
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
                        LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId
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
                        LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId
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

        public async Task<(IEnumerable<EmailTriggerReportDto> Reports, int TotalCount)> GetEmailTriggerReportsFilteredAsync(EmailTriggerReportFilterDto filter)
        {
            try
            {
                // Build the WHERE clause based on filters
                var whereConditions = new List<string> { "et.IsActive = 1" };
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(filter.StrategyName))
                {
                    whereConditions.Add("et.Description LIKE @StrategyName");
                    parameters.Add(new SqlParameter("@StrategyName", SqlDbType.NVarChar, 100) { Value = $"%{filter.StrategyName}%" });
                }                // Build the HAVING clause for aggregate filters
                var havingConditions = new List<string>();

                // Date filters must go in HAVING clause since they involve aggregate functions
                if (filter.FirstEmailSentFrom.HasValue)
                {
                    havingConditions.Add("MIN(eo.DateCreated) >= @FirstEmailSentFrom");
                    parameters.Add(new SqlParameter("@FirstEmailSentFrom", SqlDbType.DateTime) { Value = filter.FirstEmailSentFrom.Value });
                }

                if (filter.FirstEmailSentTo.HasValue)
                {
                    havingConditions.Add("MIN(eo.DateCreated) <= @FirstEmailSentTo");
                    parameters.Add(new SqlParameter("@FirstEmailSentTo", SqlDbType.DateTime) { Value = filter.FirstEmailSentTo.Value });
                }

                if (filter.MinTotalEmails.HasValue)
                {
                    havingConditions.Add("COUNT(DISTINCT eo.EmailOutboxId) >= @MinTotalEmails");
                    parameters.Add(new SqlParameter("@MinTotalEmails", SqlDbType.Int) { Value = filter.MinTotalEmails.Value });
                }

                if (filter.MaxTotalEmails.HasValue)
                {
                    havingConditions.Add("COUNT(DISTINCT eo.EmailOutboxId) <= @MaxTotalEmails");
                    parameters.Add(new SqlParameter("@MaxTotalEmails", SqlDbType.Int) { Value = filter.MaxTotalEmails.Value });
                }

                if (filter.MinDeliveredCount.HasValue)
                {
                    havingConditions.Add("SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) >= @MinDeliveredCount");
                    parameters.Add(new SqlParameter("@MinDeliveredCount", SqlDbType.Int) { Value = filter.MinDeliveredCount.Value });
                }

                if (filter.MinOpenedCount.HasValue)
                {
                    havingConditions.Add("SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) >= @MinOpenedCount");
                    parameters.Add(new SqlParameter("@MinOpenedCount", SqlDbType.Int) { Value = filter.MinOpenedCount.Value });
                }

                if (filter.MinClickedCount.HasValue)
                {
                    havingConditions.Add("SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) >= @MinClickedCount");
                    parameters.Add(new SqlParameter("@MinClickedCount", SqlDbType.Int) { Value = filter.MinClickedCount.Value });
                }

                // Build ORDER BY clause
                var validSortFields = new Dictionary<string, string>
                {
                    { "strategyname", "et.Description" },
                    { "totalemails", "COUNT(DISTINCT eo.EmailOutboxId)" },
                    { "deliveredcount", "SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END)" },
                    { "bouncedcount", "SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END)" },
                    { "openedcount", "SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END)" },
                    { "clickedcount", "SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END)" },
                    { "firstemailsent", "MIN(eo.DateCreated)" },
                    { "lastemailsent", "MAX(eo.DateCreated)" }
                };                var sortField = validSortFields.ContainsKey(filter.SortBy?.ToLower() ?? "")
                    ? validSortFields[filter.SortBy?.ToLower() ?? ""]
                    : "et.Description";

                var sortDirection = filter.SortDirection?.ToLower() == "desc" ? "DESC" : "ASC";                // First, get the total count - must include all aggregate fields in subquery for HAVING clause
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM (
                        SELECT 
                            et.Description,
                            COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
                            SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
                            SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount,
                            SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount,
                            MIN(eo.DateCreated) AS FirstEmailSent,
                            MAX(eo.DateCreated) AS LastEmailSent
                        FROM EmailTrigger et
                            LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
                            LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId
                            LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId
                        WHERE {string.Join(" AND ", whereConditions)}
                        GROUP BY et.Description
                        {(havingConditions.Any() ? "HAVING " + string.Join(" AND ", havingConditions) : "")}
                    ) AS CountQuery";

                // Debug: Log the generated SQL
                _logger.LogDebug("Generated Count SQL: {CountSql}", countSql);
                _logger.LogDebug("WHERE conditions: {WhereConditions}", string.Join(" AND ", whereConditions));
                _logger.LogDebug("HAVING conditions: {HavingConditions}", string.Join(" AND ", havingConditions));                // Build the main query
                var mainSql = $@"
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
                        LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
                        LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId
                        LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId
                    WHERE 
                        {string.Join(" AND ", whereConditions)}
                    GROUP BY 
                        et.Description
                    {(havingConditions.Any() ? "HAVING " + string.Join(" AND ", havingConditions) : "")}
                    ORDER BY 
                        {sortField} {sortDirection}
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                // Debug: Log the generated main SQL
                _logger.LogDebug("Generated Main SQL: {MainSql}", mainSql);

                var results = new List<EmailTriggerReportDto>();
                int totalCount = 0;
                int offset = (filter.PageNumber - 1) * filter.PageSize;

                using var connection = new SqlConnection(_sqlServerOptions.ConnectionString);
                await connection.OpenAsync();

                // Get total count
                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    countCommand.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;
                    foreach (var param in parameters)
                    {
                        countCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.SqlDbType, param.Size) { Value = param.Value });
                    }
                    totalCount = (int)(await countCommand.ExecuteScalarAsync() ?? 0);
                }

                // Get the actual data
                using (var command = new SqlCommand(mainSql, connection))
                {
                    command.CommandTimeout = _sqlServerOptions.CommandTimeoutSeconds;
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new SqlParameter(param.ParameterName, param.SqlDbType, param.Size) { Value = param.Value });
                    }                    command.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
                    command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = filter.PageSize });

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
                }                _logger.LogInformation("Retrieved {Count} filtered email trigger reports out of {TotalCount} total", results.Count, totalCount);
                return (results, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered email trigger reports");                throw;
            }
        }

        public async Task<EmailTriggerResponseDto> TriggerCampaignAsync(object recipientList, Dictionary<string, object>? parameters)
        {
            try
            {
                _logger.LogInformation("Triggering email campaign with recipient list and parameters");
                
                // Simulate async operation
                await Task.Delay(50);
                
                // In a real implementation, this would:
                // 1. Process the recipient list
                // 2. Create email records in EmailOutbox
                // 3. Trigger the actual email sending process
                // 4. Return the campaign details
                
                var campaignId = Guid.NewGuid().ToString();
                var recipientCount = 0;
                
                // Try to determine recipient count from various input types
                if (recipientList is IEnumerable<object> enumerable)
                {
                    recipientCount = enumerable.Count();
                }
                else if (recipientList is string sqlResult && !string.IsNullOrEmpty(sqlResult))
                {
                    // Mock recipient count based on SQL result length (simplified)
                    recipientCount = Math.Max(1, sqlResult.Length / 100);
                }
                
                // Extract explanation from parameters
                var explanation = parameters?.GetValueOrDefault("explanation")?.ToString() ?? "Email campaign triggered via natural language";
                
                _logger.LogInformation("Campaign {CampaignId} triggered for {RecipientCount} recipients", campaignId, recipientCount);
                
                return new EmailTriggerResponseDto
                {
                    Success = true,
                    CampaignId = campaignId,
                    RecipientCount = recipientCount,
                    Message = $"Successfully triggered campaign '{campaignId}' for {recipientCount} recipients. {explanation}",
                    TriggeredAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering email campaign");
                return new EmailTriggerResponseDto
                {
                    Success = false,
                    Error = $"Failed to trigger campaign: {ex.Message}",
                    TriggeredAt = DateTime.UtcNow
                };
            }
        }
    }
}