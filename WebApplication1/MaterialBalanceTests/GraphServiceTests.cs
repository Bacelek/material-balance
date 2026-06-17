using Moq;
using MaterialBalance.API.Request;
using MaterialBalance.Services;
using Xunit;

namespace MaterialBalanceTests;

public class GraphServiceTests
{
    private readonly GraphService _graphService = new();
    
        [Fact]
        public void IsConnectedGraph_ConnectedNodes_ReturnsTrue()
        {
            var nodeA = Guid.NewGuid();
            var nodeB = Guid.NewGuid();
            var nodeC = Guid.NewGuid();

            var flows = new List<Flow>
            {
                new() { Id = Guid.NewGuid(), SourceNodeId = nodeA, TargetNodeId = nodeB },
                new() { Id = Guid.NewGuid(), SourceNodeId = nodeB, TargetNodeId = nodeC }
            };

            var graph = _graphService.CreateGraph(flows);
            Assert.True(graph.IsConnectedGraph);
        }
        
        [Fact]
        public void IsConnectedGraph_DisconnectedNodes_ReturnsFalse()
        {
            var nodeA = Guid.NewGuid();
            var nodeB = Guid.NewGuid();
            var nodeC = Guid.NewGuid();
            var nodeD = Guid.NewGuid();

            var flows = new List<Flow>
            {
                new() { Id = Guid.NewGuid(), SourceNodeId = nodeA, TargetNodeId = nodeB },
                new() { Id = Guid.NewGuid(), SourceNodeId = nodeC, TargetNodeId = nodeD }
            };

            var graph = _graphService.CreateGraph(flows);
            Assert.False(graph.IsConnectedGraph);
        }
        
        
        [Fact]
        public void IsConnectedGraph_OneNode_ReturnsTrue()
        {
            var nodeA = Guid.NewGuid();
            var nodeB = Guid.NewGuid();
            var flows = new List<Flow>
            {
                new() { Id = Guid.NewGuid(), SourceNodeId = nodeA, TargetNodeId = Guid.Empty }
            };

            var graph = _graphService.CreateGraph(flows);
            Assert.True(graph.IsConnectedGraph);
        }
}