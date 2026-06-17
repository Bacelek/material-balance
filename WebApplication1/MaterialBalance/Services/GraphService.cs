using System.Data.Common;
using System.Text;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Generic;
using MaterialBalance.API.Request;


namespace MaterialBalance.Services;

public class GraphService : IGraphService
{
    public Graph CreateGraph(IEnumerable<Flow> flows)
    {
        var flowsList = new List<Flow>(flows);
        var nodes = ExtractNodes(flowsList);
        
        var graph = new Graph
        {
            Nodes = nodes,
            Flows = flowsList
        };
        
        graph.AdjacencyMatrix = CalculateAdjacencyMatrix(graph);

        graph.IsConnectedGraph = IsConnectedGraph(graph);

        return graph;
    }

    public List<Guid?> ExtractNodes(IEnumerable<Flow> flows)
    {
        var nodesSet = new HashSet<Guid?>();
        foreach (var flow in flows)
        {
            if (flow.SourceNodeId != Guid.Empty && flow.SourceNodeId != null)
                nodesSet.Add(flow.SourceNodeId);
            if (flow.TargetNodeId != Guid.Empty && flow.TargetNodeId != null)
                nodesSet.Add(flow.TargetNodeId);
        }
        return nodesSet.ToList();
    }

    public int[,] CalculateAdjacencyMatrix(Graph graph)
    {
        var nodesList = graph.Nodes;
        var flows = graph.Flows;
        
        Dictionary<Guid?, int> indexMap = nodesList
            .Select((id, idx) => new { id, idx })
            .ToDictionary(x => x.id, x => x.idx);

        int n = nodesList.Count;
        int[,] matrix = new int[n, n];
        
        foreach (var flow in flows)
        {
            if (flow.SourceNodeId != Guid.Empty && flow.TargetNodeId != Guid.Empty && flow.SourceNodeId != null && flow.TargetNodeId != null)
            {
                int i = indexMap[flow.SourceNodeId];
                int j = indexMap[flow.TargetNodeId];
                matrix[i,j] = 1;  
            }
        }
        return matrix;
    }

    public bool IsConnectedGraph(Graph graph)
    {
        int[,] adj = graph.AdjacencyMatrix;
        int n = adj.GetLength(0);

        if (n == 0) return true;

        bool[] visited = new bool[n];
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited[0] = true;
        int visitedCount = 1;

        while (queue.Count > 0)
        {
            int v = queue.Dequeue();
            for (int u = 0; u < n; u++)
            {
                
                if ((adj[v, u] != 0 || adj[u, v] != 0) && !visited[u])
                {
                    visited[u] = true;
                    queue.Enqueue(u);
                    visitedCount++;
                }
            }
        }
        return visitedCount == n;
    }
}