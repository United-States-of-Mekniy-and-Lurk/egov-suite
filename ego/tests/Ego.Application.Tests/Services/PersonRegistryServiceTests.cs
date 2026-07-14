using Ego.Application.Abstractions;
using Ego.Application.Exceptions;
using Ego.Application.Models;
using Ego.Application.Services;
using Ego.Domain.Entities;
using Ego.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Ego.Application.Tests.Services;

public class PersonRegistryServiceTests
{
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly PersonRegistryService _service;

    public PersonRegistryServiceTests()
    {
        _service = new PersonRegistryService(_personRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPersonExists_ReturnsPerson()
    {
        var person = Person.Create("sub-1", "user1", "User One", "user1@example.com");
        _personRepository.Setup(repo => repo.FindByIdAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var result = await _service.GetByIdAsync(person.Id);

        result.Should().BeSameAs(person);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPersonMissing_ThrowsPersonNotFoundException()
    {
        var id = Guid.NewGuid();
        _personRepository.Setup(repo => repo.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var act = () => _service.GetByIdAsync(id);

        await act.Should().ThrowAsync<PersonNotFoundException>()
            .WithMessage($"Person {id} not found.");
    }

    [Fact]
    public async Task CreateAsync_WhenSubjectIsNew_CreatesAndReturnsPerson()
    {
        var command = new CreatePersonCommand("sub-1", "user1", "User One", "user1@example.com", PersonStatus.Disabled);
        _personRepository.Setup(repo => repo.FindByIdentitySubjectAsync(command.IdentitySubject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var result = await _service.CreateAsync(command);

        result.IdentitySubject.Should().Be(command.IdentitySubject);
        result.PreferredUsername.Should().Be(command.PreferredUsername);
        result.DisplayName.Should().Be(command.DisplayName);
        result.Email.Should().Be(command.Email);
        result.Status.Should().Be(PersonStatus.Disabled);
        _personRepository.Verify(repo => repo.AddAsync(It.Is<Person>(person => person.IdentitySubject == command.IdentitySubject), It.IsAny<CancellationToken>()), Times.Once);
        _personRepository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSubjectAlreadyExists_ThrowsPersonAlreadyExistsException()
    {
        var command = new CreatePersonCommand("sub-1", "user1", "User One", "user1@example.com", null);
        _personRepository.Setup(repo => repo.FindByIdentitySubjectAsync(command.IdentitySubject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Person.Create(command.IdentitySubject, command.PreferredUsername, command.DisplayName, command.Email));

        var act = () => _service.CreateAsync(command);

        await act.Should().ThrowAsync<PersonAlreadyExistsException>()
            .WithMessage("A person with identity subject 'sub-1' already exists.");
    }

    [Fact]
    public async Task PatchAsync_WhenPersonExists_AppliesPatch()
    {
        var person = Person.Create("sub-1", "user1", "User One", "user1@example.com");
        var command = new PatchPersonCommand("updated-user", null, "updated@example.com", PersonStatus.Disabled);
        _personRepository.Setup(repo => repo.FindByIdAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var result = await _service.PatchAsync(person.Id, command);

        result.PreferredUsername.Should().Be("updated-user");
        result.DisplayName.Should().Be("User One");
        result.Email.Should().Be("updated@example.com");
        result.Status.Should().Be(PersonStatus.Disabled);
        _personRepository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WhenPersonMissing_ThrowsPersonNotFoundException()
    {
        var id = Guid.NewGuid();
        _personRepository.Setup(repo => repo.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var act = () => _service.PatchAsync(id, new PatchPersonCommand("user", null, null, null));

        await act.Should().ThrowAsync<PersonNotFoundException>()
            .WithMessage($"Person {id} not found.");
    }
}
