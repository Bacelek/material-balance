using MaterialBalance.API.Request;
using MaterialBalance.Configurations;
using MaterialBalance.Providers;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MaterialBalanceTests
{
    public class PostgresDbProviderTests
    {
        private const string ConnectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=123;Database=material_balance_test";

        private  readonly PostgresBdProvider _provider;

        public PostgresDbProviderTests()
        {
            var config = new Config
            {
                ConnectionString = ConnectionString,
                DatabaseName = "material_balance_test"
            };
            var options = Options.Create(config);
            _provider = new PostgresBdProvider(options);
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
        public async Task AddFlows_ReturnData()
        {
            await ClearTables();  
            
            var flowId = Guid.NewGuid();
            var targetNodeId = Guid.NewGuid();
            var flow = new Flow
            {
                Id = flowId,
                SourceNodeId = null,            
                TargetNodeId = targetNodeId,
                Type = FlowType.measurable,
                Measured = 100.0,
                Tolerance = 5,
                LowerBound = 0,
                UpperBound = 1000
            };
            await _provider.AddFlows(new[] { flow });

            var data = (await _provider.GetFlows(new[] { flowId })).First();

            Assert.NotNull(data);
            Assert.Equal(flowId, data.Id);
            Assert.Equal(Guid.Empty, data.SourceNodeId);
            Assert.Equal(targetNodeId, data.TargetNodeId);
            Assert.Equal(FlowType.measurable, data.Type);
            Assert.Equal(100, data.Measured);
            Assert.Equal(5, data.Tolerance);
        }
        
        [Fact]
        public async Task DeleteFlows_ReturnEmpty()
        {
            await ClearTables();
        
            var flowId = Guid.NewGuid();
            var flow = new Flow
            {
                Id = flowId,
                SourceNodeId = Guid.NewGuid(),
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.unmeasurable,
                Measured = 0,
                Tolerance = 0,
                LowerBound = 0,
                UpperBound = 100
            };
            await _provider.AddFlows(new[] { flow });

            await _provider.DeleteFlows(new[] { flowId });
            var result = await _provider.GetFlows(new[] { flowId });

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFlows_ReturnEmpty()
        {
            await ClearTables();
            
            var result = await _provider.GetFlows(new[] { Guid.NewGuid() });
            
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddFlows_UpdateFlow()
        {
            await ClearTables();

            var flowId = Guid.NewGuid();
            var initialFlow = new Flow
            {
                Id = flowId,
                SourceNodeId = Guid.NewGuid(),
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.measurable,
                Measured = 100,
                Tolerance = 1,
                LowerBound = 10,
                UpperBound = 50
            };
            await _provider.AddFlows(new[] { initialFlow });

            var updatedFlow = new Flow
            {
                Id = flowId,
                SourceNodeId = null,                 
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.constant,
                Measured = 200,
                Tolerance = 0,
                LowerBound = 0,
                UpperBound = 300
            };
            await _provider.AddFlows(new[] { updatedFlow });
            
            var result = (await _provider.GetFlows(new[] { flowId })).First();

            Assert.Equal(flowId, result.Id);
            Assert.Equal(Guid.Empty, result.SourceNodeId);   
            Assert.Equal(updatedFlow.TargetNodeId, result.TargetNodeId);
            Assert.Equal(FlowType.constant, result.Type);
            Assert.Equal(200, result.Measured);
            Assert.Equal(0, result.Tolerance);
            Assert.Equal(0, result.LowerBound);
            Assert.Equal(300, result.UpperBound);
        }
        
        [Fact]
        public async Task GetSolverTask_ReturnTask()
        {
            await ClearTables();
            
            var flowsId = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var taskId = await _provider.CreateSolverTask(flowsId);
            
            var task = await _provider.GetSolverTask(taskId);

            Assert.NotNull(task);
            Assert.Equal(StatusType.Pending, task.Status);
            Assert.Equal(flowsId.OrderBy(g => g), task.FlowsId.OrderBy(g => g));
            Assert.Null(task.StartedTime);
            Assert.Null(task.CompletedTime);
            Assert.NotNull(task.Result);
        }

        [Fact]
        public async Task GetSolverTask_ReturnNull()
        {
            await ClearTables();
            var task = await _provider.GetSolverTask(Guid.NewGuid());
            Assert.Null(task);
        }

        [Fact]
        public async Task UpdateSolverTaskStatus_SaveResult()
        {
            await ClearTables();
        
            var taskId = await _provider.CreateSolverTask(new[] { Guid.NewGuid() });
            var result = new SolverResult
            {
                Status = SolverResultStatus.Optimal,
                FlowsData = new double[] { 100, 200 }
            };

            await _provider.UpdateSolverTaskStatus(taskId, StatusType.Processing);
            await _provider.UpdateSolverTaskStatus(taskId, StatusType.Completed, result);
            var updated = await _provider.GetSolverTask(taskId);

            Assert.Equal(StatusType.Completed, updated.Status);
            Assert.Equal(SolverResultStatus.Optimal, updated.Result.Status);
            Assert.Equal(new double[] { 100, 200 }, updated.Result.FlowsData);
            Assert.NotNull(updated.StartedTime);
            Assert.NotNull(updated.CompletedTime);
        }
        
        [Fact]
        public async Task GetPendingSolverTask_NotProcessing_ReturnEarliestPending()
        {
            await ClearTables();

            var flows = new List<Guid> { Guid.NewGuid() };
            var taskId1 = await _provider.CreateSolverTask(flows);
            await Task.Delay(50); 
            var taskId2 = await _provider.CreateSolverTask(flows);

            var Pending = await _provider.GetPendingSolverTask();
            
            Assert.Equal(taskId1, Pending.Id);

        }
        
        [Fact]
        public async Task GetPendingSolverTask_HasProcessing_ReturnEarliestPending()
        {
            await ClearTables();

            var flows = new List<Guid> { Guid.NewGuid() };
            var taskId1 = await _provider.CreateSolverTask(flows);
            await Task.Delay(50); 
            var taskId2 = await _provider.CreateSolverTask(flows);
            
            await _provider.UpdateSolverTaskStatus(taskId1, StatusType.Processing);
            
            var Pending = await _provider.GetPendingSolverTask();
            
            Assert.Equal(taskId2, Pending.Id);


        }
        
        [Fact]
        public async Task GetPendingSolverTask_ReturnNull()
        {
            await ClearTables();

            var flows = new List<Guid> { Guid.NewGuid() };
            var taskId1 = await _provider.CreateSolverTask(flows);
            await Task.Delay(50); 
            var taskId2 = await _provider.CreateSolverTask(flows);
            
            await _provider.UpdateSolverTaskStatus(taskId1, StatusType.Processing);
            await _provider.UpdateSolverTaskStatus(taskId2, StatusType.Processing);
            
            var Pending = await _provider.GetPendingSolverTask();
            
            Assert.Null(Pending);
        }

        [Fact]
        public async Task HasProcessingSolverTask_PendingTask_ReturnFalse()
        {
            await ClearTables();
            
            await _provider.CreateSolverTask(new[] { Guid.NewGuid() });

            bool result = await _provider.HasProcessingSolverTask();
            Assert.False(result);
        }

        [Fact]
        public async Task HasProcessingSolverTask_ProcessingTask_ReturnTrue()
        {
            await ClearTables();
            
            var taskId = await _provider.CreateSolverTask(new[] { Guid.NewGuid() });
            await _provider.UpdateSolverTaskStatus(taskId, StatusType.Processing);

            bool result = await _provider.HasProcessingSolverTask();
            Assert.True(result);
        }

        [Fact]
        public async Task HasProcessingSolverTask_CompletedTask_ReturnFalse()
        {
            await ClearTables();
            
            var taskId = await _provider.CreateSolverTask(new[] { Guid.NewGuid() });
            await _provider.UpdateSolverTaskStatus(taskId, StatusType.Completed);

            bool result = await _provider.HasProcessingSolverTask();
            Assert.False(result);
        }
        
    
    }
}