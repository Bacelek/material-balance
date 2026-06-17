using Moq;
using MaterialBalance.API.Request;
using MaterialBalance.Interfaces;
using Xunit;
using MathNet.Numerics.LinearAlgebra;

namespace MaterialBalanceTests
{
    public class SolverServiceTests
    {
        private readonly Mock<IGraphService> _graphMock = new();

        private SolverService CreateService() =>
            new SolverService(_graphMock.Object);
        
        [Fact]
        public void PrepareQpData_ConstantFlow()
        {
            var flow = new Flow
            {
                Id = Guid.NewGuid(),
                SourceNodeId = Guid.NewGuid(),
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.constant,
                Measured = 400,
                Tolerance = 0,
                LowerBound = 0,
                UpperBound = 2000
            };
            var graph = new Graph
            {
                Nodes = new List<Guid?> { flow.SourceNodeId, flow.TargetNodeId },
                IsConnectedGraph = true
            };
            var service = CreateService();
            var qp = service.PrepareQpData(new[] { flow }, graph);

            Assert.Equal(flow.Measured, qp.l[0]);
            Assert.Equal(flow.Measured, qp.u[0]);
            Assert.True(qp.W[0, 0] > 0);
            Assert.Equal(-qp.W[0, 0] * flow.Measured, qp.c[0]);
        }

        [Fact]
        public void PrepareQpData_MeasurableFlow()
        {
            var flow = new Flow
            {
                Id = Guid.NewGuid(),
                SourceNodeId = Guid.NewGuid(),
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.measurable,
                Measured = 300,
                Tolerance = 5,
                LowerBound = 10,
                UpperBound = 2000
            };
            var graph = new Graph
            {
                Nodes = new List<Guid?> { flow.SourceNodeId, flow.TargetNodeId },
                IsConnectedGraph = true
            };
            var service = CreateService();
            var qp = service.PrepareQpData(new[] { flow }, graph);

            double expectedWeight = 2.0 / (flow.Tolerance * flow.Tolerance); 
            Assert.Equal(expectedWeight, qp.W[0, 0]);
            Assert.Equal(-expectedWeight * flow.Measured, qp.c[0]);
            Assert.Equal(flow.LowerBound, qp.l[0]);
            Assert.Equal(flow.UpperBound, qp.u[0]);
        }

        [Fact]
        public void PrepareQpData_UnmeasurableFlow()
        {
            var flow = new Flow
            {
                Id = Guid.NewGuid(),
                SourceNodeId = Guid.NewGuid(),
                TargetNodeId = Guid.NewGuid(),
                Type = FlowType.unmeasurable,
                Measured = 0,
                Tolerance = 0,
                LowerBound = 100,
                UpperBound = 500
            };
            var graph = new Graph
            {
                Nodes = new List<Guid?> { flow.SourceNodeId, flow.TargetNodeId },
                IsConnectedGraph = true
            };
            var service = CreateService();
            var qp = service.PrepareQpData(new[] { flow }, graph);

            double xNominal = (flow.LowerBound + flow.UpperBound) / 2.0;
            double epsilon = 1e-6;
            Assert.Equal(epsilon, qp.W[0, 0]);
            Assert.Equal(-epsilon * xNominal, qp.c[0]);
            Assert.Equal(flow.LowerBound, qp.l[0]);
            Assert.Equal(flow.UpperBound, qp.u[0]);
        }
        

       
        [Fact]
        public void FindAvailablePoint_FeasibleSystem_ReturnsPoint()
        {
            var A = Matrix<double>.Build.DenseOfArray(new double[,] { { 1, 1, -1 } });
            var b = Vector<double>.Build.Dense(new double[] { 0 });
            var l = Vector<double>.Build.Dense(new double[] { 0, 0, 0 });
            var u = Vector<double>.Build.Dense(new double[] { 10, 10, 10 });

            var data = new QpData { A = A, b = b, l = l, u = u };
            var service = CreateService();
            var x0 = service.FindAvailablePoint(data);

            Assert.True((A * x0 - b).L2Norm() < 1e-8);
            for (int i = 0; i < A.ColumnCount; i++)
            {
                Assert.InRange(x0[i], l[i], u[i]);
            }
        }

        [Fact]
        public void FindAvailablePoint_InfeasibleSystem_ThrowsException()
        {
            var A = Matrix<double>.Build.DenseOfArray(new double[,] { { 1, 1, -1 } });
            var b = Vector<double>.Build.Dense(new double[] { 0 });
            var l = Vector<double>.Build.Dense(new double[] { 10, 10, 50 });
            var u = Vector<double>.Build.Dense(new double[] { 20, 20, 60 });

            var data = new QpData { A = A, b = b, l = l, u = u };
            var service = CreateService();
            Assert.Throws<Exception>(() => service.FindAvailablePoint(data));
        }

        [Fact]
        public void ActiveSet_AllUnmeasured_ReturnsOptimal()
        {
            double eps = 1e-6;
            var W = Matrix<double>.Build.Diagonal(new double[] { eps, eps, eps });
            var c = Vector<double>.Build.Dense(new double[] { -eps * 50, -eps * 100, -eps * 50 });
            var A = Matrix<double>.Build.DenseOfArray(new double[,] { { 1, 1, -1 } });
            var b = Vector<double>.Build.Dense(new double[] { 0 });
            var l = Vector<double>.Build.Dense(new double[] { 0, 0, 0 });
            var u = Vector<double>.Build.Dense(new double[] { 100, 200, 100 });
            var x0 = Vector<double>.Build.Dense(new double[] { 0, 0, 0 });

            var data = new QpData { W = W, c = c, A = A, b = b, l = l, u = u, x0 = x0};
            var service = CreateService();
            var result = service.ActiveSet(data);

            Assert.InRange(result[0], 16.6, 16.7);
            Assert.InRange(result[1], 66.6, 66.7);
            Assert.InRange(result[2], 83.3, 83.4);
        }
        
        [Fact]
        public async Task Solve_DisconnectedGraph_ReturnsGraphError()
        {
            var flows = new List<Flow>
            {
                new() { Id = Guid.NewGuid(), SourceNodeId = Guid.NewGuid(), TargetNodeId = Guid.NewGuid() }
            };
            var graph = new Graph { IsConnectedGraph = false };
            _graphMock.Setup(g => g.CreateGraph(flows)).Returns(graph);

            var service = CreateService();
            var result = await service.Solve(flows);

            Assert.Equal(SolverResultStatus.GraphConnectedError, result.Status);
        }

        [Fact]
        public async Task Solve_InfeasibleSystem_ReturnsAvailablePointError()
        {
            var nodeId = Guid.NewGuid();
            var flows = new List<Flow>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = Guid.Empty,
                    TargetNodeId = nodeId,
                    Type = FlowType.constant,
                    LowerBound = 100,
                    UpperBound = 200,
                    Measured = 100,
                    Tolerance = 0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = Guid.Empty,
                    TargetNodeId = nodeId,
                    Type = FlowType.constant,
                    LowerBound = 100,
                    UpperBound = 200,
                    Measured = 100,
                    Tolerance = 0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = nodeId,
                    TargetNodeId = Guid.Empty,
                    Type = FlowType.constant,
                    LowerBound = 100,
                    UpperBound = 200,
                    Measured = 100,
                    Tolerance = 0
                }
            };
            
            var graph = new Graph
            {
                Nodes = new List<Guid?> { nodeId },
                IsConnectedGraph = true
            };

            _graphMock.Setup(g => g.CreateGraph(flows)).Returns(graph);

            var service = CreateService();
            var result = await service.Solve(flows);

            Assert.Equal(SolverResultStatus.AvailablePointError, result.Status);

        }
        [Fact]
        public async Task Solve_FeasibleSystem_ReturnsOptimal()
        {
            var nodeId = Guid.NewGuid();
            var flows = new List<Flow>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = Guid.Empty,
                    TargetNodeId = nodeId,
                    Type = FlowType.unmeasurable,
                    LowerBound = 0,
                    UpperBound = 100,
                    Measured = 0,
                    Tolerance = 0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = Guid.Empty,
                    TargetNodeId = nodeId,
                    Type = FlowType.unmeasurable,
                    LowerBound = 0,
                    UpperBound = 200,
                    Measured = 0,
                    Tolerance = 0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceNodeId = nodeId,
                    TargetNodeId = Guid.Empty,
                    Type = FlowType.unmeasurable,
                    LowerBound = 0,
                    UpperBound = 100,
                    Measured = 0,
                    Tolerance = 0
                }
            };
            var graph = new Graph
            {
                Nodes = new List<Guid?> { nodeId },
                IsConnectedGraph = true
            };
            _graphMock.Setup(g => g.CreateGraph(flows)).Returns(graph);

            var service = CreateService();
            var result = await service.Solve(flows);

            Assert.Equal(SolverResultStatus.Optimal, result.Status);
            Assert.Equal(3, result.FlowsData.Length);
            Assert.InRange(result.FlowsData[0], 16.6, 16.7);
            Assert.InRange(result.FlowsData[1], 66.6, 66.7);
            Assert.InRange(result.FlowsData[2], 83.3, 83.4);
        }

    }
}