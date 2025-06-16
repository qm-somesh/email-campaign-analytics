using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;
using EmailCampaignReporting.API.Services.LLM.Abstractions;
using EmailCampaignReporting.API.Services.LLM;
using EmailCampaignReporting.API.Services.LLM.Providers;
using EmailCampaignReporting.API.Services.LLM.RAG;
using EmailCampaignReporting.API.Services.Enhanced;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Email Campaign Reporting API", 
        Version = "v1",
        Description = "API for Email Campaign Analytics and Reporting with Natural Language Query Support"
    });
});

// Configure LLM Provider options (new multi-provider system)
builder.Services.Configure<LLMProviderOptions>(
    builder.Configuration.GetSection(LLMProviderOptions.SectionName));

// Configure legacy LLM options (for LLAMA compatibility)
builder.Services.Configure<LLMOptions>(
    builder.Configuration.GetSection(LLMOptions.SectionName));

// Configure SQL Server options
builder.Services.Configure<SqlServerOptions>(
    builder.Configuration.GetSection(SqlServerOptions.SectionName));

// Register SQL Server Trigger Service
var sqlServerOptions = builder.Configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>();
Console.WriteLine($"SqlServer Configuration - ConnectionString: {sqlServerOptions?.ConnectionString}");
Console.WriteLine($"SqlServer Configuration - Section found: {sqlServerOptions != null}");
if (!string.IsNullOrEmpty(sqlServerOptions?.ConnectionString))
{
    Console.WriteLine("✅ Using EmailTriggerService with SQL Server connection");
    builder.Services.AddScoped<ISqlServerTriggerService, EmailTriggerService>();
}
else
{
    Console.WriteLine("⚠️ Using MockEmailTriggerService (no SQL Server connection string available)");
    builder.Services.AddScoped<ISqlServerTriggerService, MockEmailTriggerService>();
}

// Register LLM services - Use MockLLMService since we removed campaign functionality
var llmOptions = builder.Configuration.GetSection(LLMOptions.SectionName).Get<LLMOptions>();
builder.Services.AddScoped<ILLMService, MockLLMService>();

// Register LLM Provider System
// Register LLM providers
builder.Services.AddHttpClient(); // Required for GeminiLLMProvider
builder.Services.AddScoped<LlamaLLMProvider>();
builder.Services.AddScoped<GeminiLLMProvider>();

// Register LLM factory
builder.Services.AddScoped<ILLMServiceFactory, LLMServiceFactory>();

// Register RAG service
var llmProviderOptions = builder.Configuration.GetSection(LLMProviderOptions.SectionName).Get<LLMProviderOptions>();
if (llmProviderOptions?.EnableRAG == true)
{
    Console.WriteLine("✅ RAG (Retrieval-Augmented Generation) enabled");
    builder.Services.AddSingleton<IRAGService, InMemoryRAGService>();
}
else
{
    Console.WriteLine("ℹ️ RAG (Retrieval-Augmented Generation) disabled");
}

// Register dedicated Email Trigger Filter Service for natural language filter extraction
// First register the original LLAMA service for compatibility
if (llmOptions?.ModelPath != null && 
    !llmOptions.ModelPath.Contains("path/to/your") && 
    File.Exists(llmOptions.ModelPath))
{
    Console.WriteLine("✅ LLAMA model available for EmailTriggerFilterService");
    builder.Services.AddScoped<EmailTriggerFilterService>();
}
else
{
    Console.WriteLine("⚠️ LLAMA model not available, EmailTriggerFilterService will fail gracefully");
    builder.Services.AddScoped<EmailTriggerFilterService>();
}

// Register the Enhanced Email Trigger Filter Service (multi-provider with RAG)
Console.WriteLine("✅ Registering Enhanced Email Trigger Filter Service");
builder.Services.AddScoped<EnhancedEmailTriggerFilterService>();

// Register the service interface to use the enhanced version
builder.Services.AddScoped<IEmailTriggerFilterService>(provider => 
    provider.GetRequiredService<EnhancedEmailTriggerFilterService>());

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002") // React app URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Campaign Reporting API v1");
        c.RoutePrefix = "swagger"; // Access swagger at /swagger
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();