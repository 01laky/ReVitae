using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace ReVitae.Core.Import.Structured;

internal enum StructuredYamlFlavor
{
    Unknown = 0,
    ReVitaeNative = 1,
    JsonResume = 2
}

/// <summary>Sniffs root keys and converts YAML mapping roots to JSON for shared mappers.</summary>
internal static class YamlStructuredBridge
{
    public static StructuredYamlFlavor SniffRootKeys(string yamlText)
    {
        try
        {
            using var reader = new StringReader(yamlText);
            var stream = new YamlStream();
            stream.Load(reader);
            if (stream.Documents.Count == 0)
            {
                return StructuredYamlFlavor.Unknown;
            }

            if (stream.Documents[0].RootNode is not YamlMappingNode mapping)
            {
                return StructuredYamlFlavor.Unknown;
            }

            if (HasScalarKey(mapping, "revitaeVersion"))
            {
                return StructuredYamlFlavor.ReVitaeNative;
            }

            if (HasScalarKey(mapping, "basics")
                || HasScalarKey(mapping, "work")
                || HasScalarKey(mapping, "education")
                || HasScalarKey(mapping, "skills"))
            {
                return StructuredYamlFlavor.JsonResume;
            }

            return StructuredYamlFlavor.Unknown;
        }
        catch
        {
            return StructuredYamlFlavor.Unknown;
        }
    }

    public static string MappingRootToJson(string yamlText)
    {
        using var reader = new StringReader(yamlText);
        var stream = new YamlStream();
        stream.Load(reader);
        if (stream.Documents.Count == 0)
        {
            throw new InvalidOperationException("Empty YAML stream.");
        }

        if (stream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException("YAML root must be a mapping.");
        }

        return YamlMappingToJson(mapping)!.ToJsonString();
    }

    private static bool HasScalarKey(YamlMappingNode mapping, string key)
    {
        foreach (var entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode scalar && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static JsonNode? YamlToJson(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                return JsonValue.Create(scalar.Value);
            case YamlSequenceNode sequence:
                {
                    var array = new JsonArray();
                    foreach (var child in sequence.Children)
                    {
                        JsonNode? childNode = YamlToJson(child);
                        if (childNode is not null)
                        {
                            array.Add(childNode);
                        }
                        else
                        {
                            array.Add(JsonValue.Create((string?)null));
                        }
                    }

                    return array;
                }
            case YamlMappingNode mapping:
                return YamlMappingToJson(mapping);
            default:
                return JsonValue.Create((string?)null);
        }
    }

    private static JsonObject YamlMappingToJson(YamlMappingNode mapping)
    {
        var obj = new JsonObject();
        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode keyScalar)
            {
                continue;
            }

            var key = keyScalar.Value;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            obj[key] = YamlToJson(pair.Value);
        }

        return obj;
    }
}
