using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabitFlow.Data.Configurations;

public class CheckinConfiguration : IEntityTypeConfiguration<Checkin>
{
    public void Configure(EntityTypeBuilder<Checkin> builder)
    {
        builder.HasKey(c => c.Id)
            .IsClustered(false); // Primary key as nonclustered

        // HabitId
        builder.Property(c => c.HabitId)
            .IsRequired();

        // UserId
        builder.Property(c => c.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // LocalDate
        builder.Property(c => c.LocalDate)
            .IsRequired();

        // ActualValue
        builder.Property(c => c.ActualValue)
            .IsRequired();

        // TargetValueSnapshot
        builder.Property(c => c.TargetValueSnapshot)
            .IsRequired();

        // CompletionModeSnapshot
        builder.Property(c => c.CompletionModeSnapshot)
            .IsRequired();

        // HabitTypeSnapshot
        builder.Property(c => c.HabitTypeSnapshot)
            .IsRequired();

        // IsPlanned
        builder.Property(c => c.IsPlanned)
            .IsRequired();

        // CreatedAtUtc
        builder.Property(c => c.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Constraints
        builder.HasIndex(c => new { c.HabitId, c.LocalDate })
            .HasDatabaseName("UQ_Checkins_HabitId_LocalDate")
            .IsUnique();

        // Indexes
        builder.HasIndex(c => new { c.UserId, c.LocalDate, c.HabitId })
            .HasDatabaseName("IX_Checkins_UserId_LocalDate_HabitId")
            .IsClustered();

        builder.HasIndex(c => new { c.HabitId, c.LocalDate })
            .HasDatabaseName("IX_Checkins_HabitId_LocalDate")
            .IncludeProperties(c => new { c.ActualValue, c.TargetValueSnapshot, c.CompletionModeSnapshot, c.HabitTypeSnapshot, c.IsPlanned });
    }
}
