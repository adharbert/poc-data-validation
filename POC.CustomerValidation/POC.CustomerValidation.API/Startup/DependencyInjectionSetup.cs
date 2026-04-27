using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Persistence;
using POC.CustomerValidation.API.Persistence.Repositories;
using POC.CustomerValidation.API.Services;

namespace POC.CustomerValidation.API.Startup;

public static class DependencyInjectionSetup
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Dapper configuration ----------------------------------------
        services.AddSingleton<IDbConnectionFactory>(
            new SqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")!)
        );

        // Repositories DI  -----------------------------------------------------
        services.AddScoped<IOrganizationRepository,             OrganizationRepository>();
        services.AddScoped<IFieldSectionRepository,             FieldSectionRepository>();
        services.AddScoped<IFieldDefinitionRepository,          FieldDefinitionRepository>();
        services.AddScoped<IFieldOptionRepository,              FieldOptionRepository>();
        services.AddScoped<IFieldValueHistoryRepository,        FieldValueHistoryRepository>();
        services.AddScoped<IFieldValueRepository,               FieldValueRepository>();
        services.AddScoped<ICustomerRepository,                 CustomerRepository>();
        services.AddScoped<IContractRepository,                 ContractRepository>();
        services.AddScoped<IMarketingProjectRepository,         MarketingProjectRepository>();
        services.AddScoped<IImportRepository,                   ImportRepository>();
        services.AddScoped<IImportColumnStagingRepository,      ImportColumnStagingRepository>();
        services.AddScoped<IDashboardRepository,                DashboardRepository>();

        // Services DI  ---------------------------------------------------------
        services.AddScoped<IOrganizationServices,               OrganizationServices>();
        services.AddScoped<IFieldDefinitionService,             FieldDefinitionService>();
        services.AddScoped<IFieldOptionService,                 FieldOptionService>();
        services.AddScoped<IFieldValueService,                  FieldValueService>();
        services.AddScoped<ICustomerService,                    CustomerService>();
        services.AddScoped<IContractService,                    ContractService>();
        services.AddScoped<IMarketingProjectService,            MarketingProjectService>();
        services.AddScoped<IImportService,                      ImportService>();
        services.AddScoped<IImportStagingService,               ImportStagingService>();
        services.AddScoped<IDashboardService,                   DashboardService>();

        return services;
    }
}
