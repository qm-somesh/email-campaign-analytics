using EmailCampaignReporting.API.Configuration;
using Microsoft.Extensions.Options;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Factory for creating LLM service instances with fallback to mock service
    /// </summary>
    public static class LLMServiceFactory
    {        /// <summary>
        /// Create an LLM service instance, falling back to mock service if real service fails
        /// </summary>
        public static ILLMService CreateLLMService(
            IServiceProvider serviceProvider,
            ILogger<EmailCampaignReporting.API.Services.LLMService> logger)
        {
            try
            {
                var options = serviceProvider.GetService<IOptions<LLMOptions>>();
                var campaignQueryService = serviceProvider.GetRequiredService<ICampaignQueryService>();
                
                if (options?.Value?.ModelPath != null && 
                    !options.Value.ModelPath.Contains("path/to/your") && 
                    File.Exists(options.Value.ModelPath))
                {
                    // Try to create real LLM service
                    return new EmailCampaignReporting.API.Services.LLMService(options, logger, campaignQueryService);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create real LLM service, using mock service");
            }

            // Fallback to mock service
            var mockLogger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<MockLLMService>();
            return new MockLLMService(mockLogger);
        }
    }
}
