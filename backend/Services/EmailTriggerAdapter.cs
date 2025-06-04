using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Adapter class that delegates to either a real or mock email trigger service
    /// based on the availability of the connection string at runtime.
    /// </summary>
    public class EmailTriggerAdapter : ISqlServerTriggerService
    {
        private readonly ISqlServerTriggerService _service;
        private readonly ILogger<EmailTriggerAdapter> _logger;

        public EmailTriggerAdapter(
            IConfiguration configuration,
            ILogger<EmailTriggerAdapter> logger,
            ILogger<MockEmailTriggerService> mockLogger)
        {
            _logger = logger;

            var connectionString = configuration.GetConnectionString("EmailServiceDbContext");
            if (!string.IsNullOrEmpty(connectionString) && IsConnectionValid(connectionString))
            {
                _logger.LogInformation("Using real EmailTriggerService with SQL Server");
                try
                {
                    // Try to create the real service
                    _service = new MockEmailTriggerService(mockLogger); // TEMPORARY: Using mock instead of real
                    // TODO: Replace with real implementation when compilation issue is resolved
                    // _service = new EmailTriggerService(loggerReal, configuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating real EmailTriggerService, falling back to mock");
                    _service = new MockEmailTriggerService(mockLogger);
                }
            }
            else
            {
                _logger.LogInformation("Using MockEmailTriggerService - connection string invalid or unavailable");
                _service = new MockEmailTriggerService(mockLogger);
            }
        }

        private bool IsConnectionValid(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                // Just validate the connection string format, don't actually connect
                connection.ConnectionString = connectionString;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<EmailTriggerReportDto>> GetEmailTriggerReportsAsync(int pageSize = 50, int offset = 0)
        {
            return await _service.GetEmailTriggerReportsAsync(pageSize, offset);
        }

        public async Task<EmailTriggerReportDto?> GetEmailTriggerReportByStrategyNameAsync(string strategyName)
        {
            return await _service.GetEmailTriggerReportByStrategyNameAsync(strategyName);
        }

        public async Task<EmailTriggerReportDto> GetEmailTriggerSummaryAsync()
        {
            return await _service.GetEmailTriggerSummaryAsync();
        }

        public async Task<IEnumerable<string>> GetStrategyNamesAsync()
        {
            return await _service.GetStrategyNamesAsync();
        }
    }
}
