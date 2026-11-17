namespace TianziGe.Sim;

/// <summary>
/// 一局游戏的运行期容器：持有唯一的 <see cref="FarmState"/> 与唯一 RNG，
/// 所有状态变更都必须经过 <see cref="Execute"/>（F3 R1/R5、F2 R1）。
///
/// 刻意不是单例：由调用方（View / M4 / 测试）显式构造并通过参数传递，
/// 状态与演员 ID 都不存在隐式的“当前”概念（F3 R3）。
/// </summary>
public sealed class GameSession
{
    private readonly FarmState _state;
    private readonly DeterministicRng _rng;
    private readonly StepContext _context;
    private readonly bool _assertOnApplyFailure;

    /// <param name="state">一局的状态；同一会话内保持实例身份不变（回滚是原地覆盖）。</param>
    /// <param name="assertOnApplyFailure">
    /// 开发期断言开关（F2 R3）：为 true 时 Apply 抛异常在回滚后重新抛出，
    /// 让测试 / 编辑器立刻发现命令写坏了；发行路径保持 false，回滚并跳过。
    /// 传 null（默认）时按编译配置决定：Debug 断言，Release 回滚并跳过。
    /// </param>
    public GameSession(FarmState state, bool? assertOnApplyFailure = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _rng = new DeterministicRng(state.RngState);
        _context = new StepContext(_rng);
#if DEBUG
        _assertOnApplyFailure = assertOnApplyFailure ?? true;
#else
        _assertOnApplyFailure = assertOnApplyFailure ?? false;
#endif
    }

    /// <summary>本会话持有的唯一状态。只读使用；写入必须走 <see cref="Execute"/>。</summary>
    public FarmState State => _state;

    /// <summary>
    /// 执行一条命令：Validate → （通过则）原子 Apply（F2 R2/R3）。
    ///
    /// 书签字段（<see cref="FarmState.RngState"/>、<see cref="FarmState.TotalSteps"/>）
    /// 由本方法维护；命令不得自行修改。RNG 在 Apply 成功后写回状态，
    /// 使“状态快照 + 种子”可复现整局（F2 R6）。
    /// </summary>
    public StepResult Execute(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 以状态里的 RNG 状态为准：读档 / M4 重放时可能由调用方直接写入了种子。
        // 0 是“尚未初始化”哨兵：保持会话 RNG 当前状态，首次成功后会把状态写回。
        if (_state.RngState != 0)
        {
            _rng.Restore(_state.RngState);
        }

        // 先快照：Validate 契约上无副作用，但引擎不能把“命令写坏了”变成状态污染。
        // 被拒绝时用快照恢复，成功时快照留给 Apply 失败回滚。
        FarmState snapshot = _state.Clone();
        ulong rngBeforeValidate = _rng.State;
        Rejection? rejection = command.Validate(_state, _context);
        if (rejection is not null)
        {
            _state.CopyFrom(snapshot);
            _rng.Restore(rngBeforeValidate);
            return new StepResult(StepStatus.Rejected, _state.TotalSteps, rejection, null);
        }

        try
        {
            command.Apply(_state, _context);
        }
        catch (Exception failure)
        {
            // Step 原子性：整体回滚到 Apply 前（F2 R3 / E2）。
            _state.CopyFrom(snapshot);
            _rng.Restore(rngBeforeValidate);

            if (_assertOnApplyFailure)
            {
                throw new InvalidOperationException(
                    $"命令 {command.GetType().Name} 在 Apply 阶段抛出异常，状态已回滚到 Step 前。",
                    failure);
            }

            return new StepResult(StepStatus.RolledBack, _state.TotalSteps, null, failure);
        }

        _state.RngState = _rng.State;
        _state.TotalSteps++;

        return new StepResult(StepStatus.Applied, _state.TotalSteps, null, null);
    }
}
