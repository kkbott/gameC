namespace TianziGe.Sim;

/// <summary>
/// 季相与祟夜状态（F3 schema 的 <c>SeasonState</c> 部分）。
/// 季相编号住在 <see cref="FarmState.SeasonIndex"/>；这里只放需要随存档走的夜次状态。
/// F3 只写了“季相与夜次”，未列字段；本类型是 T-005 依据 C6 R3/R7 与 G4 季末展示补的最小字段集。
/// </summary>
public sealed class SeasonState
{
    /// <summary>本季已结算的祟夜次数（C7 / G4 季末展示用）。</summary>
    public int NightsResolved;

    /// <summary>
    /// C6 在回合结算第 3 步置位、C7 在第 5 步消费的待结算标志（C6 R3/R7）。
    /// 祟夜是单个原子 Step，标志随存档走（C6 A8）。
    /// </summary>
    public bool PendingNight;

    internal void CopyFrom(SeasonState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        NightsResolved = source.NightsResolved;
        PendingNight = source.PendingNight;
    }
}
