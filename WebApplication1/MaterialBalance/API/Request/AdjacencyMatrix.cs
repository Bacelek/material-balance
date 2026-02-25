namespace MaterialBalance.API.Request;

public class AdjacencyMatrix
{
    public List<Guid> Nodes { get; set; }
    public List<List<int>> Matrix { get; set; } 
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