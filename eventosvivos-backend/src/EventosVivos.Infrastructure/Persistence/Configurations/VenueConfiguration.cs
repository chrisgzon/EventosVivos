using EventosVivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever(); // IDs fijos del seed
        builder.Property(v => v.Name).IsRequired().HasMaxLength(150);
        builder.Property(v => v.City).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Capacity).IsRequired();
    }
}
