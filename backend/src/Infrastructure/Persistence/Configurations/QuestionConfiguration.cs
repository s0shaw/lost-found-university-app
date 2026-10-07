using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityLostFound.Domain.Categories;

namespace UniversityLostFound.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).HasMaxLength(Question.TextMaxLength).IsRequired();
        builder.Property(q => q.DisplayOrder).IsRequired();

        builder.HasOne<Category>().WithMany().HasForeignKey(q => q.CategoryId);
    }
}
