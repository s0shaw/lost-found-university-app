using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Categories;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class ItemReportConfiguration : IEntityTypeConfiguration<ItemReport>
{
    public void Configure(EntityTypeBuilder<ItemReport> builder)
    {
        builder.ToTable("item_reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TrackingCode).HasMaxLength(ShortCode.LengthFor(ItemReport.CodePrefix)).IsRequired();
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(ItemReport.TitleMaxLength).IsRequired();
        builder.Property(r => r.PublicDescription).HasMaxLength(ItemReport.PublicDescriptionMaxLength).IsRequired();
        builder.Property(r => r.SecretDescription).HasMaxLength(ItemReport.SecretDescriptionMaxLength).IsRequired();
        builder.Property(r => r.OccurredOn).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CloseReason).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<UniversityMember>().WithMany().HasForeignKey(r => r.ReporterUniversityMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UniversityLocation>().WithMany().HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UniversityLocation>().WithMany().HasForeignKey(r => r.HandoverPointId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(r => r.SecretAnswers, answers =>
        {
            answers.ToTable("item_report_secret_answers");
            answers.WithOwner().HasForeignKey("ItemReportId");
            answers.HasKey("ItemReportId", nameof(SecretAnswer.QuestionId));
            answers.Property(a => a.OptionId).IsRequired();
        });
        builder.Navigation(r => r.SecretAnswers).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Claims).WithOne().HasForeignKey(c => c.ReportId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Claims).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.TrackingCode).IsUnique();
        builder.HasIndex(r => new { r.Type, r.Status, r.OccurredOn });
    }
}
