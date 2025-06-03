using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services;
using EmailCampaignReporting.API.Models.DTOs;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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

// Configure BigQuery options
builder.Services.Configure<BigQueryOptions>(
    builder.Configuration.GetSection(BigQueryOptions.SectionName));

// Configure LLM options
builder.Services.Configure<LLMOptions>(
    builder.Configuration.GetSection(LLMOptions.SectionName));

// Register BigQuery services - use mock service if credentials are not available
var bigQueryOptions = builder.Configuration.GetSection(BigQueryOptions.SectionName).Get<BigQueryOptions>();
if (bigQueryOptions?.CredentialsPath != null && 
    !bigQueryOptions.CredentialsPath.Contains("path/to/your") && 
    File.Exists(bigQueryOptions.CredentialsPath))
{
    builder.Services.AddScoped<IBigQueryService, BigQueryService>();
}
else
{
    builder.Services.AddScoped<IBigQueryService, MockBigQueryService>();
}

// Register Campaign Query Service
builder.Services.AddScoped<ICampaignQueryService, CampaignQueryService>();

// Register LLM services - Use enhanced service with rule-based fallback
var llmOptions = builder.Configuration.GetSection(LLMOptions.SectionName).Get<LLMOptions>();
if (llmOptions?.ModelPath != null && 
    !llmOptions.ModelPath.Contains("path/to/your"))
{
    // Use regular LLM service for now
    builder.Services.AddScoped<ILLMService, LLMService>();
}
else
{
    // Use mock service when model is not available
    builder.Services.AddScoped<ILLMService, MockLLMService>();
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001") // React app URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
