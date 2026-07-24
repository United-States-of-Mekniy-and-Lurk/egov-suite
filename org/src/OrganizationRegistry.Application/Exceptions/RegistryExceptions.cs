namespace OrganizationRegistry.Application.Exceptions;

public sealed class RegistryValidationException(string message) : Exception(message);
public sealed class RegistryNotFoundException(string message) : Exception(message);
public sealed class RegistryForbiddenException(string message) : Exception(message);
public sealed class RegistryConflictException(string message) : Exception(message);