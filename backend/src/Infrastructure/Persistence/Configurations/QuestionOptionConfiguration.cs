using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Categories;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("question_options");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Text).HasMaxLength(QuestionOption.TextMaxLength).IsRequired();
        builder.Property(o => o.DisplayOrder).IsRequired();

        builder.HasOne<Question>().WithMany().HasForeignKey(o => o.QuestionId);
    }
}
