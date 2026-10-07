using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TrackingCode).HasMaxLength(ShortCode.LengthFor(Claim.CodePrefix)).IsRequired();
        builder.Property(c => c.SecretDescription).HasMaxLength(Claim.SecretDescriptionMaxLength).IsRequired();
        builder.Property(c => c.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Score).IsRequired();
        builder.Property(c => c.StaffNote).HasMaxLength(Claim.StaffNoteMaxLength);

        builder.HasOne<UniversityMember>().WithMany().HasForeignKey(c => c.ClaimantUniversityMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UniversityLocation>().WithMany().HasForeignKey(c => c.HandoverPointId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StaffAccount>().WithMany().HasForeignKey(c => c.DecidedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(c => c.Answers, answers =>
        {
            answers.ToTable("claim_secret_answers");
            answers.WithOwner().HasForeignKey("ClaimId");
            answers.HasKey("ClaimId", nameof(SecretAnswer.QuestionId));
            answers.Property(a => a.OptionId).IsRequired();
        });
        builder.Navigation(c => c.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.TrackingCode).IsUnique();
        builder.HasIndex(c => new { c.Status, c.Score });
        builder.HasIndex(c => new { c.ReportId, c.ClaimantUniversityMemberId }).IsUnique();
    }
}
