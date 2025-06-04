using Microsoft.Data.SqlClient;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Factory class for creating the appropriate implementation of ISqlServerTriggerService
    /// based on configuration and environment.
    /// </summary>
    public static class EmailTriggerFactory
    {
        public static ISqlServerTriggerService CreateService(
            IServiceProvider serviceProvider,
            ILogger<MockEmailTriggerService> mockLogger)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            if (configuration == null || loggerFactory == null)
            {
                Console.WriteLine("Missing configuration or logger factory - Using MockEmailTriggerService");
                return new MockEmailTriggerService(mockLogger);
            }

            // Check connection string availability
            var connectionString = configuration.GetConnectionString("EmailServiceDbContext");
            Console.WriteLine($"Connection string found: {!string.IsNullOrEmpty(connectionString)}");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("No connection string - Using MockEmailTriggerService");
                return new MockEmailTriggerService(mockLogger);
            }
            
            // Log that we're using the mock service even though connection string is valid
            Console.WriteLine("Using MockEmailTriggerService while real service has compilation issues");
            return new MockEmailTriggerService(mockLogger);
        }
    }
}
