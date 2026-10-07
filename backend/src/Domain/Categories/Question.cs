using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Categories;

public sealed class Question : Entity
{
    public const int TextMaxLength = 200;

    public Question(Guid categoryId, string text, int displayOrder)
    {
        CategoryId = categoryId;
        Text = text;
        DisplayOrder = displayOrder;
    }

    public Guid CategoryId { get; private set; }
    public string Text { get; private set; }
    public int DisplayOrder { get; private set; }
}
