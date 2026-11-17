using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

[Category("F2")]
public sealed class CommandExecutionTests
{
    [Test]
    public void Execute_AppliesCommand_AndAdvancesStepCounter()
    {
        var state = new FarmState();
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new AddMoneyCommand(7));

        Assert.Multiple(() =>
        {
            Assert.That(result.Applied, Is.True);
            Assert.That(result.StepNumber, Is.EqualTo(1));
            Assert.That(state.Money, Is.EqualTo(7));
            Assert.That(state.TotalSteps, Is.EqualTo(1));
            Assert.That(state.RngState, Is.Not.Zero, "成功后 RNG 状态必须写回 FarmState（F2 R4）");
        });
    }

    [Test]
    public void Execute_RejectedCommand_LeavesStateHashUnchanged()
    {
        var state = new FarmState { Money = 10 };
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new AddMoneyCommand(-1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Rejected, Is.True);
            Assert.That(result.Rejection, Is.Not.Null);
            Assert.That(result.Rejection!.Value.Code, Is.EqualTo("invalid_amount"));
            Assert.That(result.StepNumber, Is.EqualTo(0));
            Assert.That(state.Money, Is.EqualTo(10));
            Assert.That(state.TotalSteps, Is.Zero);
            Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before),
                "校验失败的命令不得改变状态（F2 A5）");
        });
    }

    [Test]
    public void Execute_RejectedCommand_DoesNotLeakRngConsumption()
    {
        var state = new FarmState { RngState = 99 };
        var referenceState = state.Clone();
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new RejectAfterDrawingRngCommand());

        Assert.That(result.Rejected, Is.True);
        Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before),
            "Validate 被拒绝时，状态必须零变化");

        Assert.That(session.Execute(new DrawThenSetMoneyCommand()).Applied, Is.True);
        var referenceSession = new GameSession(referenceState, assertOnApplyFailure: false);
        Assert.That(referenceSession.Execute(new DrawThenSetMoneyCommand()).Applied, Is.True);

        Assert.That(state.Money, Is.EqualTo(referenceState.Money),
            "被拒绝的 Validate 不得消耗 RNG 流（否则下一次随机结果会偏移）");
    }

    [Test]
    public void Execute_RejectedCommand_WithImpureValidate_StillRestoresState()
    {
        var state = new FarmState { Money = 10 };
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new ImpureRejectCommand());

        Assert.Multiple(() =>
        {
            Assert.That(result.Rejected, Is.True);
            Assert.That(state.Money, Is.EqualTo(10), "Validate 违约写入也必须被拒绝路径回滚");
            Assert.That(state.Hand.Tiles, Is.Empty);
            Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before));
        });
    }

    [Test]
    public void Execute_ApplyFailure_RollsBackWholeState()
    {
        var state = TestStateFactory.CreatePopulatedState();
        int stepsBefore = state.TotalSteps;
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new MutateThenThrowCommand());

        Assert.Multiple(() =>
        {
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.Failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(result.StepNumber, Is.EqualTo(stepsBefore));
            Assert.That(state.TotalSteps, Is.EqualTo(stepsBefore));
            Assert.That(state.Money, Is.EqualTo(87));
            Assert.That(state.Plots[0].Status, Is.EqualTo(PlotStatus.Char));
            Assert.That(state.Hand.Tiles, Has.Count.EqualTo(3));
            Assert.That(state.Codex.Learned, Has.Count.EqualTo(2));
            Assert.That(state.Season.PendingNight, Is.True);
            Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before),
                "Apply 抛异常后状态必须回到 Step 前（F2 A6 / R3）");
        });
    }

    [Test]
    public void Execute_ApplyFailure_DoesNotLeakRngConsumption()
    {
        var state = new FarmState { RngState = 99 };
        var referenceState = state.Clone();
        var session = new GameSession(state, assertOnApplyFailure: false);

        Assert.That(session.Execute(new DrawThenMutateThenThrowCommand()).RolledBack, Is.True);

        Assert.That(session.Execute(new DrawThenSetMoneyCommand()).Applied, Is.True);
        var referenceSession = new GameSession(referenceState, assertOnApplyFailure: false);
        Assert.That(referenceSession.Execute(new DrawThenSetMoneyCommand()).Applied, Is.True);

        Assert.That(state.Money, Is.EqualTo(referenceState.Money),
            "Apply 抛异常回滚后，RNG 流必须回到 Step 前");
    }

    [Test]
    public void Execute_ApplyFailure_WithNulledContainers_StillRollsBack()
    {
        var state = TestStateFactory.CreatePopulatedState();
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: false);

        StepResult result = session.Execute(new NullContainersThenThrowCommand());

        Assert.Multiple(() =>
        {
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.Failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(state.Hand, Is.Not.Null);
            Assert.That(state.Village, Is.Not.Null);
            Assert.That(state.Codex, Is.Not.Null);
            Assert.That(state.Season, Is.Not.Null);
            Assert.That(state.Plots, Has.Length.EqualTo(FarmState.PlotCapacity));
            Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before),
                "命令把子结构置 null 后抛异常，回滚也不能失败（F2 R3）");
        });
    }

    [Test]
    public void Execute_ApplyFailure_InDevAssertMode_RethrowsAfterRollback()
    {
        var state = TestStateFactory.CreatePopulatedState();
        ulong before = FarmStateHash.Compute(state);
        var session = new GameSession(state, assertOnApplyFailure: true);

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.Execute(new MutateThenThrowCommand()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before), "重抛前必须已经回滚");
        });
    }

    [Test]
    public void Validate_CalledRepeatedly_DoesNotChangeState()
    {
        var state = TestStateFactory.CreatePopulatedState();
        ulong before = FarmStateHash.Compute(state);
        var command = new AddMoneyCommand(5);
        var context = new StepContext(new DeterministicRng(state.RngState));

        for (var i = 0; i < 1000; i++)
        {
            Assert.That(command.Validate(state, context), Is.Null);
        }

        Assert.That(FarmStateHash.Compute(state), Is.EqualTo(before), "Validate 必须无副作用（F2 A4）");
    }

    [Test]
    public void SameSeedAndCommandSequence_ProduceSameStateHash()
    {
        ulong first = RunDeterministicSequence();
        ulong second = RunDeterministicSequence();

        Assert.That(second, Is.EqualTo(first), "同状态 + 同种子 + 同命令序列必须复现（F2 R6）");
    }

    private static ulong RunDeterministicSequence()
    {
        var state = new FarmState { RngState = 7 };
        var session = new GameSession(state, assertOnApplyFailure: false);

        Assert.That(session.Execute(new AddMoneyCommand(3)).Applied, Is.True);
        Assert.That(session.Execute(new DrawThenSetMoneyCommand()).Applied, Is.True);
        Assert.That(session.Execute(new AddMoneyCommand(4)).Applied, Is.True);

        return FarmStateHash.Compute(state);
    }

    private sealed class AddMoneyCommand : ICommand
    {
        private readonly int _amount;

        public AddMoneyCommand(int amount) => _amount = amount;

        public Rejection? Validate(FarmState state, StepContext context)
        {
            return _amount > 0 ? null : new Rejection("invalid_amount", "金额必须为正。");
        }

        public void Apply(FarmState state, StepContext context)
        {
            state.Money += _amount;
        }
    }

    private sealed class DrawThenSetMoneyCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context) => null;

        public void Apply(FarmState state, StepContext context)
        {
            state.Money = context.Rng.Range(0, 1000);
        }
    }

    private sealed class RejectAfterDrawingRngCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context)
        {
            _ = context.Rng.NextUInt64();
            return new Rejection("test_rejection", "故意拒绝。");
        }

        public void Apply(FarmState state, StepContext context)
        {
            throw new InvalidOperationException("被拒绝的命令不应执行 Apply。");
        }
    }

    private sealed class ImpureRejectCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context)
        {
            // 故意违反“Validate 无副作用”契约，验证拒绝路径仍会恢复快照。
            state.Money = 999;
            state.Hand.Tiles.Add(new GlyphId(42));
            return new Rejection("impure_rejection", "故意在 Validate 里写状态。");
        }

        public void Apply(FarmState state, StepContext context)
        {
            throw new InvalidOperationException("被拒绝的命令不应执行 Apply。");
        }
    }

    private sealed class DrawThenMutateThenThrowCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context) => null;

        public void Apply(FarmState state, StepContext context)
        {
            _ = context.Rng.NextUInt64();
            state.Money = 1234;
            throw new InvalidOperationException("消耗随机数后抛异常。");
        }
    }

    private sealed class NullContainersThenThrowCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context) => null;

        public void Apply(FarmState state, StepContext context)
        {
            state.Hand = null!;
            state.Village = null!;
            state.Codex = null!;
            state.Season = null!;
            state.Plots = null!;

            throw new InvalidOperationException("把所有子结构置 null 后抛异常。");
        }
    }

    private sealed class MutateThenThrowCommand : ICommand
    {
        public Rejection? Validate(FarmState state, StepContext context) => null;

        public void Apply(FarmState state, StepContext context)
        {
            state.Money = 1234;
            state.QiOfSui = 0;
            state.Plots[0].Status = PlotStatus.Sui;
            state.Hand.Tiles.Add(new GlyphId(99));
            state.Codex.Learned.Add((new GlyphId(5), new GlyphId(6)));
            state.Season.PendingNight = false;

            throw new InvalidOperationException("模拟命令写坏一半后抛异常。");
        }
    }
}
