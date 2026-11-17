using TianziGe.Sim.Data;

namespace TianziGe.Sim.Tests;

/// <summary>
/// T-003 数据管线：Sim 不启动 Godot 也能解析仓库里的真实 JSON（F1-A1）。
/// </summary>
[Category("DataPipeline")]
public sealed class DataPipelineTests
{
    [Test]
    public void RealDataFiles_ParseInto50CharsAnd26Components()
    {
        string root = FindRepositoryRoot();
        string charTableJson = File.ReadAllText(Path.Combine(root, "Client", "Data", "char-table.json"));
        string componentJson = File.ReadAllText(Path.Combine(root, "Client", "Data", "component-registry.json"));

        CharTable table = CharTableLoader.Load(charTableJson, componentJson);

        Assert.Multiple(() =>
        {
            Assert.That(table.Chars, Has.Count.EqualTo(50), "字表条目数必须与 _meta.counts.total 一致");
            Assert.That(table.Components, Has.Count.EqualTo(26), "部件数必须与 _meta 声明一致");
            Assert.That(table.Chars.Select(entry => entry.Char).Distinct().Count(), Is.EqualTo(50),
                "50 个字的 char 不应重复");
            Assert.That(table.Chars.All(entry => entry.Components.Count == 2), Is.True,
                "MVP 字表每个字都由两个部件组成");
        });
    }

    [Test]
    public void LoadChars_MissingRequiredField_ThrowsWithJsonPath()
    {
        const string json = """
            { "chars": [ { "id": "qiu1", "char": "秋", "components": ["禾", "火"], "category": "produce" } ] }
            """;

        InvalidDataException? exception = Assert.Throws<InvalidDataException>(() => CharTableLoader.LoadChars(json));

        Assert.That(exception!.Message, Does.Contain("$.chars[0].template"));
    }

    [Test]
    public void LoadComponents_MissingTargets_ThrowsWithJsonPath()
    {
        const string json = """
            { "components": [ { "glyphId": 1, "glyph": "田", "rarity": 3, "family": "soil" } ] }
            """;

        InvalidDataException? exception = Assert.Throws<InvalidDataException>(() => CharTableLoader.LoadComponents(json));

        Assert.That(exception!.Message, Does.Contain("$.components[0].targets"));
    }

    [Test]
    public void LoadChars_InvalidJson_ThrowsInvalidDataException()
    {
        const string json = "{ \"chars\": [ }";

        Assert.Throws<InvalidDataException>(() => CharTableLoader.LoadChars(json));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TianziGe.Tools.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("找不到仓库根（TianziGe.Tools.sln）。");
    }
}
