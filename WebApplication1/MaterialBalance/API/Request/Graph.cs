namespace MaterialBalance.API.Request;

public class Graph
{
    public List<Guid> Nodes { get; set; }
    public List<List<int>> AdjacencyMatrix { get; set; } 
    public List<Flow>  Flows { get; set; }
}
/*"matrix": [
[ 0, 1, 0, 0, 0, 0, 0, 0, 1, 0],
[ 0, 0, 0, 0, 1, 0, 0, 0, 1, 0],
[ 0, 0, 0, 1, 0, 0, 0, 0, 0, 0],
[ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ],
[ 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 ], 
[ 0, 0, 1, 0, 0, 0, 0, 0, 0, 0],
[ 0, 0, 0, 0, 0, 0, 0, 1, 0, 0],
[ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
[ 0, 0, 0, 0, 0, 1, 0, 0, 0, 0],
[ 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]*/