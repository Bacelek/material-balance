using System.Data.Common;
using System.Text;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using MaterialBalance.API.Request;


namespace MaterialBalance.Services;

public class GraphService : IGraphService
{
    public Graph CreateGraph(IEnumerable<Flow> flows)
    {
        var flowsList = flows.ToList();
        var nodes = ExtractNodes(flowsList);
        
        var graph = new Graph
        {
            Nodes = nodes,
            Flows = flowsList
        };
        
        graph.AdjacencyMatrix = CalculateAdjacencyMatrix(graph);

        return graph;
    }

    public List<Guid> ExtractNodes(IEnumerable<Flow> flows)
    {
        var nodesSet = new HashSet<Guid>();
        foreach (var flow in flows)
        {
            if (flow.SourceNodeId != Guid.Empty)
                nodesSet.Add(flow.SourceNodeId);
            if (flow.TargetNodeId != Guid.Empty)
                nodesSet.Add(flow.TargetNodeId);
        }
        return nodesSet.ToList();
    }

    public List<List<int>> CalculateAdjacencyMatrix(Graph graph)
    {
        var nodesList = graph.Nodes;
        var flows = graph.Flows;
        
        var indexMap = nodesList
            .Select((id, idx) => new { id, idx })
            .ToDictionary(x => x.id, x => x.idx);

        int n = nodesList.Count;
        var matrix = new List<List<int>>(n);
        for (int i = 0; i < n; i++)
            matrix.Add(new List<int>(new int[n]));
        
        foreach (var flow in flows)
        {
            if (flow.SourceNodeId != Guid.Empty && flow.TargetNodeId != Guid.Empty)
            {
                int i = indexMap[flow.SourceNodeId];
                int j = indexMap[flow.TargetNodeId];
                matrix[i][j] = 1;  
            }
        }
        return matrix;
    }
}