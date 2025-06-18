using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace EmailCampaignReporting.API.Services.NaturalSqlRAG
{
    /// <summary>
    /// Orchestrates the RAG + Gemini SQL workflow and executes the generated SQL.
    /// </summary>
    public class NaturalLanguageSqlQueryOrchestrator
    {
        private readonly INaturalSqlRagService _ragService;
        private readonly GeminiSqlGeneratorService _geminiService;
        private readonly string _connectionString;
        private readonly ILogger<NaturalLanguageSqlQueryOrchestrator> _logger;

        public NaturalLanguageSqlQueryOrchestrator(
            INaturalSqlRagService ragService,
            GeminiSqlGeneratorService geminiService,
            string connectionString,
            ILogger<NaturalLanguageSqlQueryOrchestrator> logger)
        {
            _ragService = ragService;
            _geminiService = geminiService;
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<PaginatedResponse<EmailTriggerReportDto>> RunAsync(
            string userQuery, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // 1. Get RAG context
            var contextItems = await _ragService.GetContextForSqlAsync(userQuery);
            var context = string.Join("\n", contextItems);            // 2. Build prompt for Gemini
            var schema = @"Table: EmailTrigger (et) - Description, IsActive, CommunicationId
Table: EmailOutbox_bak (eo) - EmailOutboxId, CommunicationId, DateCreated
Table: WebhookLogs_bak (es) - EmailOutboxId, StatusId
Table: EmailStatus (st) - StatusId, Status (values: 'delivered', 'bounced', 'failed', 'opened', 'clicked', 'complained', 'unsubscribed')";

            var requiredColumns = @"REQUIRED OUTPUT COLUMNS (must be included in SELECT with exact aliases):
- et.Description AS StrategyName
- COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails
- SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount
- SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount
- SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount
- SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount
- SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount
- SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount
- MIN(eo.DateCreated) AS FirstEmailSent
- MAX(eo.DateCreated) AS LastEmailSent";

            var exampleSql = @"SELECT et.Description AS StrategyName, COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails, SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount, SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount, SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount, SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount, SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount, SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount, MIN(eo.DateCreated) AS FirstEmailSent, MAX(eo.DateCreated) AS LastEmailSent FROM EmailTrigger et LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId WHERE et.IsActive = 1 GROUP BY et.Description ORDER BY et.Description";            var prompt = $@"You are an expert SQL assistant. Generate a SQL SELECT query to answer the user's request.

CRITICAL REQUIREMENTS:
- You MUST include ALL 10 required columns in your SELECT statement with the EXACT aliases shown below
- Use only the tables and columns from the provided schema
- Always use LEFT JOINs to avoid losing data
- Always include 'WHERE et.IsActive = 1' condition
- Always GROUP BY et.Description
- Always include ORDER BY clause (required for pagination)
- Do not include OFFSET/FETCH clauses (pagination will be added automatically)
- Output ONLY the SQL query, no explanations

IMPORTANT: Pay attention to time-based requests:
- If user mentions 'last month', 'past month': ADD 'AND eo.DateCreated >= DATEADD(month, -1, GETDATE())'
- If user mentions 'last week', 'past week': ADD 'AND eo.DateCreated >= DATEADD(week, -1, GETDATE())'
- If user mentions 'last N days': ADD 'AND eo.DateCreated >= DATEADD(day, -N, GETDATE())'
- If user mentions 'recent', 'latest': ADD appropriate date filter

IMPORTANT: Pay attention to performance-based requests:
- If user wants 'high open rates', 'best open': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC
- If user wants 'high click rates': ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC
- If user wants 'low bounce rates': ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC

{requiredColumns}

SCHEMA:
{schema}

EXAMPLE QUERY STRUCTURE:
{exampleSql}

CONTEXT:
{context}

USER REQUEST: {userQuery}

Generate the SQL query now:";// 3. Generate SQL with Gemini
            var rawResponse = await _geminiService.GenerateSqlAsync(prompt, cancellationToken);
            var generatedSql = ExtractSqlFromResponse(rawResponse);
            if (string.IsNullOrWhiteSpace(generatedSql) || !generatedSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Gemini response did not contain valid SQL. Raw response: {Response}", rawResponse);
                throw new InvalidOperationException("Gemini did not return a valid SELECT SQL query.");
            }            // 4. Add ORDER BY and pagination if not present
            if (!generatedSql.Contains("ORDER BY"))
            {
                generatedSql += " ORDER BY et.Description";
            }
            if (!generatedSql.Contains("@Offset"))
            {
                generatedSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            // 5. Execute SQL and map results
            var results = new List<EmailTriggerReportDto>();
            int totalCount = 0;
            int offset = (pageNumber - 1) * pageSize;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM ( {generatedSql} ) AS CountQuery";
            using (var countCommand = new SqlCommand(countSql, connection))
            {
                countCommand.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
                countCommand.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });
                totalCount = (int)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
            }

            // Get actual data
            using (var command = new SqlCommand(generatedSql, connection))
            {
                command.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
                command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
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
            }

            return new PaginatedResponse<EmailTriggerReportDto>
            {
                Items = results,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }        private string ExtractSqlFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return string.Empty;

            // First, clean up the response by removing escaped newlines and trimming
            var cleanResponse = response.Replace("\\n", "\n").Replace("\\u003e", ">").Replace("\\u003c", "<").Trim();

            // Try to extract SQL from code block (```sql ... ```
            var match = System.Text.RegularExpressions.Regex.Match(cleanResponse, @"```sql\s*(.*?)\s*```", 
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Try to extract SQL from generic code block (``` ... ```
            match = System.Text.RegularExpressions.Regex.Match(cleanResponse, @"```\s*(.*?)\s*```", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success)
            {
                var sql = match.Groups[1].Value.Trim();
                if (sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    return sql;
            }

            // Look for SELECT statement in the response
            match = System.Text.RegularExpressions.Regex.Match(cleanResponse, @"\bSELECT\b.*?(?=;|\n\n|\Z)", 
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Value.Trim();

            // Fallback: return the response as-is if it looks like SQL
            if (cleanResponse.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return cleanResponse.Trim();

            return string.Empty;
        }
    }
}
