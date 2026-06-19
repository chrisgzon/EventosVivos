using EventosVivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.BuyerName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.BuyerEmail).IsRequired().HasMaxLength(255);
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.ReservationCode).HasMaxLength(10);
        builder.Property(r => r.IsLostOnCancellation).IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasOne(r => r.Event)
               .WithMany(e => e.Reservations)
               .HasForeignKey(r => r.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.EventId).HasDatabaseName("IX_reservations_eventId");
        builder.HasIndex(r => r.ReservationCode)
               .IsUnique()
               .HasFilter("\"ReservationCode\" IS NOT NULL")
               .HasDatabaseName("IX_reservations_code_unique");
    }
}
