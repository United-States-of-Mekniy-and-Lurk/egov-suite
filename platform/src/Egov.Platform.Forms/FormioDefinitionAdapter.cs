using System.Text.Json;
using System.Text.Json.Nodes;

namespace Egov.Platform.Forms;

public static class FormioDefinitionAdapter
{
    public static JsonObject ConvertLegacy(
        FormDefinition definition,
        JsonSerializerOptions jsonOptions,
        FormioConversionOptions? conversionOptions = null)
    {
        conversionOptions ??= new FormioConversionOptions();
        var components = new JsonArray();
        foreach (var field in definition.Fields)
        {
            var component = new JsonObject
            {
                ["key"] = field.Name,
                ["label"] = field.GetLabel(conversionOptions.DefaultCulture),
                ["type"] = field.Type switch
                {
                    "textarea" => "textarea",
                    "date" => "datetime",
                    "number" => "number",
                    "checkbox" => "checkbox",
                    "select" => "select",
                    _ => "textfield"
                },
                ["input"] = true,
                ["validate"] = new JsonObject { ["required"] = field.Required },
                ["properties"] = new JsonObject
                {
                    [conversionOptions.PersistencePropertyName] = conversionOptions.PersistencePropertyValue,
                    ["labels"] = JsonSerializer.Serialize(field.Labels, jsonOptions)
                }
            };
            if (field.Type == "date") component["enableTime"] = false;
            if (field.Type == "select")
            {
                component["data"] = new JsonObject
                {
                    ["values"] = new JsonArray(field.Options.Select(option => (JsonNode)new JsonObject
                    {
                        ["label"] = option.GetLabel(conversionOptions.DefaultCulture),
                        ["value"] = option.Value
                    }).ToArray())
                };
                component["properties"]!["optionLabels"] = JsonSerializer.Serialize(
                    field.Options.ToDictionary(option => option.Value, option => option.Labels), jsonOptions);
            }
            components.Add(component);
        }

        components.Add(new JsonObject
        {
            ["type"] = "button",
            ["key"] = "submit",
            ["label"] = conversionOptions.SubmitLabel,
            ["action"] = "submit",
            ["theme"] = "primary",
            ["input"] = true
        });

        return new JsonObject
        {
            ["title"] = definition.Title,
            ["display"] = "form",
            ["components"] = components,
            ["properties"] = new JsonObject
            {
                ["titles"] = JsonSerializer.SerializeToNode(definition.Titles, jsonOptions)
            }
        };
    }
}

public sealed class FormioConversionOptions
{
    public string DefaultCulture { get; init; } = "en";
    public string PersistencePropertyName { get; init; } = "persistence";
    public string PersistencePropertyValue { get; init; } = "registry";
    public string SubmitLabel { get; init; } = "Submit";
}