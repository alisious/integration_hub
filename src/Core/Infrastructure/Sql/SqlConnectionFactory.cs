using System.Data;
using Microsoft.Data.SqlClient;
using IntegrationHub.Infrastructure.Abstractions;

namespace IntegrationHub.Infrastructure.Sql;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public IDbConnection Create()
        => new SqlConnection(_connectionString);
}
