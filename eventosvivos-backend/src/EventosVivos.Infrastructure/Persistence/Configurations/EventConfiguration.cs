using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.MaxCapacity).IsRequired();
        builder.Property(e => e.StartDateTimeUtc).IsRequired();
        builder.Property(e => e.EndDateTimeUtc).IsRequired();
        builder.Property(e => e.TicketPrice).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.HasOne(e => e.Venue)
               .WithMany(v => v.Events)
               .HasForeignKey(e => e.VenueId)
               .OnDelete(DeleteBehavior.Restrict);

        // Private collection backing field
        builder.Navigation(e => e.Reservations)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Index para consultas de superposición (RN02)
        builder.HasIndex(e => new { e.VenueId, e.StartDateTimeUtc, e.EndDateTimeUtc })
               .HasDatabaseName("IX_events_venue_dates");

        // Index para filtros de la lista (RF-02)
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_events_status");
        builder.HasIndex(e => e.Type).HasDatabaseName("IX_events_type");
    }
}
