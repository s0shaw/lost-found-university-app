namespace UniversityLostFound.Application.Common;

/// <summary>Mapped to HTTP 404 by the API.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with id '{id}' was not found.")
    {
    }

    // For lookups where naming the missing thing would leak whether it exists.
    public NotFoundException(string message)
        : base(message)
    {
    }
}
