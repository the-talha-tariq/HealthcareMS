using AIAssistantService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIAssistantService.Data
{
    public class PatientReadDbContext : DbContext
    {
        public PatientReadDbContext(DbContextOptions<PatientReadDbContext> options)
            : base(options) { }

        public DbSet<PatientReadModel> Patients => Set<PatientReadModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatientReadModel>(entity =>
            {
                entity.ToTable("Patients", schema: "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(200);
            });
        }
    }
}
