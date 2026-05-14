namespace POC.CustomerValidation.API.Persistence;

public interface ITenantContext
{
    string ConnectionString { get; }
    void Resolve(string connectionString);
}

public class TenantContext(IConfiguration configuration) : ITenantContext
{
    private string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public string ConnectionString => _connectionString;

    public void Resolve(string connectionString) => _connectionString = connectionString;
}
