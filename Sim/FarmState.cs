namespace TianziGe.Sim;

/// <summary>
/// 游戏状态的唯一可变真相（F3）。
/// 这是 T-001 的最小骨架：只放存档版本、RNG 状态与几个基础计数器，
/// 不含任何玩法规则。字段随 GDD 逐项落地，游戏状态只能经 ICommand 改变。
/// </summary>
public sealed class FarmState
{
    public const int CurrentSaveVersion = 1;

    public int SaveVersion = CurrentSaveVersion;

    /// <summary>确定性 RNG 的状态（F2 R4）。</summary>
    public ulong RngState;

    public int SeasonIndex;
    public int RoundIndex;
    public int TotalSteps;

    public int Money;
    public int QiOfSui;
}
