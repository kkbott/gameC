namespace TianziGe.Sim;

/// <summary>
/// 命令两阶段契约（F2 R2）。
/// Validate 必须无副作用；Apply 只能读写传入的 state 与 ctx。
/// </summary>
public interface ICommand
{
    Rejection? Validate(FarmState state, StepContext context);

    void Apply(FarmState state, StepContext context);
}
