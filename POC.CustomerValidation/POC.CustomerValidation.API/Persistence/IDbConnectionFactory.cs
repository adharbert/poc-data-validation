using Microsoft.Data.SqlClient;
using System.Data;


namespace POC.CustomerValidation.API.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

// Marker interface — always resolves to the central (non-tenant) database.
// Use this in repositories that read/write global data (e.g. LibraryRepository, DashboardRepository).
public interface ICentralDbConnectionFactory : IDbConnectionFactory { }

public class SqlConnectionFactory : IDbConnectionFactory, ICentralDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}