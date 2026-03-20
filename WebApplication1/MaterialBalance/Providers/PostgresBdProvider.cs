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
        const int batchSize = 100;
        var flowsList = flows.ToList();
    
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();

        for (int i = 0; i < flowsList.Count; i += batchSize)
        {
            var batch = flowsList.Skip(i).Take(batchSize);
        
            await using var command = new NpgsqlCommand();
            command.Connection = connection;
        
            var values = new List<string>();
            int paramIndex = 0;
        
            foreach (var flow in batch)
            {
                int index = paramIndex++;
                values.Add($"(@Id{index}, @SourceNodeId{index}, @TargetNodeId{index}, @Type{index}, @LowerBound{index}, @UpperBound{index})");
                
                command.Parameters.AddWithValue($"Id{index}", flow.Id);
                command.Parameters.AddWithValue($"SourceNodeId{index}", flow.SourceNodeId);
                command.Parameters.AddWithValue($"TargetNodeId{index}", flow.TargetNodeId);
                command.Parameters.AddWithValue($"Type{index}", (int)flow.Type);
                command.Parameters.AddWithValue($"LowerBound{index}", flow.LowerBound);
                command.Parameters.AddWithValue($"UpperBound{index}", flow.UpperBound);
            }
        
            command.CommandText = $"""
                                   INSERT INTO "Flows" ("Id", "SourceNodeId", "TargetNodeId", "Type", "LowerBound", "UpperBound")
                                   VALUES {string.Join(", ", values)}
                                   ON CONFLICT ("Id") DO UPDATE SET
                                       "SourceNodeId" = EXCLUDED."SourceNodeId",
                                       "TargetNodeId" = EXCLUDED."TargetNodeId",
                                       "Type" = EXCLUDED."Type",
                                       "LowerBound" = EXCLUDED."LowerBound",
                                       "UpperBound" = EXCLUDED."UpperBound"
                                   """;
            
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteFlows(IEnumerable<Guid> flowsId)
    {
        const int batchSize = 100;
        var flowsIdList = flowsId.ToList();
    
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        
        for (int i = 0; i < flowsIdList.Count; i += batchSize)
        {
            var batch = flowsIdList.Skip(i).Take(batchSize).ToArray();
        
            await using var command = new NpgsqlCommand();
            command.Connection = connection;
            
            command.CommandText = $"""DELETE FROM "Flows" WHERE "Id" = ANY(@flowId)""";
            command.Parameters.AddWithValue("flowId", batch);
        
            await command.ExecuteNonQueryAsync();
        }
    }
    
    public async Task<IEnumerable<Flow>> GetFlows(IEnumerable<Guid> flowsId)
    {
        const int batchSize = 100;
        var result = new List<Flow>();
    
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        var flowsIdList = flowsId.ToList();
        
        for (int i = 0; i < flowsIdList.Count; i += batchSize)
        {
            var batch = flowsIdList.Skip(i).Take(batchSize).ToArray();
        
            await using var command = new NpgsqlCommand();
            command.Connection = connection;
        
            command.CommandText = $"""
                                   SELECT "Id", "SourceNodeId", "TargetNodeId", "Type", "LowerBound", "UpperBound" 
                                   FROM "Flows" 
                                   WHERE "Id" = ANY(@flowIds)
                                   """;
            command.Parameters.AddWithValue("flowIds", batch);
        
            await using var reader = await command.ExecuteReaderAsync();
        
            while (await reader.ReadAsync())
            {
                var flow = new Flow
                {
                    Id = reader.GetGuid(0),
                    SourceNodeId = reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
                    TargetNodeId = reader.GetGuid(2),
                    Type = (FlowType)reader.GetInt32(3),
                    LowerBound = reader.GetDouble(4),
                    UpperBound = reader.GetDouble(5)
                };
                result.Add(flow);
            }
        }
    
        return result;
    }
}