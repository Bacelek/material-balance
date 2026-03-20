using MaterialBalance.API.Request;

namespace MaterialBalance.Interfaces;

public interface IGraphService
{
    /// <summary>
    /// ...
    /// </summary>
    /// <returns></returns>
    Graph CreateGraph(IEnumerable<Flow> flows);
    
    List<Guid> ExtractNodes(IEnumerable<Flow> flows);
    
    List<List<int>> CalculateAdjacencyMatrix(Graph graph);
}