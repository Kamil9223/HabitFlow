using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HabitFlow.Data;

/// <summary>
/// Database context factory for EF Core tools (design-time).
/// Enables migration generation without running the application.
/// </summary>
public class HabitFlowDbContextFactory : IDesignTimeDbContextFactory<HabitFlowDbContext>
{
    public HabitFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HabitFlowDbContext>();

        // Connection string for design-time (local SQL Server)
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HabitFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new HabitFlowDbContext(optionsBuilder.Options);
    }
}
