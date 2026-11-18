namespace TianziGe.Sim;

/// <summary>一次 <see cref="GameSession.Execute"/> 的结果类别。</summary>
public enum StepStatus
{
    /// <summary>Validate 拒绝；状态零变化（F2 R2）。</summary>
    Rejected = 0,

    /// <summary>Apply 成功，状态已推进。</summary>
    Applied,

    /// <summary>Apply 抛异常；状态已回滚到 Step 前（F2 R3 / E2）。</summary>
    RolledBack,
}

/// <summary>
/// 一次逻辑步的结果（F2 的 <c>StepResult</c>）。
/// <see cref="StepNumber"/> 是执行后的累计步数：成功时等于新的 <see cref="FarmState.TotalSteps"/>，
/// 被拒绝或回滚时等于当前未变的 <see cref="FarmState.TotalSteps"/>。
/// </summary>
public readonly record struct StepResult(
    StepStatus Status,
    int StepNumber,
    Rejection? Rejection,
    Exception? Failure)
{
    public bool Applied => Status == StepStatus.Applied;

    public bool Rejected => Status == StepStatus.Rejected;

    public bool RolledBack => Status == StepStatus.RolledBack;
}
