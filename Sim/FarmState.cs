namespace TianziGe.Sim;

/// <summary>
/// 游戏状态的唯一可变真相（F3）。
/// 纯 C#：不继承任何引擎对象、不引用 Godot、不含静态可变字段。
/// 字段只能经 <see cref="GameSession.Execute(ICommand)"/> 改变（F3 R5 / F2 R1）；
/// 序列化与克隆能力见 <see cref="FarmStateSerializer"/> / <see cref="FarmState.Clone"/>。
/// </summary>
public sealed class FarmState
{
    /// <summary>schema 版本；迁移链归 F4，当前程序只支持 v1（F3 E1/E2）。</summary>
    public const int CurrentSaveVersion = 1;

    /// <summary>田块固定容量：4 列 × 3 行，索引即位置（C1 R1）。</summary>
    public const int PlotCapacity = 12;

    public int SaveVersion = CurrentSaveVersion;

    /// <summary>确定性 RNG 的状态（F2 R4）；随存档走。</summary>
    public ulong RngState;

    /// <summary>季相：0 春 / 1 夏 / 2 秋 / 3 冬（G4 R1）。</summary>
    public int SeasonIndex;

    /// <summary>本季当前回合序号；推进规则归 G4。</summary>
    public int RoundIndex;

    /// <summary>已成功执行的逻辑步数；由 Execute 维护，命令不得自行修改。</summary>
    public int TotalSteps;

    /// <summary>铜钱（C5 R1）。</summary>
    public int Money;

    /// <summary>祟气 0..Qmax（C6 R1）；算法归 C6，F3 只存值。</summary>
    public int QiOfSui;

    /// <summary>固定 12 块田，未开垦的 status = Locked（C1 R1/R2）。</summary>
    public Plot[] Plots = new Plot[PlotCapacity];

    /// <summary>手牌与牌堆（C2 R1）；规则归 C2。</summary>
    public HandState Hand = new();

    /// <summary>村民（G2）；MVP 阶段 Members 恒为空，规则只信 Count（F3 §6.3）。</summary>
    public VillageState Village = new();

    /// <summary>字谱（G1）；以无序部件对为键（C3 硬契约）。</summary>
    public CodexState Codex = new();

    /// <summary>季相与祟夜计数。</summary>
    public SeasonState Season = new();

    /// <summary>
    /// 深拷贝：克隆出的状态与原件不共享任何可变容器，可安全交给 M4 / 存档预演使用（F3 A4）。
    /// </summary>
    public FarmState Clone()
    {
        var clone = new FarmState();
        clone.CopyFrom(this);
        return clone;
    }

    /// <summary>
    /// 用另一份状态整体覆盖本实例（深拷贝）。仅供 F2 的 Step 回滚内部使用：
    /// 保持 FarmState 实例身份不变，避免持有者手里的引用失效。
    /// </summary>
    internal void CopyFrom(FarmState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ReferenceEquals(this, source))
        {
            return;
        }

        SaveVersion = source.SaveVersion;
        RngState = source.RngState;
        SeasonIndex = source.SeasonIndex;
        RoundIndex = source.RoundIndex;
        TotalSteps = source.TotalSteps;
        Money = source.Money;
        QiOfSui = source.QiOfSui;

        Plot[] sourcePlots = source.Plots ?? new Plot[PlotCapacity];
        if (Plots is null || Plots.Length != sourcePlots.Length)
        {
            Plots = new Plot[sourcePlots.Length];
        }

        Array.Copy(sourcePlots, Plots, sourcePlots.Length);

        // 回滚可能面对“命令把子结构置 null 后抛异常”的坏状态；
        // 这里先重建目标容器，保证回滚本身不会用 NullReferenceException 掩盖原始失败。
        Hand ??= new HandState();
        Hand.CopyFrom(source.Hand ?? new HandState());

        Village ??= new VillageState();
        Village.CopyFrom(source.Village ?? new VillageState());

        Codex ??= new CodexState();
        Codex.CopyFrom(source.Codex ?? new CodexState());

        Season ??= new SeasonState();
        Season.CopyFrom(source.Season ?? new SeasonState());
    }
}
