using System.Text.Json;
using System.Text.Json.Nodes;

namespace CitizenService.Web.Models;

public static class FormioDefinitionAdapter
{
    public static JsonObject ConvertLegacy(ApplicationFormDefinition definition, JsonSerializerOptions jsonOptions)
    {
        var components = new JsonArray();
        foreach (var field in definition.Fields)
        {
            var component = new JsonObject
            {
                ["key"] = field.Name,
                ["label"] = field.GetLabel("en"),
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
                    ["persistence"] = "registry",
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
                        ["label"] = option.GetLabel("en"),
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
            ["label"] = "Submit",
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