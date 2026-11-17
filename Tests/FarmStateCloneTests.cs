using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

[Category("F3")]
public sealed class FarmStateCloneTests
{
    [Test]
    public void Clone_CopiesEveryField_WithoutSharingMutableContainers()
    {
        var original = TestStateFactory.CreatePopulatedState();
        ulong originalHash = FarmStateHash.Compute(original);

        var clone = original.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(FarmStateHash.Compute(clone), Is.EqualTo(originalHash), "Clone 后 hash 必须与原状态一致（F3 A4）");
            Assert.That(clone.SaveVersion, Is.EqualTo(original.SaveVersion));
            Assert.That(clone.RngState, Is.EqualTo(original.RngState));
            Assert.That(clone.SeasonIndex, Is.EqualTo(original.SeasonIndex));
            Assert.That(clone.RoundIndex, Is.EqualTo(original.RoundIndex));
            Assert.That(clone.TotalSteps, Is.EqualTo(original.TotalSteps));
            Assert.That(clone.Money, Is.EqualTo(original.Money));
            Assert.That(clone.QiOfSui, Is.EqualTo(original.QiOfSui));
            Assert.That(clone.Plots, Is.Not.SameAs(original.Plots), "Plots 数组必须深拷贝");
            Assert.That(clone.Hand.Tiles, Is.Not.SameAs(original.Hand.Tiles), "手牌列表必须深拷贝");
            Assert.That(clone.Hand.DrawPile, Is.Not.SameAs(original.Hand.DrawPile), "牌堆列表必须深拷贝");
            Assert.That(clone.Village.Members, Is.Not.SameAs(original.Village.Members), "村民列表必须深拷贝");
            Assert.That(clone.Codex.Learned, Is.Not.SameAs(original.Codex.Learned), "字谱集合必须深拷贝");
            Assert.That(clone.Season, Is.Not.SameAs(original.Season), "Season 必须深拷贝");
        });
    }

    [Test]
    public void MutatingClone_DoesNotChangeOriginal()
    {
        var original = TestStateFactory.CreatePopulatedState();
        ulong originalHash = FarmStateHash.Compute(original);
        var clone = original.Clone();

        clone.Money = 999;
        clone.QiOfSui = 0;
        clone.RngState = 7;
        clone.SeasonIndex = 0;
        clone.RoundIndex = 0;
        clone.TotalSteps = 0;
        clone.Plots[0] = new Plot { Status = PlotStatus.Sui, Soil = 1 };
        clone.Plots[2].Soil = 50;
        clone.Hand.Tiles.Add(new GlyphId(99));
        clone.Hand.DrawPile.Clear();
        clone.Hand.Capacity = 8;
        clone.Codex.Learned.Add((new GlyphId(42), new GlyphId(43)));
        clone.Village.Count = 2;
        clone.Village.Members.Add(new Villager(new EntityId(9)));
        clone.Season.NightsResolved = 3;
        clone.Season.PendingNight = false;

        Assert.Multiple(() =>
        {
            Assert.That(FarmStateHash.Compute(original), Is.EqualTo(originalHash), "修改克隆后原状态 hash 必须不变（F3 A4）");
            Assert.That(original.Money, Is.EqualTo(87));
            Assert.That(original.RngState, Is.EqualTo(0x1234_5678_9ABC_DEF0UL));
            Assert.That(original.Plots[0].Status, Is.EqualTo(PlotStatus.Char));
            Assert.That(original.Plots[0].Char.Value, Is.EqualTo("qiu1"));
            Assert.That(original.Plots[2].Soil, Is.EqualTo(100));
            Assert.That(original.Hand.Tiles, Has.Count.EqualTo(3));
            Assert.That(original.Hand.DrawPile, Has.Count.EqualTo(2));
            Assert.That(original.Hand.Capacity, Is.EqualTo(6));
            Assert.That(original.Codex.Learned, Has.Count.EqualTo(2));
            Assert.That(original.Village.Count, Is.EqualTo(4));
            Assert.That(original.Village.Members, Has.Count.EqualTo(1));
            Assert.That(original.Season.NightsResolved, Is.EqualTo(1));
            Assert.That(original.Season.PendingNight, Is.True);
        });
    }

    [Test]
    public void MutatingOriginal_DoesNotChangeClone()
    {
        var original = TestStateFactory.CreatePopulatedState();
        var clone = original.Clone();
        ulong cloneHash = FarmStateHash.Compute(clone);

        original.Money = 1;
        original.Plots[0].Status = PlotStatus.Locked;
        original.Hand.Tiles.Clear();
        original.Codex.Learned.Clear();
        original.Season.PendingNight = false;

        Assert.Multiple(() =>
        {
            Assert.That(FarmStateHash.Compute(clone), Is.EqualTo(cloneHash), "修改原状态后克隆 hash 必须不变");
            Assert.That(clone.Money, Is.EqualTo(87));
            Assert.That(clone.Plots[0].Status, Is.EqualTo(PlotStatus.Char));
            Assert.That(clone.Hand.Tiles, Has.Count.EqualTo(3));
            Assert.That(clone.Codex.Learned, Has.Count.EqualTo(2));
            Assert.That(clone.Season.PendingNight, Is.True);
        });
    }
}
