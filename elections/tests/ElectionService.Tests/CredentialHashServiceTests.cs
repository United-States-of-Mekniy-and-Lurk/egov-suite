using ElectionService.Infrastructure;
using ElectionService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElectionService.Tests;

public sealed class CredentialHashServiceTests
{
    private const string Secret = "a-test-secret-with-at-least-32-characters";
    private const string RotatedSecret = "a-rotated-secret-with-at-least-32-characters";

    [Fact]
    public void CitizenAndInvitationHashes_AreDeterministicAndElectionScoped()
    {
        var service = CreateService();
        var firstElection = Guid.NewGuid();
        var secondElection = Guid.NewGuid();
        var personId = Guid.NewGuid();
        const string token = "plain-invitation-token";

        Assert.Equal(service.HashCitizen(firstElection, personId, "v1"), service.HashCitizen(firstElection, personId, "v1"));
        Assert.NotEqual(service.HashCitizen(firstElection, personId, "v1"), service.HashCitizen(secondElection, personId, "v1"));
        Assert.Equal(service.HashInvitation(firstElection, token, "v1"), service.HashInvitation(firstElection, token, "v1"));
        Assert.NotEqual(service.HashInvitation(firstElection, token, "v1"), service.HashInvitation(secondElection, token, "v1"));
    }

    [Fact]
    public void CreateInvitation_ReturnsPlaintextTokenDifferentFromPersistedHash()
    {
        var service = CreateService();
        var electionId = Guid.NewGuid();

        var invitation = service.CreateInvitation(electionId, "v1");

        Assert.NotEqual(invitation.Token, invitation.Hash);
        Assert.Equal(service.HashInvitation(electionId, invitation.Token, "v1"), invitation.Hash);
    }

    [Fact]
    public void RotatedKeyRing_KeepsOlderElectionHashesResolvable()
    {
        var service = CreateService("v2", new Dictionary<string, string>
        {
            ["v1"] = Secret,
            ["v2"] = RotatedSecret
        });
        var electionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        Assert.Equal("v2", service.ActiveKeyVersion);
        Assert.NotEqual(
            service.HashCitizen(electionId, personId, "v1"),
            service.HashCitizen(electionId, personId, "v2"));
        Assert.Equal(
            service.HashCitizen(electionId, personId, "v1"),
            service.HashCitizen(electionId, personId, "v1"));
    }

    [Fact]
    public void InfrastructureConfiguration_RejectsCredentialHashSecretShorterThan32Characters()
    {
        var values = new Dictionary<string, string?>
        {
            ["Voting:ActiveKeyVersion"] = "v1",
            ["Voting:CredentialHashKeys:v1"] = "too-short",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElectionInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<VotingOptions>>().Value);
    }

    private static CredentialHashService CreateService(
        string activeKeyVersion = "v1",
        Dictionary<string, string>? keys = null) =>
        new(Options.Create(new VotingOptions
        {
            ActiveKeyVersion = activeKeyVersion,
            CredentialHashKeys = keys ?? new Dictionary<string, string> { ["v1"] = Secret }
        }));
}