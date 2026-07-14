using AIAssistantService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIAssistantService.Data
{
    public class AppointmentReadDbContext : DbContext
    {
        public AppointmentReadDbContext(
            DbContextOptions<AppointmentReadDbContext> options)
            : base(options) { }

        public DbSet<AppointmentReadModel> Appointments =>
            Set<AppointmentReadModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentReadModel>(entity =>
            {
                entity.ToTable("Appointments", schema: "dbo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PatientName).HasMaxLength(200);
                entity.Property(e => e.DoctorName).HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(50);
            });
        }
    }
}
