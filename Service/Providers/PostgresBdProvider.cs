using System.Data.Common;
using System.Text;
using MaterialBalance.API.Request;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MaterialBalance.Providers;

public class PostgresBdProvider :  IDBProvider
{
    
    private readonly string _connectionString;
    private readonly string _serverConnectionString;
    private readonly string _databaseName;

    public PostgresBdProvider(IOptions<Config> config)
    {

        _serverConnectionString = config.Value.ConnectionString;
        _databaseName = config.Value.DatabaseName;
        _connectionString = $"{_serverConnectionString};Database={_databaseName};";
    }

    /// <summary>
    /// Создание БД
    /// </summary>
    private async Task CreateDatabase()
    {
        await using DbConnection serverConnection = await GetServerConnection();
        
        DbCommand createDbCommand = serverConnection.CreateCommand();
        
        createDbCommand.CommandText = $"CREATE DATABASE \"{_databaseName}\" ";
        await createDbCommand.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// Подключение к серверу БД
    /// </summary>
    /// <returns></returns>
    private async Task<DbConnection> GetServerConnection()
    {
        NpgsqlConnection connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        return connection;
    }
    
    public async Task Initialize()
    {
        await CreateDatabase();

        await using DbConnection connection = await GetServerConnection();
        await using DbCommand command = connection.CreateCommand();
        
        StringBuilder query = new ();
        
        query.AppendLine("""
                         CREATE TABLE IF NOT EXISTS public."Flows" 
                         ("Id" UUID, "SourceFlowId" UUID, "TargetFlowId" UUID, "Type" int, "LowerBound" double, "UpperBound" double, NOT NULL, PRIMARY KEY ("Id"));
                         """);
        
        
        command.CommandText = query.ToString();
        await command.ExecuteNonQueryAsync();
    }

    
    public Task AddFlows(IEnumerable<Flow> flows)
    {
        //Запрос на добавление потоков
        throw new NotImplementedException();
    }
}