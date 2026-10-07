using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Locations;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class UniversityLocationConfiguration : IEntityTypeConfiguration<UniversityLocation>
{
    public void Configure(EntityTypeBuilder<UniversityLocation> builder)
    {
        builder.ToTable("university_locations");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(UniversityLocation.NameMaxLength).IsRequired();
        builder.Property(l => l.IsHandoverPoint).IsRequired();

        builder.HasIndex(l => l.Name).IsUnique();
    }
}
