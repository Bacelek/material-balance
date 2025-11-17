using System.Data.Common;
using System.Text;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using MaterialBalance.API.Request;

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
    
    private async Task<DbConnection> GetDataBaseConnection()
    {
        NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
    
    public async Task Initialize()
    {
        await CreateDatabase();

        await using DbConnection connection = await GetDataBaseConnection();
        await using DbCommand command = connection.CreateCommand();
        
        StringBuilder query = new ();
        
        query.AppendLine("""
                         CREATE TABLE IF NOT EXISTS public."Flows" 
                         ("Id" UUID NOT NULL, "SourceNodeId" UUID, "TargetNodeId" UUID NOT NULL, "Type" int NOT NULL, "LowerBound" double precision NOT NULL, "UpperBound" double precision NOT NULL, PRIMARY KEY ("Id"));
                         """);
        
        
        command.CommandText = query.ToString();
        await command.ExecuteNonQueryAsync();
    }

    
    public async Task AddFlows(IEnumerable<Flow> flows)
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        
        foreach (var flow in flows)
        {
            await using var command = new NpgsqlCommand();
            command.Connection = connection;
            command.CommandText = """
                                  INSERT INTO "Flows" ("Id", "SourceNodeId", "TargetNodeId", "Type", "LowerBound", "UpperBound")
                                  VALUES (@Id, @SourceNodeId, @TargetNodeId, @Type, @LowerBound, @UpperBound)
                                  ON CONFLICT ("Id") DO UPDATE SET
                                      "SourceNodeId" = EXCLUDED."SourceNodeId",
                                      "TargetNodeId" = EXCLUDED."TargetNodeId",
                                      "Type" = EXCLUDED."Type",
                                      "LowerBound" = EXCLUDED."LowerBound",
                                      "UpperBound" = EXCLUDED."UpperBound"
                                  """;
            command.Parameters.AddWithValue("Id", flow.Id);
            command.Parameters.AddWithValue("SourceNodeId", flow.SourceNodeId);
            command.Parameters.AddWithValue("TargetNodeId", flow.TargetNodeId);
            command.Parameters.AddWithValue("Type", (int)flow.Type);
            command.Parameters.AddWithValue("LowerBound", flow.LowerBound);
            command.Parameters.AddWithValue("UpperBound", flow.UpperBound);
            
            await command.ExecuteNonQueryAsync();
        }
    }
}