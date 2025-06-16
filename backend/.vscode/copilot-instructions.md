# GitHub Copilot Instructions for EmailCampaignReporting Backend

## Project Overview
This is a .NET 9.0 Web API project for Email Campaign Reporting with Natural Language Query Support using LLM integration (LLAMA and Gemini 1.5 Flash with RAG architecture).

## Default Commands

### Build Command
**ALWAYS use this command format for building the project:**
```
cd d:\Dev\EmailCampaignReporting\backend ; dotnet build backend.sln
```

**DO NOT use:** `cd d:\Dev\EmailCampaignReporting\backend && dotnet build`

### Other Common Commands
- **Run the application:** `cd d:\Dev\EmailCampaignReporting\backend ; dotnet run --project EmailCampaignReporting.API.csproj`
- **Test the application:** `cd d:\Dev\EmailCampaignReporting\backend ; dotnet test backend.sln`
- **Restore packages:** `cd d:\Dev\EmailCampaignReporting\backend ; dotnet restore backend.sln`
- **Clean build artifacts:** `cd d:\Dev\EmailCampaignReporting\backend ; dotnet clean backend.sln`

## Project Structure Guidelines

### Key Directories
- `Controllers/` - API controllers
- `Services/` - Business logic and external service integrations
  - `Services/LLM/` - LLM provider abstractions and implementations
  - `Services/LLM/Abstractions/` - Interfaces for LLM services
  - `Services/LLM/Providers/` - Concrete LLM provider implementations
  - `Services/LLM/RAG/` - Retrieval-Augmented Generation services
- `Models/DTOs/` - Data Transfer Objects
- `Configuration/` - Configuration classes

### Naming Conventions
- Use PascalCase for classes, methods, and properties
- Use camelCase for local variables and parameters
- Prefix interfaces with `I` (e.g., `ILLMProvider`)
- Suffix service implementations with `Service` (e.g., `GeminiLLMProvider`)

## Architecture Patterns

### LLM Provider System
- Use dependency injection for all services
- Implement factory pattern for LLM provider selection
- Follow SOLID principles, especially Single Responsibility and Dependency Inversion
- All LLM providers must implement `ILLMProvider` interface
- Use configuration-driven provider selection

### Error Handling
- Always wrap LLM calls in try-catch blocks
- Log errors with appropriate log levels
- Return meaningful error messages in API responses
- Implement fallback mechanisms for LLM failures

### Configuration
- Use strongly-typed configuration classes
- Store sensitive information (API keys) in user secrets or environment variables
- Support multiple LLM providers through configuration

## Code Quality Standards

### Documentation
- Add XML documentation comments for all public APIs
- Include parameter descriptions and return value explanations
- Document any complex business logic

### Testing
- Create unit tests for all service classes
- Mock external dependencies (LLM APIs, databases)
- Test both success and failure scenarios

### Performance
- Use async/await pattern for all I/O operations
- Implement proper disposal patterns for resources
- Consider caching for frequently accessed data

## Specific Guidelines for LLM Integration

### Provider Implementation
- Each LLM provider should handle its own authentication
- Implement retry logic with exponential backoff
- Add proper timeout handling
- Track token usage and costs

### RAG Implementation
- Keep vector store operations separate from LLM calls
- Implement relevance scoring for retrieved documents
- Limit context size to stay within model limits

## Common Issues and Solutions

### Build Issues
- If build fails, try: `cd d:\Dev\EmailCampaignReporting\backend ; dotnet clean ; dotnet restore ; dotnet build`
- Check for missing NuGet packages
- Verify .NET 9.0 SDK is installed

### LLM Integration Issues
- Verify API keys are properly configured
- Check network connectivity for external API calls
- Monitor rate limits and quotas

## File Patterns to Follow

### Service Registration (Program.cs)
```csharp
// Configure options
builder.Services.Configure<LLMProviderOptions>(
    builder.Configuration.GetSection(LLMProviderOptions.SectionName));

// Register services
builder.Services.AddScoped<ILLMServiceFactory, LLMServiceFactory>();
builder.Services.AddScoped<IEmailTriggerFilterService, EnhancedEmailTriggerFilterService>();
```

### Configuration Classes
```csharp
public class SomeOptions
{
    public const string SectionName = "SomeSection";
    // Properties with default values
}
```

### Service Implementations
```csharp
public class SomeService : ISomeService, IDisposable
{
    private readonly ILogger<SomeService> _logger;
    
    public SomeService(ILogger<SomeService> logger)
    {
        _logger = logger;
    }
    
    public void Dispose()
    {
        // Cleanup resources
    }
}
```

Remember: Always prioritize maintainability, testability, and clear separation of concerns in this codebase.
