using System.ComponentModel.DataAnnotations;
using WebApplication1.Enums;

namespace WebApplication1.Database.Models;

public class Flow
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid SourceNodeId { get; set; }
    [Required]
    public Guid TargetNodeId { get; set; }
    [Required]
    public FlowType Type { get; set; }
    [Required]
    public double LowerBound {get; set;}
    [Required]
    public double UpperBound {get; set;}
    
    public Node SourceNode { get; set; }
    public Node TargetNode { get; set; }
}