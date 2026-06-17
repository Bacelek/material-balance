using Xunit;
using Moq;
using System.Text.Json;
using MaterialBalance.API.Request;
using MaterialBalance.Services;
using MaterialBalance.Providers;
using Microsoft.Extensions.Options;
using MaterialBalance.Configurations;
using Npgsql;

namespace MaterialBalanceTests;

public class CasesTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=123;Database=material_balance_test";

    private  readonly PostgresBdProvider _provider;
    private readonly SolverService _service = new(new GraphService());
    
    public CasesTests()
    {
        var config = new Config
        {
            ConnectionString = ConnectionString,
            DatabaseName = "material_balance_test"
        };
        var options = Options.Create(config);
        _provider = new PostgresBdProvider(options);
    }
    private static List<Flow> LoadFlowsFromFile(string fileName)
    {
        var path = Path.Combine("CasesTests", fileName);
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var flows = JsonSerializer.Deserialize<List<Flow>>(json, options);
        return flows;
    }
    
    private async Task ClearTables()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"DELETE FROM ""Flows""; DELETE FROM ""Tasks"";";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Solve_Case1()
    {
        await ClearTables();
        
        var flows = LoadFlowsFromFile("case1.json");
        
        await _provider.AddFlows(flows);
        var flowIds = flows.Select(f => f.Id).ToList();
        var dbFlows = await _provider.GetFlows(flowIds);
        
        var result = await _service.Solve(dbFlows);
        
        Assert.NotNull(result.FlowsData);
        Assert.True(result.Disbalance < 1e-6);
    }
    
    [Fact]
    public async Task Solve_Case2()
    {
        await ClearTables();
        
        var flows = LoadFlowsFromFile("case2.json");
        
        await _provider.AddFlows(flows);
        var flowIds = flows.Select(f => f.Id).ToList();
        var dbFlows = await _provider.GetFlows(flowIds);
        
        var result = await _service.Solve(dbFlows);
        
        
        Assert.NotNull(result.FlowsData);
        Assert.True(result.Disbalance < 1e-6);
    }
    
    [Fact]
    public async Task Solve_Case3()
    {
        await ClearTables();
        
        var flows = LoadFlowsFromFile("case3.json");
        
        await _provider.AddFlows(flows);
        var flowIds = flows.Select(f => f.Id).ToList();
        var dbFlows = await _provider.GetFlows(flowIds);
        
        var result = await _service.Solve(dbFlows);
        
        Assert.NotNull(result.FlowsData);
        Assert.True(result.Disbalance < 1e-6);
    }
    
    [Fact]
    public async Task Solve_Case4()
    {
        await ClearTables();
        
        var flows = LoadFlowsFromFile("case4.json");
        
        await _provider.AddFlows(flows);
        var flowIds = flows.Select(f => f.Id).ToList();
        var dbFlows = await _provider.GetFlows(flowIds);
        
        var result = await _service.Solve(dbFlows);
        
        Assert.NotNull(result.FlowsData);
        Assert.True(result.Disbalance < 1e-6);
    }
}