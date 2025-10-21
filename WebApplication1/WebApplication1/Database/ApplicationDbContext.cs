using Microsoft.EntityFrameworkCore;
using WebApplication1.Database.Models;

namespace WebApplication1.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }
    public DbSet<Flow>  Flows { get; set; }
    
    public DbSet<Node> Nodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Flow>()
            .HasOne(flow => flow.SourceNode)
            .WithMany(node => node.OutgoingFlows)
            .HasForeignKey(flow => flow.SourceNodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder
            .Entity<Flow>()
            .HasOne(flow => flow.TargetNode)
            .WithMany(node => node.IncomingFlows)
            .HasForeignKey(flow => flow.TargetNodeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        base.OnModelCreating(modelBuilder);
    }
}