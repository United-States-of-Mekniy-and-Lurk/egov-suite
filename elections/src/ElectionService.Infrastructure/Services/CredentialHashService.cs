using System.Security.Cryptography;
using System.Text;
using ElectionService.Application.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ElectionService.Infrastructure.Services;

public sealed class VotingOptions
{
    public const string SectionName = "Voting";
    public string ActiveKeyVersion { get; set; } = string.Empty;
    public Dictionary<string, string> CredentialHashKeys { get; set; } = [];
}

public sealed class CredentialHashService(IOptions<VotingOptions> options) : ICredentialHashService
{
    private readonly VotingOptions options = options.Value;

    public string ActiveKeyVersion => options.ActiveKeyVersion;

    public string HashCitizen(Guid electionId, Guid personId, string keyVersion) =>
        Hash($"citizen:{electionId:N}:{personId:N}", keyVersion);

    public (string Token, string Hash) CreateInvitation(Guid electionId, string keyVersion)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (token, HashInvitation(electionId, token, keyVersion));
    }

    public string HashInvitation(Guid electionId, string token, string keyVersion)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Invitation token is required.", nameof(token));
        return Hash($"invitation:{electionId:N}:{token}", keyVersion);
    }

    private string Hash(string value, string keyVersion)
    {
        if (!options.CredentialHashKeys.TryGetValue(keyVersion, out var secret))
            throw new InvalidOperationException($"Credential hash key version '{keyVersion}' is not configured.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(value)));
    }
}