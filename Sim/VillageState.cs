namespace TianziGe.Sim;

/// <summary>
/// 村民条目占位（G2 落地前不启用）。MVP 阶段 <see cref="VillageState.Members"/> 恒为空，
/// 只信 <see cref="VillageState.Count"/>；具体成员字段等 G2 设计批准后再扩展。
/// </summary>
public sealed record Villager(EntityId Id);

/// <summary>
/// 村民状态（F3 §6.3）：MVP 初值 5、只减不增、下限 2。
/// 字段归 F3；去留规则归 G2/C7。
/// </summary>
public sealed class VillageState
{
    public const int InitialCount = 5;

    public const int MinCount = 2;

    public int Count = InitialCount;

    /// <summary>G2 落地前恒为空；禁止与 Count 双写（F3 E5）。</summary>
    public List<Villager> Members = new();

    internal void CopyFrom(VillageState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Count = source.Count;
        Members ??= new List<Villager>();
        Members.Clear();
        Members.AddRange(source.Members);
    }
}
