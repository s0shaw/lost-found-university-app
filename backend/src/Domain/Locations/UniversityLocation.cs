using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Locations;

public sealed class UniversityLocation : Entity
{
    public const int NameMaxLength = 120;

    public UniversityLocation(string name, bool isHandoverPoint)
    {
        Name = name;
        IsHandoverPoint = isHandoverPoint;
    }

    public string Name { get; private set; }
    public bool IsHandoverPoint { get; private set; }
}
