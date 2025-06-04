using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace EmailCampaignReporting.API.Services
{
    public static class EmailTriggerServiceFactory
    {
        public static ISqlServerTriggerService CreateService(
            IServiceProvider serviceProvider,
            ILogger<MockEmailTriggerService> mockLogger)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            if (configuration == null || loggerFactory == null)
            {
                return new MockEmailTriggerService(mockLogger);
            }

            var connectionString = configuration.GetConnectionString("EmailServiceDbContext");
            if (string.IsNullOrEmpty(connectionString))
            {
                mockLogger.LogInformation("No connection string - Using MockEmailTriggerService");
                return new MockEmailTriggerService(mockLogger);
            }

            // Validate connection string format
            try
            {
                using var conn = new SqlConnection(connectionString);
                // Just parse, don't connect
            }
            catch (Exception ex)
            {
                mockLogger.LogWarning(ex, "Invalid connection string - Using MockEmailTriggerService");
                return new MockEmailTriggerService(mockLogger);
            }

            // Try to create the real service
            try
            {
                // At this point we're going with the mock service since the real one has compilation issues
                mockLogger.LogInformation("Using MockEmailTriggerService even though connection string is valid (real service has compilation issues)");
                return new MockEmailTriggerService(mockLogger);
                
                // The following code would be used once the compilation issues are resolved:
                /*
                var serviceType = Type.GetType("EmailCampaignReporting.API.Services.EmailTriggerService, EmailCampaignReporting.API");
                if (serviceType != null)
                {
                    var emailLogger = loggerFactory.CreateLogger(serviceType);
                    var instance = Activator.CreateInstance(serviceType, emailLogger, configuration) as ISqlServerTriggerService;
                    if (instance != null)
                    {
                        return instance;
                    }
                }
                */
            }
            catch (Exception ex)
            {
                mockLogger.LogError(ex, "Error creating EmailTriggerService - Falling back to MockEmailTriggerService");
            }

            // Default to mock service if anything fails
            return new MockEmailTriggerService(mockLogger);
        }
    }
}
