using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Categories;

public sealed class Category : Entity
{
    public const int NameMaxLength = 80;

    public Category(string name, int displayOrder)
    {
        Name = name;
        DisplayOrder = displayOrder;
    }

    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }
}
