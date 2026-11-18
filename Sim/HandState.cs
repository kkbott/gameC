namespace TianziGe.Sim;

/// <summary>
/// 手牌与牌堆状态（C2 R1）。字段归 F3，补牌 / 洗牌规则归 C2。
/// 列表顺序有意义：<see cref="Tiles"/> 的位置固定、不自动排序。
/// </summary>
public sealed class HandState
{
    /// <summary>开局手牌容量（C2 R6）；每季末 +1，上限 8。</summary>
    public const int InitialCapacity = 5;

    public const int MaxCapacity = 8;

    public List<GlyphId> Tiles = new();

    public int Capacity = InitialCapacity;

    /// <summary>季初预生成的牌堆；洗牌结果必须写回这里（C2 R7）。</summary>
    public List<GlyphId> DrawPile = new();

    internal void CopyFrom(HandState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Tiles ??= new List<GlyphId>();
        Tiles.Clear();
        Tiles.AddRange(source.Tiles);
        Capacity = source.Capacity;

        DrawPile ??= new List<GlyphId>();
        DrawPile.Clear();
        DrawPile.AddRange(source.DrawPile);
    }
}
