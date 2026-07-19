using System.Text.RegularExpressions;
using CitizenService.Application.Services;
using FluentAssertions;

namespace CitizenService.Application.Tests.Services;

public class RandomCitizenNumberGeneratorTests
{
    [Fact]
    public void Generate_ReturnsNonEnumerableCitizenNumbers()
    {
        var generator = new RandomCitizenNumberGenerator();

        var generated = Enumerable.Range(0, 1_000)
            .Select(_ => generator.Generate())
            .ToList();

        generated.Should().OnlyContain(number =>
            Regex.IsMatch(number, "^CIT-(?:[0-9A-F]{4}-){5}[0-9A-F]{4}$"));
        generated.Should().OnlyHaveUniqueItems();
    }
}