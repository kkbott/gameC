using System.Text.RegularExpressions;
using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

[Category("F3")]
public sealed class FarmStateSerializationTests
{
    [Test]
    public void SerializeDeserialize_RoundTripsToSameHash()
    {
        var original = TestStateFactory.CreatePopulatedState();
        string json = FarmStateSerializer.Serialize(original);

        var copy = FarmStateSerializer.Deserialize(json);

        Assert.Multiple(() =>
        {
            Assert.That(FarmStateSerializer.Serialize(copy), Is.EqualTo(json), "规范化 JSON 必须逐字节稳定");
            Assert.That(FarmStateHash.Compute(copy), Is.EqualTo(FarmStateHash.Compute(original)),
                "Deserialize(Serialize(s)) 后 hash 必须一致（F3 A3）");
            Assert.That(copy.SaveVersion, Is.EqualTo(FarmState.CurrentSaveVersion));
            Assert.That(copy.RngState, Is.EqualTo(original.RngState));
            Assert.That(copy.SeasonIndex, Is.EqualTo(2));
            Assert.That(copy.RoundIndex, Is.EqualTo(5));
            Assert.That(copy.TotalSteps, Is.EqualTo(42));
            Assert.That(copy.Money, Is.EqualTo(87));
            Assert.That(copy.QiOfSui, Is.EqualTo(9));
            Assert.That(copy.Plots[0].Status, Is.EqualTo(PlotStatus.Char));
            Assert.That(copy.Plots[0].Char.Value, Is.EqualTo("qiu1"));
            Assert.That(copy.Plots[0].Soil, Is.EqualTo(88));
            Assert.That(copy.Plots[1].Status, Is.EqualTo(PlotStatus.Sui));
            Assert.That(copy.Plots[2].Status, Is.EqualTo(PlotStatus.Empty));
            Assert.That(copy.Plots[2].Char.Value, Is.Null, "未写字的田 Char 必须保持 default（null）");
            Assert.That(copy.Hand.Capacity, Is.EqualTo(6));
            Assert.That(copy.Hand.Tiles, Is.EqualTo(original.Hand.Tiles));
            Assert.That(copy.Hand.DrawPile, Is.EqualTo(original.Hand.DrawPile));
            Assert.That(copy.Codex.Learned, Is.EquivalentTo(original.Codex.Learned));
            Assert.That(copy.Village.Count, Is.EqualTo(4));
            Assert.That(copy.Village.Members, Is.EqualTo(original.Village.Members));
            Assert.That(copy.Season.NightsResolved, Is.EqualTo(1));
            Assert.That(copy.Season.PendingNight, Is.True);
        });
    }

    [Test]
    public void Serialize_UsesEnumsAsStrings_AndIntegersForHashStableOutput()
    {
        var state = TestStateFactory.CreatePopulatedState();

        string json = FarmStateSerializer.Serialize(state);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"status\":\"Char\""), "枚举必须写字符串名（F4 R1）");
            Assert.That(json, Does.Contain("\"saveVersion\":1"));
            Assert.That(Regex.IsMatch(json, @"[:\[,]-?\d+\.\d"), Is.False,
                "canonical JSON 不得出现浮点字面量（F2 §4.1）");
        });
    }

    [Test]
    public void Serialize_WritesTopLevelKeysInOrdinalOrder()
    {
        string json = FarmStateSerializer.Serialize(new FarmState());
        string[] keys =
        {
            "codex", "hand", "money", "plots", "qiOfSui", "rngState",
            "roundIndex", "saveVersion", "season", "seasonIndex", "totalSteps", "village",
        };

        int previous = -1;
        foreach (string key in keys)
        {
            int index = json.IndexOf($"\"{key}\":", StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThan(previous), $"键 {key} 必须按字典序出现");
            previous = index;
        }
    }

    [Test]
    public void SerializeDeserialize_DefaultState_RoundTripsEmptyCollections()
    {
        var state = new FarmState();

        var copy = FarmStateSerializer.Deserialize(FarmStateSerializer.Serialize(state));

        Assert.Multiple(() =>
        {
            Assert.That(FarmStateHash.Compute(copy), Is.EqualTo(FarmStateHash.Compute(state)));
            Assert.That(copy.Plots, Has.All.Matches<Plot>(plot => plot.Status == PlotStatus.Locked && plot.Char.Value is null));
            Assert.That(copy.Hand.Tiles, Is.Empty);
            Assert.That(copy.Hand.DrawPile, Is.Empty);
            Assert.That(copy.Codex.Learned, Is.Empty);
            Assert.That(copy.Village.Members, Is.Empty);
        });
    }

    [Test]
    public void SerializeDeserialize_PreservesNullAndEmptyCharIds()
    {
        var state = new FarmState();
        state.Plots[0].Char = default;
        state.Plots[1].Char = new CharId(string.Empty);
        state.Plots[2].Char = new CharId("qiu1");

        var copy = FarmStateSerializer.Deserialize(FarmStateSerializer.Serialize(state));

        Assert.Multiple(() =>
        {
            Assert.That(copy.Plots[0].Char.Value, Is.Null, "default CharId 必须原样往返");
            Assert.That(copy.Plots[1].Char.Value, Is.Empty, "空字符串 CharId 不能被静默转成 null");
            Assert.That(copy.Plots[2].Char.Value, Is.EqualTo("qiu1"));
            Assert.That(FarmStateHash.Compute(copy), Is.EqualTo(FarmStateHash.Compute(state)));
        });
    }

    [Test]
    public void Deserialize_NumericEnumString_IsRejected()
    {
        var state = TestStateFactory.CreatePopulatedState();
        string json = FarmStateSerializer.Serialize(state)
            .Replace("\"status\":\"Char\"", "\"status\":\"2\"", StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidDataException>(() => FarmStateSerializer.Deserialize(json));

        Assert.That(exception!.Message, Does.Contain("PlotStatus"));
    }

    [Test]
    public void Serialize_IsIndependentOfIterationOrderForCodexPairs()
    {
        var first = new FarmState();
        first.Codex.Learned.Add((new GlyphId(9), new GlyphId(2)));
        first.Codex.Learned.Add((new GlyphId(1), new GlyphId(4)));

        var second = new FarmState();
        second.Codex.Learned.Add((new GlyphId(1), new GlyphId(4)));
        second.Codex.Learned.Add((new GlyphId(9), new GlyphId(2)));

        Assert.That(FarmStateSerializer.Serialize(first), Is.EqualTo(FarmStateSerializer.Serialize(second)),
            "集合必须按稳定键排序，hash 才不会依赖插入顺序（F3 §4.1）");
        Assert.That(FarmStateHash.Compute(first), Is.EqualTo(FarmStateHash.Compute(second)));
    }

    [Test]
    public void Serialize_RoundTripsMaxUInt64RngState()
    {
        var state = new FarmState { RngState = ulong.MaxValue };

        var copy = FarmStateSerializer.Deserialize(FarmStateSerializer.Serialize(state));

        Assert.That(copy.RngState, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void Hash_ChangesWhenAnyTrackedValueChanges()
    {
        var state = TestStateFactory.CreatePopulatedState();
        ulong baseline = FarmStateHash.Compute(state);

        state.Money++;

        Assert.That(FarmStateHash.Compute(state), Is.Not.EqualTo(baseline));
    }

    [Test]
    public void Deserialize_MissingSaveVersion_IsRejected()
    {
        var exception = Assert.Throws<InvalidDataException>(() => FarmStateSerializer.Deserialize("{}"));

        Assert.That(exception!.Message, Does.Contain("$.saveVersion"));
    }

    [Test]
    public void Deserialize_NewerSaveVersion_IsRejected()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => FarmStateSerializer.Deserialize("""{"saveVersion":2}"""));

        Assert.That(exception!.Message, Does.Contain("F3 E1"));
    }

    [Test]
    public void Deserialize_OlderSaveVersion_IsRejectedUntilMigrationChainExists()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => FarmStateSerializer.Deserialize("""{"saveVersion":0}"""));

        Assert.That(exception!.Message, Does.Contain("F3 E2"));
    }

    [Test]
    public void Deserialize_MalformedJson_IsRejectedAsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() => FarmStateSerializer.Deserialize("{"));
    }
}
