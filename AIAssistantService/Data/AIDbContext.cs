using AIAssistantService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIAssistantService.Data
{
    public class AIDbContext : DbContext
    {
        public AIDbContext(DbContextOptions<AIDbContext> options)
            : base(options) { }

        // Chat history — AI service's own table
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Chat messages — AI service owns this table
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired();
                entity.HasIndex(e => e.SessionId);
            });
        }
    }
}
