using System.Security.Cryptography;
using CitizenService.Application.Interfaces;

namespace CitizenService.Application.Services;

public class RandomCitizenNumberGenerator : ICitizenNumberGenerator
{
    public string Generate()
    {
        var encoded = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        return $"CIT-{string.Join('-', Enumerable.Range(0, 6).Select(index => encoded.Substring(index * 4, 4)))}";
    }
}