using System.Text.Json;
using Egov.Platform.Forms;
using FluentAssertions;

namespace Egov.Platform.Tests;

public sealed class FormioDefinitionAdapterTests
{
    [Fact]
    public void ConvertLegacy_UsesConsumerConventions()
    {
        var definition = new FormDefinition
        {
            Title = "Registration",
            Fields =
            [
                new FormFieldDefinition
                {
                    Name = "companyName",
                    Label = "Company name",
                    Labels = new Dictionary<string, string> { ["cs"] = "Název společnosti" },
                    Required = true
                }
            ]
        };
        var options = new FormioConversionOptions
        {
            DefaultCulture = "cs",
            PersistencePropertyName = "destination",
            PersistencePropertyValue = "organisation-register",
            SubmitLabel = "File registration"
        };

        var converted = FormioDefinitionAdapter.ConvertLegacy(
            definition,
            new JsonSerializerOptions(),
            options);
        var field = converted["components"]![0]!;
        var submit = converted["components"]![1]!;

        field["label"]!.GetValue<string>().Should().Be("Název společnosti");
        field["properties"]!["destination"]!.GetValue<string>()
            .Should().Be("organisation-register");
        submit["label"]!.GetValue<string>().Should().Be("File registration");
    }
}