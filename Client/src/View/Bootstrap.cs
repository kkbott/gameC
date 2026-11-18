using Godot;
using TianziGe.Sim;
using TianziGe.View.FontTest;

namespace TianziGe.View;

/// <summary>
/// T-001 的启动探针：证明 Godot View 程序集能引用 TianziGe.Sim，
/// 并且规则层可以在不涉及场景 / 渲染状态的情况下被驱动。
/// 真正的游戏入口在后续任务里替换。
/// </summary>
public partial class Bootstrap : Node
{
    public override void _Ready()
    {
        string[] userArgs = OS.GetCmdlineUserArgs();
        if (System.Array.IndexOf(userArgs, "--steam-probe") >= 0)
        {
            GetTree().Quit(SteamTest.SteamProbe.Run());
            return;
        }

        if (System.Array.IndexOf(userArgs, "--data-probe") >= 0)
        {
            GetTree().Quit(DataProbe.Run());
            return;
        }

        if (System.Array.IndexOf(userArgs, "--font-probe") >= 0)
        {
            GetNode<Label>("BootLabel").Hide();
            var probe = new FontProbe();
            AddChild(probe);
            probe.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return;
        }

        var state = new FarmState();
        var session = new GameSession(state);
        var command = new BootProbeCommand();

        StepResult result = session.Execute(command);
        if (!result.Applied)
        {
            string detail = result.Rejected
                ? result.Rejection!.Value.Code
                : result.Failure?.Message ?? result.Status.ToString();
            GD.PushError($"Boot probe failed ({result.Status}): {detail}");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"TianziGe boot OK: sim={typeof(FarmState).Assembly.GetName().Name} money={state.Money}");

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--boot-probe") >= 0)
        {
            GetTree().Quit(0);
        }
    }

    private sealed class BootProbeCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context) => null;

        public void Apply(FarmState state, StepContext context)
        {
            state.Money += 3;
        }
    }
}
