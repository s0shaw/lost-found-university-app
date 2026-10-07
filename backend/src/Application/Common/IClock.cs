namespace UniversityLostFound.Application.Common;

/// <summary>Abstracts "now" so use cases are testable without depending on the wall clock.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
