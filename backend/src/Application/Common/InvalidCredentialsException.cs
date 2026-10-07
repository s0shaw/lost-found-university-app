namespace UniversityLostFound.Application.Common;

// Mapped to HTTP 401 by ApiExceptionHandler.
public sealed class InvalidCredentialsException(string message) : Exception(message);
