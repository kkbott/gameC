namespace TianziGe.Sim;

/// <summary>
/// 命令两阶段契约（F2 R2）。
/// Validate 必须无副作用；Apply 只能读写传入的 state 与 ctx。
/// 命令不得修改 <see cref="FarmState.SaveVersion"/>；
/// <see cref="FarmState.RngState"/> 与 <see cref="FarmState.TotalSteps"/> 由
/// <see cref="GameSession.Execute"/> 维护，命令不得自行修改。
/// </summary>
public interface ICommand
{
    Rejection? Validate(FarmState state, StepContext context);

    void Apply(FarmState state, StepContext context);
}
