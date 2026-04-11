using System.Data;

namespace IntegrationHub.Infrastructure.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection Create(); // ZAWSZE nowa instancja (pooling obsłuży ADO.NET)
}
