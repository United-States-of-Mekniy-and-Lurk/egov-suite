namespace Ego.Application.Exceptions;

public class PersonNotFoundException(object id) : Exception($"Person {id} not found.")
{
}
