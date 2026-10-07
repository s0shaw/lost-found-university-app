namespace UniversityLostFound.Domain.Common;

/// <summary>Base class for entities: identity is the <see cref="Id"/>, not the field values.</summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
}
