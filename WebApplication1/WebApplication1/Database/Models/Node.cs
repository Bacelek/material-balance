using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Database.Models;

public class Node
{
    [Key]
    public Guid ID { get; set; }
    
    public List<Flow> Flows { get; set; }
    
    public Flow Flow { get; set; }
}