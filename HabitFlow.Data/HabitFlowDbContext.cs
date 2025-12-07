using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HabitFlow.Data;

/// <summary>
/// Main database context for the HabitFlow application.
/// Inherits from IdentityDbContext for ASP.NET Core Identity support.
/// </summary>
public class HabitFlowDbContext : IdentityDbContext<ApplicationUser>
{
    public HabitFlowDbContext(DbContextOptions<HabitFlowDbContext> options)
        : base(options)
    {
    }

    // DbSets for domain entities
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<Checkin> Checkins => Set<Checkin>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all Fluent API configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
