using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class UniversityMemberConfiguration : IEntityTypeConfiguration<UniversityMember>
{
    public void Configure(EntityTypeBuilder<UniversityMember> builder)
    {
        builder.ToTable("university_members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.UniversityId).HasMaxLength(ShortCode.LengthFor(UniversityMember.CodePrefix)).IsRequired();
        builder.Property(m => m.FullName).HasMaxLength(UniversityMember.FullNameMaxLength).IsRequired();
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();

        builder.HasIndex(m => m.UniversityId).IsUnique();
    }
}
