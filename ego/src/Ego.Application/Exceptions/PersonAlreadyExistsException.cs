namespace Ego.Application.Exceptions;

public class PersonAlreadyExistsException(string subject) : Exception($"A person with identity subject '{subject}' already exists.")
{
}
