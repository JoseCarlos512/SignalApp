using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Persistence.Entities;

namespace MyProject.Infrastructure.Persistence;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatSessionEntity> Sessions => Set<ChatSessionEntity>();
    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();
    public DbSet<AdvisorStateEntity> Advisors => Set<AdvisorStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSessionEntity>(entity =>
        {
            entity.ToTable("ChatSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.Property(e => e.Dni).HasMaxLength(30);
            entity.Property(e => e.Phone).HasMaxLength(40);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderType).HasMaxLength(20);
            entity.Property(e => e.SenderId).HasMaxLength(120);
            entity.Property(e => e.Text).HasMaxLength(4000);
        });

        modelBuilder.Entity<AdvisorStateEntity>(entity =>
        {
            entity.ToTable("AdvisorStates");
            entity.HasKey(e => e.AdvisorId);
            entity.Property(e => e.Name).HasMaxLength(120);
        });
    }
}
