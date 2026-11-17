using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

public sealed class CommandContractTests
{
    [Test]
    public void Validate_DoesNotMutateState()
    {
        var state = new FarmState { Money = 10 };
        var command = new AddMoneyCommand(5);

        var rejection = command.Validate(state, NewContext());

        Assert.That(rejection, Is.Null);
        Assert.That(state.Money, Is.EqualTo(10), "Validate 必须无副作用");
    }

    [Test]
    public void Apply_ChangesStateThroughTheCommandOnly()
    {
        var state = new FarmState { Money = 10 };
        var command = new AddMoneyCommand(5);

        command.Apply(state, NewContext());

        Assert.That(state.Money, Is.EqualTo(15));
    }

    [Test]
    public void InvalidCommand_ReturnsRejectionAndLeavesStateUntouched()
    {
        var state = new FarmState { Money = 10 };
        var command = new AddMoneyCommand(-1);

        var rejection = command.Validate(state, NewContext());

        Assert.That(rejection, Is.Not.Null);
        Assert.That(rejection!.Value.Code, Is.EqualTo("invalid_amount"));
        Assert.That(state.Money, Is.EqualTo(10));
    }

    private static StepContext NewContext()
    {
        return new StepContext(new DeterministicRng(1));
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
}
