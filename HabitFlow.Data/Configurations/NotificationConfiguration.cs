using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabitFlow.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        // UserId
        builder.Property(n => n.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // HabitId
        builder.Property(n => n.HabitId)
            .IsRequired();

        // LocalDate
        builder.Property(n => n.LocalDate)
            .IsRequired();

        // Type
        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<byte>();

        // Content
        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(1024);

        // AiStatus
        builder.Property(n => n.AiStatus)
            .IsRequired(false)
            .HasConversion<byte>();

        // AiError
        builder.Property(n => n.AiError)
            .HasMaxLength(512);

        // CreatedAtUtc
        builder.Property(n => n.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Constraints
        builder.HasIndex(n => new { n.HabitId, n.LocalDate, n.Type })
            .HasDatabaseName("UQ_Notifications_HabitId_LocalDate_Type")
            .IsUnique();

        // Indexes
        builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc })
            .HasDatabaseName("IX_Notifications_UserId_CreatedAtUtc")
            .IsDescending(false, true)
            .IncludeProperties(n => new { n.Content, n.Type, n.HabitId, n.LocalDate });
    }
}
