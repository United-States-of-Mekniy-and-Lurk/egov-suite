using Ego.Application.Abstractions;
using Ego.Application.Models;
using Ego.Application.Services;
using Ego.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Ego.Application.Tests.Services;

public class IdentitySynchronizerTests
{
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly IdentitySynchronizer _service;

    public IdentitySynchronizerTests()
    {
        _service = new IdentitySynchronizer(_personRepository.Object);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenPersonExists_UpdatesAndReturnsPerson()
    {
        var person = Person.Create("sub-1", "user1", "User One", "user1@example.com");
        var claims = new IdentityClaims("sub-1", "updated-user", "Updated User", "updated@example.com");
        _personRepository.Setup(repo => repo.FindByIdentitySubjectAsync(claims.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        var result = await _service.SynchronizeAsync(claims);

        result.Should().BeSameAs(person);
        result.PreferredUsername.Should().Be(claims.PreferredUsername);
        result.DisplayName.Should().Be(claims.DisplayName);
        result.Email.Should().Be(claims.Email);
        _personRepository.Verify(repo => repo.AddAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()), Times.Never);
        _personRepository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenPersonMissing_CreatesAndReturnsPerson()
    {
        var claims = new IdentityClaims("sub-1", "user1", "User One", "user1@example.com");
        _personRepository.Setup(repo => repo.FindByIdentitySubjectAsync(claims.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var result = await _service.SynchronizeAsync(claims);

        result.IdentitySubject.Should().Be(claims.Subject);
        result.PreferredUsername.Should().Be(claims.PreferredUsername);
        result.DisplayName.Should().Be(claims.DisplayName);
        result.Email.Should().Be(claims.Email);
        _personRepository.Verify(repo => repo.AddAsync(It.Is<Person>(person => person.IdentitySubject == claims.Subject), It.IsAny<CancellationToken>()), Times.Once);
        _personRepository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
