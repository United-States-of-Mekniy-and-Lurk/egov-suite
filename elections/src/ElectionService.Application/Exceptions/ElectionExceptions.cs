namespace ElectionService.Application.Exceptions;

public class ElectionValidationException(string message) : Exception(message);
public sealed class ElectionNotFoundException(string message) : ElectionValidationException(message);
public sealed class ElectionConflictException(string message) : ElectionValidationException(message);
public sealed class ElectionForbiddenException(string message) : ElectionValidationException(message);