using Microsoft.EntityFrameworkCore;

namespace HRMCPServer.Data;

public class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var employeeEntity = modelBuilder.Entity<Employee>();

        employeeEntity.HasKey(c => c.Id);

        employeeEntity.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        employeeEntity.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        employeeEntity.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        employeeEntity.HasIndex(c => c.Email)
            .IsUnique();

        employeeEntity.Property(c => c.CurrentRole)
            .HasMaxLength(200);

        employeeEntity.Property(c => c.SpokenLanguagesData)
            .IsRequired();

        employeeEntity.Property(c => c.SkillsData)
            .IsRequired();
    }
}
