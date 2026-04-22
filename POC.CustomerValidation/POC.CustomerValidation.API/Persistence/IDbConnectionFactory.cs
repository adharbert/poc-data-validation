using Microsoft.Data.SqlClient;
using System.Data;


namespace POC.CustomerValidation.API.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}


public class SqlConnectionFactory : IDbConnectionFactory
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