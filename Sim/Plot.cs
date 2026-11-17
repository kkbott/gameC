namespace TianziGe.Sim;

/// <summary>
/// 田块状态（C1 R2）。枚举值固定，<c>Locked = 0</c> 保证未初始化的
/// <see cref="Plot"/> 数组元素不会意外变成可写状态。
/// </summary>
public enum PlotStatus
{
    Locked = 0,
    Empty,
    Char,
    Sui,
}

/// <summary>
/// 一块田 = 一个田字格（C1）。字段归 F3；状态迁移规则归 C1–C7。
/// 索引即位置：<c>index = row * 4 + col</c>，邻接由索引计算（C1 R1/R4）。
/// </summary>
public struct Plot
{
    public PlotStatus Status;

    /// <summary>仅 <see cref="Status"/> == <see cref="PlotStatus.Char"/> 时有意义。</summary>
    public CharId Char;

    /// <summary>该字生效的回合；规则归 C4/C1。</summary>
    public int AppliedRound;

    /// <summary>duration = rounds:N 的剩余回合；规则归 C4/C1。</summary>
    public int RemainingRounds;

    /// <summary>地力 0..100；字段归 F3，变化规则归 C5。</summary>
    public int Soil;
}
