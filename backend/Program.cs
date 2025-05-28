using EmailCampaignReporting.API.Configuration;
using EmailCampaignReporting.API.Services;

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
        Description = "API for Email Campaign Analytics and Reporting"
    });
});

// Configure BigQuery options
builder.Services.Configure<BigQueryOptions>(
    builder.Configuration.GetSection(BigQueryOptions.SectionName));

// Register services - use mock service if credentials are not available
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
