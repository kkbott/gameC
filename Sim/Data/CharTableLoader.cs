using System.Text.Json;

namespace TianziGe.Sim.Data;

/// <summary>字表条目（F1）。只放 T-003 数据管线所需的字段；完整 schema 校验归 T-007。</summary>
public sealed record CharEntry(
    string Id,
    string Char,
    IReadOnlyList<string> Components,
    string Category,
    string Template,
    int Tier);

/// <summary>部件条目（C2）。targets 是发牌可达性的依据。</summary>
public sealed record ComponentEntry(
    int GlyphId,
    string Glyph,
    int Rarity,
    string Family,
    IReadOnlyList<string> Targets);

/// <summary>字表 + 部件表的只读解析结果。</summary>
public sealed record CharTable(
    IReadOnlyList<CharEntry> Chars,
    IReadOnlyList<ComponentEntry> Components);

/// <summary>
/// 最小 JSON → Sim 解析器（T-003）。
/// 输入是 JSON 文本，输出只读集合；不依赖 Godot，也不做完整 schema 校验。
/// 缺字段 / 类型错误时抛 InvalidDataException，并带上字段路径，便于构建期定位。
/// </summary>
public static class CharTableLoader
{
    /// <summary>解析 <c>Client/Data/char-table.json</c> 的 <c>chars</c> 数组。</summary>
    public static IReadOnlyList<CharEntry> LoadChars(string json)
    {
        using JsonDocument document = ParseDocument(json, "字表");
        JsonElement root = document.RootElement;
        JsonElement charsElement = RequireProperty(root, "chars", "$");
        if (charsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.chars 必须是数组。");
        }

        var chars = new List<CharEntry>(charsElement.GetArrayLength());
        int index = 0;
        foreach (JsonElement entry in charsElement.EnumerateArray())
        {
            chars.Add(ParseChar(entry, $"$.chars[{index}]"));
            index++;
        }

        return chars.AsReadOnly();
    }

    /// <summary>解析 <c>Client/Data/component-registry.json</c> 的 <c>components</c> 数组。</summary>
    public static IReadOnlyList<ComponentEntry> LoadComponents(string json)
    {
        using JsonDocument document = ParseDocument(json, "部件表");
        JsonElement root = document.RootElement;
        JsonElement componentsElement = RequireProperty(root, "components", "$");
        if (componentsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.components 必须是数组。");
        }

        var components = new List<ComponentEntry>(componentsElement.GetArrayLength());
        int index = 0;
        foreach (JsonElement entry in componentsElement.EnumerateArray())
        {
            components.Add(ParseComponent(entry, $"$.components[{index}]"));
            index++;
        }

        return components.AsReadOnly();
    }

    /// <summary>两份数据源的组合入口；View 侧传入从 res://Data 读到的文本。</summary>
    public static CharTable Load(string charTableJson, string componentRegistryJson)
    {
        ArgumentNullException.ThrowIfNull(charTableJson);
        ArgumentNullException.ThrowIfNull(componentRegistryJson);

        return new CharTable(LoadChars(charTableJson), LoadComponents(componentRegistryJson));
    }

    private static JsonDocument ParseDocument(string json, string label)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new InvalidDataException($"{label} JSON 根节点必须是对象。");
            }

            return document;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{label} JSON 不是合法 JSON。", exception);
        }
    }

    private static CharEntry ParseChar(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{path} 必须是对象。");
        }

        string id = RequireString(element, "id", path);
        string character = RequireString(element, "char", path);
        string category = RequireString(element, "category", path);
        string template = RequireString(element, "template", path);
        int tier = RequireInt(element, "tier", path);
        IReadOnlyList<string> components = RequireStringArray(element, "components", path);

        return new CharEntry(id, character, components, category, template, tier);
    }

    private static ComponentEntry ParseComponent(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{path} 必须是对象。");
        }

        int glyphId = RequireInt(element, "glyphId", path);
        string glyph = RequireString(element, "glyph", path);
        int rarity = RequireInt(element, "rarity", path);
        string family = RequireString(element, "family", path);
        IReadOnlyList<string> targets = RequireStringArray(element, "targets", path);

        return new ComponentEntry(glyphId, glyph, rarity, family, targets);
    }

    private static JsonElement RequireProperty(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            throw new InvalidDataException($"{path}.{name} 缺少字段（或为 null）。");
        }

        return value;
    }

    private static string RequireString(JsonElement element, string name, string path)
    {
        JsonElement value = RequireProperty(element, name, path);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
        {
            throw new InvalidDataException($"{path}.{name} 必须是非空字符串。");
        }

        return value.GetString()!;
    }

    private static int RequireInt(JsonElement element, string name, string path)
    {
        JsonElement value = RequireProperty(element, name, path);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
        {
            throw new InvalidDataException($"{path}.{name} 必须是整数。");
        }

        return result;
    }

    private static IReadOnlyList<string> RequireStringArray(JsonElement element, string name, string path)
    {
        JsonElement value = RequireProperty(element, name, path);
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{path}.{name} 必须是字符串数组。");
        }

        var items = new List<string>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(item.GetString()))
            {
                throw new InvalidDataException($"{path}.{name}[{index}] 必须是非空字符串。");
            }

            items.Add(item.GetString()!);
            index++;
        }

        return items.AsReadOnly();
    }
}
