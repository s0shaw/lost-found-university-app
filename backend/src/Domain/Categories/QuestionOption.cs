using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Categories;

public sealed class QuestionOption : Entity
{
    public const int TextMaxLength = 80;

    public QuestionOption(Guid questionId, string text, int displayOrder)
    {
        QuestionId = questionId;
        Text = text;
        DisplayOrder = displayOrder;
    }

    public Guid QuestionId { get; private set; }
    public string Text { get; private set; }
    public int DisplayOrder { get; private set; }
}
