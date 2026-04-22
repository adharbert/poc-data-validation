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



        // Repositoies DI  ------------------------------------------------------
        services.AddScoped<IOrganizationRepository,         OrganizationRepository>();
        services.AddScoped<IFieldSectionRepository,         FieldSectionRepository>();
        services.AddScoped<IFieldDefinitionRepository,      FieldDefinitionRepository>();
        services.AddScoped<IFieldOptionRepository,          FieldOptionRepository>();
        services.AddScoped<IFieldValueHistoryRepository,    FieldValueHistoryRepository>();
        services.AddScoped<IFieldValueRepository,           FieldValueRepository>();


        // Serfvices DI  --------------------------------------------------------
        services.AddScoped<IOrganizationServices,   OrganizationServices>();
        services.AddScoped<IFieldOptionRepository,  FieldOptionRepository>();
        services.AddScoped<IFieldDefinitionService, FieldDefinitionService>();
        services.AddScoped<IFieldOptionService,     FieldOptionService>();
        services.AddScoped<IFieldValueService,      FieldValueService>();



        return services;
    }
}
