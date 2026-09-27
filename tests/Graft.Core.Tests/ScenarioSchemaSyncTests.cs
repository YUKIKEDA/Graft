using System.Reflection;
using System.Text.Json;
using Graft.Core.Scenario;

namespace Graft.Core.Tests;

public sealed class ScenarioSchemaSyncTests
{
    /// <summary>
    /// The schema action set and the parser action set are the same.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - docs/scenario.schema.json is in the repo
    /// - ScenarioActions exposes one public string constant per action
    ///
    /// Steps:
    /// - Read each $defs action const and each oneOf step ref
    /// - Read ScenarioActions constants
    ///
    /// Expected:
    /// - The three sets are equal
    /// </remarks>
    [Fact]
    public void SchemaActions_MatchScenarioActions()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepoFile("docs", "scenario.schema.json")));
        var defs = schema.RootElement.GetProperty("$defs");
        var schemaActions = ActionConstants(defs);
        var oneOfActions = OneOfActions(defs);
        var parserActions = typeof(ScenarioActions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(Order(parserActions), Order(schemaActions));
        Assert.Equal(Order(parserActions), Order(oneOfActions));
    }

    /// <summary>
    /// Every sample scenario matches the schema's closed property set and parses.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - tests/sample-apps/SampleWpfApp.Tests/Scenarios contains the sample JSON files
    ///
    /// Steps:
    /// - For each file, check each step's required properties and reject unknown properties
    /// - ScenarioJson.ParseFile
    ///
    /// Expected:
    /// - Every file parses
    /// </remarks>
    [Fact]
    public void SampleScenarios_MatchSchemaAndParse()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RepoFile("docs", "scenario.schema.json")));
        var defs = schema.RootElement.GetProperty("$defs");
        var directory = RepoFile("tests", "sample-apps", "SampleWpfApp.Tests", "Scenarios");
        var files = Directory.GetFiles(directory, "*.scenario.json");
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var step in document.RootElement.GetProperty("steps").EnumerateArray())
            {
                var action = step.GetProperty("action").GetString();
                Assert.False(string.IsNullOrEmpty(action));
                var definition = defs.GetProperty(action!);
                var allowed = definition
                    .GetProperty("properties")
                    .EnumerateObject()
                    .Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var required in definition.GetProperty("required").EnumerateArray())
                {
                    Assert.True(
                        step.TryGetProperty(required.GetString()!, out _),
                        $"{Path.GetFileName(file)} step {action} is missing {required.GetString()}."
                    );
                }

                foreach (var property in step.EnumerateObject())
                {
                    Assert.True(allowed.Contains(property.Name), $"{Path.GetFileName(file)} step {action} has unknown property {property.Name}.");
                }
            }

            var parsed = ScenarioJson.ParseFile(file);
            Assert.NotEmpty(parsed.Operations);
        }
    }

    private static HashSet<string> ActionConstants(JsonElement defs)
    {
        var actions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in defs.EnumerateObject())
        {
            if (
                definition.Value.TryGetProperty("properties", out var properties)
                && properties.TryGetProperty("action", out var action)
                && action.TryGetProperty("const", out var constant)
            )
            {
                actions.Add(constant.GetString()!);
            }
        }

        return actions;
    }

    private static HashSet<string> OneOfActions(JsonElement defs)
    {
        var actions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var branch in defs.GetProperty("step").GetProperty("oneOf").EnumerateArray())
        {
            var reference = branch.GetProperty("$ref").GetString()!;
            actions.Add(reference["#/$defs/".Length..]);
        }

        return actions;
    }

    private static string[] Order(HashSet<string> values) => values.Order(StringComparer.Ordinal).ToArray();

    private static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate " + string.Join('/', parts) + " from the test base directory.");
    }
}
