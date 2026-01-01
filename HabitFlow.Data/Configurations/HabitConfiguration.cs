using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabitFlow.Data.Configurations;

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.HasKey(h => h.Id);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Habits_DaysOfWeekMask", "DaysOfWeekMask BETWEEN 1 AND 127");
            t.HasCheckConstraint("CK_Habits_TargetValue", "TargetValue BETWEEN 1 AND 1000");
        });

        // UserId
        builder.Property(h => h.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // Title
        builder.Property(h => h.Title)
            .IsRequired()
            .HasMaxLength(80);

        // Description
        builder.Property(h => h.Description)
            .HasMaxLength(1000);

        // Type
        builder.Property(h => h.Type)
            .IsRequired()
            .HasConversion<byte>();

        // CompletionMode
        builder.Property(h => h.CompletionMode)
            .IsRequired()
            .HasConversion<byte>();

        // DaysOfWeekMask
        builder.Property(h => h.DaysOfWeekMask)
            .IsRequired();

        // TargetValue
        builder.Property(h => h.TargetValue)
            .IsRequired()
            .HasDefaultValue((short)1);

        // TargetUnit
        builder.Property(h => h.TargetUnit)
            .HasMaxLength(32);

        // DeadlineDate
        builder.Property(h => h.DeadlineDate)
            .IsRequired(false);

        // CreatedAtUtc
        builder.Property(h => h.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(h => new { h.UserId, h.CreatedAtUtc })
            .HasDatabaseName("IX_Habits_UserId_CreatedAtUtc")
            .IsDescending(false, true)
            .IncludeProperties(h => new { h.Title, h.Type, h.DaysOfWeekMask, h.TargetValue, h.CompletionMode, h.DeadlineDate });

        // Relations
        builder.HasMany(h => h.Checkins)
            .WithOne(c => c.Habit)
            .HasForeignKey(c => c.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Notifications)
            .WithOne(n => n.Habit)
            .HasForeignKey(n => n.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
