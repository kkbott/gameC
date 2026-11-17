using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

public sealed class DeterministicRngTests
{
    [Test]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new DeterministicRng(20261117);
        var b = new DeterministicRng(20261117);

        for (var i = 0; i < 64; i++)
        {
            Assert.That(a.NextUInt64(), Is.EqualTo(b.NextUInt64()), $"第 {i} 个数不一致");
        }
    }

    [Test]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new DeterministicRng(1);
        var b = new DeterministicRng(2);

        var differs = false;
        for (var i = 0; i < 8; i++)
        {
            differs |= a.NextUInt64() != b.NextUInt64();
        }

        Assert.That(differs, Is.True);
    }

    [Test]
    public void Range_StaysInsideRequestedBounds()
    {
        var rng = new DeterministicRng(7);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Range(-3, 5);
            Assert.That(value, Is.InRange(-3, 4));
        }
    }

    [Test]
    public void Restore_RestoresTheExactSequence()
    {
        var rng = new DeterministicRng(99);
        _ = rng.NextUInt64();

        var saved = rng.State;
        var expected = rng.NextUInt64();

        rng.Restore(saved);
        Assert.That(rng.NextUInt64(), Is.EqualTo(expected));
    }

    [Test]
    public void Restore_RejectsTheZeroState()
    {
        var rng = new DeterministicRng(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Restore(0));
    }

    [Test]
    public void Range_RejectsEmptyInterval()
    {
        var rng = new DeterministicRng(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Range(3, 3));
    }
}
