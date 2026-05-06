using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Dapper;
using DbUp;
using Microsoft.Data.SqlClient;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;
using POC.CustomerValidation.API.Persistence;
using System.Reflection;

namespace POC.CustomerValidation.API.Services.Provisioning;

public class OrganizationProvisioningService(
    IOrganizationRepository repo,
    ITenantConnectionCache cache,
    IConfiguration config,
    ILogger<OrganizationProvisioningService> log) : IOrganizationProvisioningService
{
    private readonly IOrganizationRepository _repo = repo;
    private readonly ITenantConnectionCache _cache = cache;
    private readonly IConfiguration _config = config;
    private readonly ILogger<OrganizationProvisioningService> _log = log;

    public async Task ProvisionAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await _repo.GetByIdAsync(organizationId)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        if (string.IsNullOrWhiteSpace(org.Abbreviation))
            throw new InvalidOperationException($"Organization {organizationId} has no Abbreviation — cannot provision isolated database.");

        _log.LogInformation("Starting database provisioning for org {OrgId} ({Abbr})", organizationId, org.Abbreviation);
        await _repo.UpdateProvisioningStatusAsync(organizationId, "provisioning");

        try
        {
            var connectionString = IsAzureConfigured()
                ? await CreateAzureDatabaseAsync(org.Abbreviation, ct)
                : CreateLocalDatabase(org.Abbreviation);

            DeploySchema(connectionString);
            await SeedOrganizationAsync(org, connectionString);

            await _repo.UpdateProvisioningStatusAsync(organizationId, "ready", connectionString);
            _cache.Invalidate(organizationId);

            _log.LogInformation("Provisioning complete for org {OrgId} ({Abbr})", organizationId, org.Abbreviation);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Provisioning failed for org {OrgId} ({Abbr})", organizationId, org.Abbreviation);
            await _repo.UpdateProvisioningStatusAsync(organizationId, "failed");
            throw;
        }
    }

    // -------------------------------------------------------
    // Mode detection
    // -------------------------------------------------------

    private bool IsAzureConfigured() =>
        !string.IsNullOrWhiteSpace(_config["Azure:SubscriptionId"]);

    // -------------------------------------------------------
    // Local dev mode — creates a SQL Server database on the
    // local instance using the connection string template from
    // appsettings. DbUp's EnsureDatabase handles CREATE DATABASE.
    // -------------------------------------------------------

    private string CreateLocalDatabase(string databaseName)
    {
        var template = _config["Azure:LocalConnectionStringTemplate"]
            ?? throw new InvalidOperationException(
                "No Azure provisioning config found. " +
                "For local dev set Azure:LocalConnectionStringTemplate in appsettings. " +
                "For Azure set Azure:SubscriptionId.");

        var connectionString = template.Replace("{DatabaseName}", databaseName);

        _log.LogInformation("Local mode: creating database '{DbName}'", databaseName);
        EnsureDatabase.For.SqlDatabase(connectionString);
        _log.LogInformation("Local mode: database '{DbName}' ready", databaseName);

        return connectionString;
    }

    // -------------------------------------------------------
    // Azure mode — creates a database in the elastic pool via
    // Azure Resource Manager. Uses Managed Identity (no secrets).
    // -------------------------------------------------------

    private async Task<string> CreateAzureDatabaseAsync(string databaseName, CancellationToken ct)
    {
        var subscriptionId  = _config["Azure:SubscriptionId"]!;
        var resourceGroup   = _config["Azure:ResourceGroupName"]    ?? throw new InvalidOperationException("Azure:ResourceGroupName not configured.");
        var serverName      = _config["Azure:SqlServerName"]        ?? throw new InvalidOperationException("Azure:SqlServerName not configured.");
        var location        = _config["Azure:Location"]             ?? "eastus";
        var elasticPoolId   = _config["Azure:ElasticPoolResourceId"];

        _log.LogInformation("Creating Azure SQL database '{DbName}' on server '{Server}'", databaseName, serverName);

        var armClient = new ArmClient(new DefaultAzureCredential());
        var server = armClient.GetSqlServerResource(
            SqlServerResource.CreateResourceIdentifier(subscriptionId, resourceGroup, serverName));

        var dbData = new SqlDatabaseData(new Azure.Core.AzureLocation(location));

        if (!string.IsNullOrEmpty(elasticPoolId))
            dbData.ElasticPoolId = new Azure.Core.ResourceIdentifier(elasticPoolId);

        await server.GetSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, dbData, ct);

        _log.LogInformation("Azure SQL database '{DbName}' created successfully", databaseName);

        // Managed Identity connection — no username/password stored
        return $"Server=tcp:{serverName}.database.windows.net,1433;Initial Catalog={databaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    }

    // -------------------------------------------------------
    // Schema deployment — same for both modes
    // -------------------------------------------------------

    public async Task<IEnumerable<DatabaseMigrationResult>> MigrateAllIsolatedAsync(CancellationToken ct = default)
    {
        var orgs    = await _repo.GetAllIsolatedAsync();
        var results = new List<DatabaseMigrationResult>();

        foreach (var org in orgs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _log.LogInformation("Running migrations for isolated org {OrgId} ({Abbr})", org.OrganizationId, org.Abbreviation);
                DeploySchema(org.IsolatedConnectionString!);
                results.Add(new DatabaseMigrationResult(org.OrganizationId, org.OrganizationName, true, null));
                _log.LogInformation("Migrations complete for org {OrgId}", org.OrganizationId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Migration failed for org {OrgId} ({Abbr})", org.OrganizationId, org.Abbreviation);
                results.Add(new DatabaseMigrationResult(org.OrganizationId, org.OrganizationName, false, ex.Message));
            }
        }

        return results;
    }

    private void DeploySchema(string connectionString)
    {
        _log.LogInformation("Deploying schema via DbUp");

        var result = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains(".Provisioning.Scripts."))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
            throw new Exception("Schema deployment failed.", result.Error);

        _log.LogInformation("Schema deployment complete");
    }

    // -------------------------------------------------------
    // Seeds the organization row into the isolated database so
    // FK constraints (Customers, FieldSections, etc.) resolve
    // without requiring a cross-database reference.
    // -------------------------------------------------------
    private async Task SeedOrganizationAsync(Organization org, string connectionString)
    {
        _log.LogInformation("Seeding organization row into isolated database for org {OrgId}", org.OrganizationId);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Organizations WHERE Id = @Id)
            INSERT INTO dbo.Organizations (
                Id, Name, FilingName, MarketingName, Abbreviation, OrganizationCode,
                Website, Phone, CompanyEmail, IsActive,
                RequiresIsolatedDatabase, DatabaseProvisioningStatus,
                CreateUtcDt, CreatedBy, ModifiedUtcDt, ModifiedBy)
            VALUES (
                @Id, @Name, @FilingName, @MarketingName, @Abbreviation, @OrganizationCode,
                @Website, @Phone, @CompanyEmail, @IsActive,
                1, 'ready',
                @CreatedDate, @CreatedBy, @ModifiedDate, 'Provisioning')
            """;

        using var conn = new SqlConnection(connectionString);
        await conn.ExecuteAsync(sql, new
        {
            Id               = org.OrganizationId,
            Name             = org.OrganizationName,
            org.FilingName,
            org.MarketingName,
            org.Abbreviation,
            org.OrganizationCode,
            org.Website,
            org.Phone,
            org.CompanyEmail,
            IsActive         = org.IsActive ?? true,
            org.CreatedDate,
            CreatedBy        = org.CreatedBy ?? "System",
            ModifiedDate     = DateTime.UtcNow,
        });

        _log.LogInformation("Organization row seeded into isolated database for org {OrgId}", org.OrganizationId);
    }
}
