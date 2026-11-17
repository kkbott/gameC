using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

public sealed class FarmStateTests
{
    [Test]
    public void DefaultState_StartsAtSaveVersionOne()
    {
        var state = new FarmState();

        Assert.That(state.SaveVersion, Is.EqualTo(FarmState.CurrentSaveVersion));
        Assert.That(state.SaveVersion, Is.EqualTo(1));
    }

    [Test]
    public void DefaultState_HasNoMoneyOrProgress()
    {
        var state = new FarmState();

        Assert.That(state.Money, Is.Zero);
        Assert.That(state.QiOfSui, Is.Zero);
        Assert.That(state.RoundIndex, Is.Zero);
        Assert.That(state.SeasonIndex, Is.Zero);
        Assert.That(state.TotalSteps, Is.Zero);
        Assert.That(state.RngState, Is.Zero);
    }
}
