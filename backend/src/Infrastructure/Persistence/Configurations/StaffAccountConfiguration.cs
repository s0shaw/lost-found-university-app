using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class StaffAccountConfiguration : IEntityTypeConfiguration<StaffAccount>
{
    private const int PasswordHashLength = 100;

    public void Configure(EntityTypeBuilder<StaffAccount> builder)
    {
        builder.ToTable("staff_accounts");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PasswordHash).HasMaxLength(PasswordHashLength).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();

        builder.HasOne<UniversityMember>().WithOne().HasForeignKey<StaffAccount>(s => s.UniversityMemberId);
    }
}
