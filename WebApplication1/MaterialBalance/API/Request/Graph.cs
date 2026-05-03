using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MaterialBalance.API.Request;

public class Graph
{
    public List<Guid> Nodes { get; set; }
    public int[,] AdjacencyMatrix { get; set; } 
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