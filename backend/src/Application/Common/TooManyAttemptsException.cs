namespace UniversityLostFound.Application.Common;

// Mapped to HTTP 429 by ApiExceptionHandler.
public sealed class TooManyAttemptsException(string message) : Exception(message);
