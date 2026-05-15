using POC.CustomerValidation.API.Hubs;
using POC.CustomerValidation.API.Middleware;
using POC.CustomerValidation.API.Services.Provisioning;
using POC.CustomerValidation.API.Startup;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Setting up Serilog for sql logging.
builder.Host.AddCustomSerilogLogging();



// -------------------------------------------------------
// CORS
// -------------------------------------------------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminSpa", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});



// Auto-start Azurite in Development first — must be registered before RegisterServices()
// so it starts before StartupBlobProvisioningService and BlobImportPollingService.
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<AzuriteHostedService>();

// Handle DI setups.  Startup/DependencyInjectionSetup.cs
builder.Services.RegisterServices(builder.Configuration);


// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddOpenApi();
//builder.Services.AddEndpointsApiExplorer();


// -------------------------------------------------------
// Health checks
// -------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("LoggingConnection")!,
        name: "sqlserver",
        tags: ["db", "sql"]);



// ************** app section
var app = builder.Build();


app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// UseRouting must run before TenantResolutionMiddleware so route values
// (e.g. {organisationId}) are populated when the middleware reads them.
app.UseRouting();
app.UseMiddleware<TenantResolutionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "My API";
    options.Theme = ScalarTheme.Moon;
    options.CustomCss = ".section-flare { height: 100px; }";  // For some reason, if I don't add this, the UI has a HUGE gap from top to first endpoint.
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ImportHub>("/hubs/import");
app.MapHealthChecks("/health");

app.Run();
