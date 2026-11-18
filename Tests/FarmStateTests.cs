using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

[Category("F3")]
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

        Assert.Multiple(() =>
        {
            Assert.That(state.Money, Is.Zero);
            Assert.That(state.QiOfSui, Is.Zero);
            Assert.That(state.RoundIndex, Is.Zero);
            Assert.That(state.SeasonIndex, Is.Zero);
            Assert.That(state.TotalSteps, Is.Zero);
            Assert.That(state.RngState, Is.Zero);
        });
    }

    [Test]
    public void DefaultState_HasCompleteSchemaV1()
    {
        var state = new FarmState();

        Assert.Multiple(() =>
        {
            Assert.That(state.Plots, Has.Length.EqualTo(FarmState.PlotCapacity));
            Assert.That(state.Plots, Has.All.Matches<Plot>(plot => plot.Status == PlotStatus.Locked),
                "未初始化的田必须落在 Locked（枚举 0）");
            Assert.That(state.Hand, Is.Not.Null);
            Assert.That(state.Hand.Tiles, Is.Empty);
            Assert.That(state.Hand.Capacity, Is.EqualTo(HandState.InitialCapacity));
            Assert.That(state.Hand.DrawPile, Is.Empty);
            Assert.That(state.Village, Is.Not.Null);
            Assert.That(state.Village.Count, Is.EqualTo(VillageState.InitialCount));
            Assert.That(state.Village.Members, Is.Empty);
            Assert.That(state.Codex, Is.Not.Null);
            Assert.That(state.Codex.Learned, Is.Empty);
            Assert.That(state.Season, Is.Not.Null);
            Assert.That(state.Season.NightsResolved, Is.Zero);
            Assert.That(state.Season.PendingNight, Is.False);
        });
    }
}
