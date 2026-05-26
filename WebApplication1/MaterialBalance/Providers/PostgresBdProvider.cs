using System.Data.Common;
using System.Text;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using MaterialBalance.API.Request;
using System.Text.Json;
using NpgsqlTypes;

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
                values.Add($"(@Id{index}, @SourceNodeId{index}, @TargetNodeId{index}, @Type{index}, @LowerBound{index}, @UpperBound{index}, @Measured{index}, @Tolerance{index})");
                
                command.Parameters.AddWithValue($"Id{index}", flow.Id);
                command.Parameters.AddWithValue($"SourceNodeId{index}", NpgsqlDbType.Uuid, (object?)flow.SourceNodeId ?? DBNull.Value);
                command.Parameters.AddWithValue($"TargetNodeId{index}", NpgsqlDbType.Uuid, (object?)flow.TargetNodeId ?? DBNull.Value);
                command.Parameters.AddWithValue($"Type{index}", (int)flow.Type);
                command.Parameters.AddWithValue($"LowerBound{index}", flow.LowerBound);
                command.Parameters.AddWithValue($"UpperBound{index}", flow.UpperBound);
                command.Parameters.AddWithValue($"Measured{index}", flow.Measured);
                command.Parameters.AddWithValue($"Tolerance{index}", flow.Tolerance);
            }
        
            command.CommandText = $"""
                                   INSERT INTO "Flows" ("Id", "SourceNodeId", "TargetNodeId", "Type", "LowerBound", "UpperBound", "Measured", "Tolerance")
                                   VALUES {string.Join(", ", values)}
                                   ON CONFLICT ("Id") DO UPDATE SET
                                       "SourceNodeId" = EXCLUDED."SourceNodeId",
                                       "TargetNodeId" = EXCLUDED."TargetNodeId",
                                       "Type" = EXCLUDED."Type",
                                       "LowerBound" = EXCLUDED."LowerBound",
                                       "UpperBound" = EXCLUDED."UpperBound",
                                       "Measured" = EXCLUDED."Measured",
                                       "Tolerance" = EXCLUDED."Tolerance"
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
                                   SELECT "Id", "SourceNodeId", "TargetNodeId", "Type", "LowerBound", "UpperBound", "Measured", "Tolerance"
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
                    TargetNodeId = reader.IsDBNull(2) ? Guid.Empty : reader.GetGuid(2),
                    Type = (FlowType)reader.GetInt32(3),
                    LowerBound = reader.GetDouble(4),
                    UpperBound = reader.GetDouble(5),
                    Measured = reader.GetDouble(6),
                    Tolerance = reader.GetDouble(7)
                };
                result.Add(flow);
            }
        }
    
        return result;
    }
    
    public async Task<Guid> CreateSolverTask(IEnumerable<Guid> flowsId)
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        await using var command = new NpgsqlCommand();
        command.Connection = connection;
        
        var id = Guid.NewGuid();
        var flowsArray = flowsId.ToArray();
        command.CommandText = $"""
        INSERT INTO "Tasks" ("Id", "Status", "CreatedTime", "FlowsId")
        VALUES (@Id, @Status, @CreatedTime, @FlowsId)
        """;
        
        
        
        command.Parameters.AddWithValue("Id", id);
        command.Parameters.AddWithValue("Status", (int)StatusType.Pending);
        command.Parameters.AddWithValue("CreatedTime", DateTime.Now);
        command.Parameters.AddWithValue("FlowsId", flowsArray);
        await command.ExecuteNonQueryAsync();
        return id;
    }
    
    public async Task<SolverTask?> GetSolverTask(Guid? taskId)
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        await using var command = new NpgsqlCommand();
        command.Connection = connection;

        command.CommandText = $"""
            SELECT *
            FROM "Tasks"
            WHERE "Id" = @Id
            """;
        
        command.Parameters.AddWithValue("Id", taskId);
        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            SolverResult result = new();
            if (!reader.IsDBNull(5))
            {
                string json = reader.GetString(5); 
                result = JsonSerializer.Deserialize<SolverResult>(json);
            }
            return new SolverTask
            {
                Id = reader.GetGuid(0),
                Status = (StatusType)reader.GetInt32(1),
                CreatedTime = reader.GetDateTime(2),
                StartedTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                CompletedTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Result = result,
                FlowsId = reader.GetFieldValue<Guid[]>(6).ToList()
            };
        }
        return null;
    }

    public async Task<SolverTask?> GetPendingSolverTask()
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        await using var command = new NpgsqlCommand();
        command.Connection = connection;
        
        command.CommandText = $"""
            SELECT *
            FROM "Tasks"
            WHERE "Status" = @PendingStatus
            ORDER BY "CreatedTime" 
            LIMIT 1
            """;
        
        command.Parameters.AddWithValue("PendingStatus", (int)StatusType.Pending);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            SolverResult result = new();
            if (!reader.IsDBNull(5))
            {
                string json = reader.GetString(5); 
                result = JsonSerializer.Deserialize<SolverResult>(json);
            }
            return new SolverTask
            {
                Id = reader.GetGuid(0),
                Status = (StatusType)reader.GetInt32(1),
                CreatedTime = reader.GetDateTime(2),
                StartedTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                CompletedTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Result = result,
                FlowsId = reader.GetFieldValue<Guid[]>(6).ToList()
            };
        }
        return null;
    }

    public async Task<bool> HasProcessingSolverTask()
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        await using var command = new NpgsqlCommand();
        command.Connection = connection;

        command.CommandText = $"""
                               SELECT EXISTS (SELECT * FROM "Tasks" WHERE "Status" = @TaskStatus)
                               """;
        command.Parameters.AddWithValue("TaskStatus", (int)StatusType.Processing);
        
        return (bool)await command.ExecuteScalarAsync();
    }

    public async Task UpdateSolverTaskStatus(Guid taskId, StatusType status, SolverResult? result = null)
    {
        await using var connection = (NpgsqlConnection)await GetDataBaseConnection();
        await using var command = new NpgsqlCommand();
        command.Connection = connection;
        string? resultJson = null;
        if (result != null)
        {
            resultJson = JsonSerializer.Serialize(result);
        }

        command.CommandText = $"""
            UPDATE "Tasks"
            SET "Status" = @Status,
                "StartedTime" = CASE WHEN @Status = @ProcessingStatus THEN @Now ELSE "StartedTime" END,
                "CompletedTime" = CASE WHEN @Status = @CompletedStatus THEN @Now ELSE "CompletedTime" END,
                "Result" = CASE WHEN @ResultJson IS NOT NULL THEN @ResultJson::jsonb ELSE "Result" END
            WHERE "Id" = @Id
            """;
        
        command.Parameters.AddWithValue("Id", taskId);
        command.Parameters.AddWithValue("Status", (int)status);
        command.Parameters.AddWithValue("ProcessingStatus", (int)StatusType.Processing);
        command.Parameters.AddWithValue("CompletedStatus", (int)StatusType.Completed);
        command.Parameters.AddWithValue("Now", DateTime.Now);
        command.Parameters.AddWithValue("ResultJson", NpgsqlTypes.NpgsqlDbType.Jsonb, (object?)resultJson ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}