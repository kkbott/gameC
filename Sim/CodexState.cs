namespace TianziGe.Sim;

/// <summary>
/// 字谱状态（G1）。以**无序部件对**为键，而不是以字符 ID 为键——
/// 这是 C3 免检逻辑的硬契约：必须在 <c>Lookup</c> 之前就能判断是否收录。
/// 调用方（G1/C3）负责按 glyphId 规范化部件对；canonical 序列化会按稳定顺序写出集合。
/// </summary>
public sealed class CodexState
{
    public HashSet<(GlyphId A, GlyphId B)> Learned = new();

    internal void CopyFrom(CodexState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Learned ??= new HashSet<(GlyphId A, GlyphId B)>();
        Learned.Clear();
        Learned.UnionWith(source.Learned);
    }
}
