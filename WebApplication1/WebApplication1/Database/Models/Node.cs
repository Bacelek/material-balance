using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Database.Models;

public class Node
{
    [Key]
    public Guid Id { get; set; }
    
    public List<Flow> OutgoingFlows { get; set; }
    public List<Flow> IncomingFlows { get; set; }
}