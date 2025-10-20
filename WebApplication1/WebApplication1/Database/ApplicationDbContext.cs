using Microsoft.EntityFrameworkCore;
using WebApplication1.Database.Models;

namespace WebApplication1.Models;

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
            .HasOne(flow => flow.Node)
            .WithMany(node => node.Flows)
            .HasForeignKey(flow => flow.SourceNodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder
            .Entity<Flow>()
            .HasOne(flow => flow.Node)
            .WithMany(node => node.Flows)
            .HasForeignKey(flow => flow.TargetNodeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        base.OnModelCreating(modelBuilder);
    }
}