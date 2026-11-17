namespace TianziGe.Sim;

/// <summary>
/// 命令被拒绝的原因。Validate 返回 null 表示可以执行。
/// </summary>
public readonly record struct Rejection(string Code, string Message);

/// <summary>
/// 一次逻辑步的上下文。只携带与规则相关、可复现的输入；不携带任何引擎对象。
/// </summary>
public sealed class StepContext
{
    public StepContext(DeterministicRng rng)
    {
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public DeterministicRng Rng { get; }
}
