using System.Text;
using System.Text.Json;

namespace TianziGe.Sim;

/// <summary>
/// <see cref="FarmState"/> 的规范化 JSON 读写（F3 §4.1 / F2 §4.2）。
///
/// 规范形式：键按**字典序**、枚举写字符串名、整数/布尔、集合按稳定键排序。
/// 同一状态两次序列化必须逐字节一致，供 <see cref="FarmStateHash"/> 做 FNV-1a。
/// 文件落盘、原子写入与存档外壳（charTableVersion / gameVersion）归 F4，不在本类。
/// </summary>
public static class FarmStateSerializer
{
    /// <summary>序列化为规范化 JSON（无浮点、无时间戳、无引擎对象）。</summary>
    public static string Serialize(FarmState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            // 键严格按 Ordinal 字典序写出（F2 §4.2 / F3 §4.1）。
            WriteCodex(writer, state.Codex);
            WriteHand(writer, state.Hand);
            writer.WriteNumber("money", state.Money);
            WritePlots(writer, state.Plots);
            writer.WriteNumber("qiOfSui", state.QiOfSui);
            writer.WriteNumber("rngState", state.RngState);
            writer.WriteNumber("roundIndex", state.RoundIndex);
            writer.WriteNumber("saveVersion", state.SaveVersion);
            WriteSeason(writer, state.Season);
            writer.WriteNumber("seasonIndex", state.SeasonIndex);
            writer.WriteNumber("totalSteps", state.TotalSteps);
            WriteVillage(writer, state.Village);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// 从规范化 JSON 还原状态。只接受当前 schema 版本 1；
    /// 更高版本拒绝（F3 E1），更低版本需要 F4 的迁移链（F3 E2），均抛 <see cref="InvalidDataException"/>。
    /// </summary>
    public static FarmState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("存档 JSON 为空。");
        }

        using JsonDocument document = ParseDocument(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("存档 JSON 根节点必须是对象。");
        }

        int saveVersion = RequireInt32(root, "saveVersion", "$");
        if (saveVersion > FarmState.CurrentSaveVersion)
        {
            throw new InvalidDataException(
                $"存档版本 {saveVersion} 高于当前程序支持的 {FarmState.CurrentSaveVersion}（F3 E1）。");
        }

        if (saveVersion != FarmState.CurrentSaveVersion)
        {
            throw new InvalidDataException(
                $"存档版本 {saveVersion} 需要迁移链，而迁移链归 F4（F3 E2）。");
        }

        var state = new FarmState
        {
            SaveVersion = saveVersion,
            RngState = RequireUInt64(root, "rngState", "$"),
            SeasonIndex = RequireInt32(root, "seasonIndex", "$"),
            RoundIndex = RequireInt32(root, "roundIndex", "$"),
            TotalSteps = RequireInt32(root, "totalSteps", "$"),
            Money = RequireInt32(root, "money", "$"),
            QiOfSui = RequireInt32(root, "qiOfSui", "$"),
            Plots = RequirePlots(root),
            Hand = RequireHand(root),
            Village = RequireVillage(root),
            Codex = RequireCodex(root),
            Season = RequireSeason(root),
        };

        return state;
    }

    private static void WriteCodex(Utf8JsonWriter writer, CodexState codex)
    {
        writer.WriteStartObject("codex");
        writer.WriteStartArray("learned");

        foreach ((GlyphId a, GlyphId b) in codex.Learned
                     .OrderBy(pair => pair.A.Value)
                     .ThenBy(pair => pair.B.Value))
        {
            writer.WriteStartObject();
            writer.WriteNumber("a", a.Value);
            writer.WriteNumber("b", b.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteHand(Utf8JsonWriter writer, HandState hand)
    {
        writer.WriteStartObject("hand");
        writer.WriteNumber("capacity", hand.Capacity);
        WriteGlyphList(writer, "drawPile", hand.DrawPile);
        WriteGlyphList(writer, "tiles", hand.Tiles);
        writer.WriteEndObject();
    }

    private static void WritePlots(Utf8JsonWriter writer, Plot[] plots)
    {
        writer.WriteStartArray("plots");

        foreach (Plot plot in plots)
        {
            writer.WriteStartObject();
            writer.WriteNumber("appliedRound", plot.AppliedRound);
            if (plot.Char.Value is null)
            {
                writer.WriteNull("char");
            }
            else
            {
                writer.WriteString("char", plot.Char.Value);
            }

            writer.WriteNumber("remainingRounds", plot.RemainingRounds);
            writer.WriteNumber("soil", plot.Soil);
            writer.WriteString("status", plot.Status.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteSeason(Utf8JsonWriter writer, SeasonState season)
    {
        writer.WriteStartObject("season");
        writer.WriteNumber("nightsResolved", season.NightsResolved);
        writer.WriteBoolean("pendingNight", season.PendingNight);
        writer.WriteEndObject();
    }

    private static void WriteVillage(Utf8JsonWriter writer, VillageState village)
    {
        writer.WriteStartObject("village");
        writer.WriteNumber("count", village.Count);
        writer.WriteStartArray("members");

        foreach (Villager member in village.Members)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", member.Id.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteGlyphList(Utf8JsonWriter writer, string name, List<GlyphId> glyphs)
    {
        writer.WriteStartArray(name);
        foreach (GlyphId glyph in glyphs)
        {
            writer.WriteNumberValue(glyph.Value);
        }

        writer.WriteEndArray();
    }

    private static HandState RequireHand(JsonElement root)
    {
        JsonElement handElement = RequireObject(root, "hand", "$");
        return new HandState
        {
            Capacity = RequireInt32(handElement, "capacity", "$.hand"),
            DrawPile = RequireGlyphList(handElement, "drawPile", "$.hand"),
            Tiles = RequireGlyphList(handElement, "tiles", "$.hand"),
        };
    }

    private static VillageState RequireVillage(JsonElement root)
    {
        JsonElement villageElement = RequireObject(root, "village", "$");
        var village = new VillageState
        {
            Count = RequireInt32(villageElement, "count", "$.village"),
        };

        JsonElement membersElement = RequireProperty(villageElement, "members", "$.village");
        if (membersElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.village.members 必须是数组。");
        }

        int index = 0;
        foreach (JsonElement memberElement in membersElement.EnumerateArray())
        {
            string path = $"$.village.members[{index}]";
            if (memberElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"{path} 必须是对象。");
            }

            village.Members.Add(new Villager(new EntityId(RequireInt32(memberElement, "id", path))));
            index++;
        }

        return village;
    }

    private static CodexState RequireCodex(JsonElement root)
    {
        JsonElement codexElement = RequireObject(root, "codex", "$");
        JsonElement learnedElement = RequireProperty(codexElement, "learned", "$.codex");
        if (learnedElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.codex.learned 必须是数组。");
        }

        var codex = new CodexState();
        int index = 0;
        foreach (JsonElement pairElement in learnedElement.EnumerateArray())
        {
            string path = $"$.codex.learned[{index}]";
            if (pairElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"{path} 必须是对象。");
            }

            codex.Learned.Add((
                new GlyphId(RequireInt32(pairElement, "a", path)),
                new GlyphId(RequireInt32(pairElement, "b", path))));
            index++;
        }

        return codex;
    }

    private static SeasonState RequireSeason(JsonElement root)
    {
        JsonElement seasonElement = RequireObject(root, "season", "$");
        return new SeasonState
        {
            NightsResolved = RequireInt32(seasonElement, "nightsResolved", "$.season"),
            PendingNight = RequireBoolean(seasonElement, "pendingNight", "$.season"),
        };
    }

    private static Plot[] RequirePlots(JsonElement root)
    {
        JsonElement plotsElement = RequireProperty(root, "plots", "$");
        if (plotsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.plots 必须是数组。");
        }

        int length = plotsElement.GetArrayLength();
        if (length != FarmState.PlotCapacity)
        {
            throw new InvalidDataException(
                $"$.plots 必须包含 {FarmState.PlotCapacity} 块田（实际 {length}）；扩缩容迁移归 F4 / F3 E4。");
        }

        var plots = new Plot[FarmState.PlotCapacity];
        int index = 0;
        foreach (JsonElement plotElement in plotsElement.EnumerateArray())
        {
            string path = $"$.plots[{index}]";
            if (plotElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"{path} 必须是对象。");
            }

            plots[index] = new Plot
            {
                AppliedRound = RequireInt32(plotElement, "appliedRound", path),
                Char = RequireCharId(plotElement, "char", path),
                RemainingRounds = RequireInt32(plotElement, "remainingRounds", path),
                Soil = RequireInt32(plotElement, "soil", path),
                Status = RequireEnum<PlotStatus>(plotElement, "status", path),
            };
            index++;
        }

        return plots;
    }

    private static List<GlyphId> RequireGlyphList(JsonElement parent, string name, string path)
    {
        JsonElement listElement = RequireProperty(parent, name, path);
        if (listElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{path}.{name} 必须是数组。");
        }

        var glyphs = new List<GlyphId>(listElement.GetArrayLength());
        int index = 0;
        foreach (JsonElement glyphElement in listElement.EnumerateArray())
        {
            if (glyphElement.ValueKind != JsonValueKind.Number || !glyphElement.TryGetInt32(out int glyphId))
            {
                throw new InvalidDataException($"{path}.{name}[{index}] 必须是整数 glyphId。");
            }

            glyphs.Add(new GlyphId(glyphId));
            index++;
        }

        return glyphs;
    }

    private static TEnum RequireEnum<TEnum>(JsonElement parent, string name, string path)
        where TEnum : struct, Enum
    {
        string value = RequireString(parent, name, path);
        if (!Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{path}.{name} 不是合法的 {typeof(TEnum).Name}：{value}。");
        }

        return Enum.Parse<TEnum>(value);
    }

    private static JsonElement RequireObject(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{path}.{name} 必须是对象。");
        }

        return element;
    }

    private static JsonElement RequireProperty(JsonElement parent, string name, string path)
    {
        if (!parent.TryGetProperty(name, out JsonElement value))
        {
            throw new InvalidDataException($"{path}.{name} 缺失。");
        }

        return value;
    }

    private static int RequireInt32(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out int value))
        {
            throw new InvalidDataException($"{path}.{name} 必须是 32 位整数。");
        }

        return value;
    }

    private static ulong RequireUInt64(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetUInt64(out ulong value))
        {
            throw new InvalidDataException($"{path}.{name} 必须是 64 位无符号整数。");
        }

        return value;
    }

    private static bool RequireBoolean(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidDataException($"{path}.{name} 必须是布尔值。"),
        };
    }

    private static string RequireString(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{path}.{name} 必须是字符串。");
        }

        return element.GetString() ?? string.Empty;
    }

    private static CharId RequireCharId(JsonElement parent, string name, string path)
    {
        JsonElement element = RequireProperty(parent, name, path);
        return element.ValueKind switch
        {
            JsonValueKind.Null => default,
            JsonValueKind.String => new CharId(element.GetString() ?? string.Empty),
            _ => throw new InvalidDataException($"{path}.{name} 必须是字符串或 null。"),
        };
    }

    private static JsonDocument ParseDocument(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("存档 JSON 解析失败。", exception);
        }
    }
}
