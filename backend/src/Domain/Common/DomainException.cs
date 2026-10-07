namespace UniversityLostFound.Domain.Common;

/// <summary>Thrown when a domain rule (invariant) is violated. Mapped to HTTP 400 by the API.</summary>
public sealed class DomainException(string message) : Exception(message);
