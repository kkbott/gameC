namespace TianziGe.Sim.Tests;

/// <summary>
/// 构造覆盖 schema 各子结构的状态，供序列化 / 克隆 / 回滚测试复用。
/// 这是测试数据，不是新局初始化（新局初始化归 T14 / C2 / G4）。
/// </summary>
internal static class TestStateFactory
{
    public static FarmState CreatePopulatedState()
    {
        var state = new FarmState
        {
            RngState = 0x1234_5678_9ABC_DEF0UL,
            SeasonIndex = 2,
            RoundIndex = 5,
            TotalSteps = 42,
            Money = 87,
            QiOfSui = 9,
        };

        state.Plots[0] = new Plot
        {
            Status = PlotStatus.Char,
            Char = new CharId("qiu1"),
            AppliedRound = 12,
            RemainingRounds = 2,
            Soil = 88,
        };
        state.Plots[1] = new Plot
        {
            Status = PlotStatus.Sui,
            Soil = 40,
        };
        state.Plots[2] = new Plot
        {
            Status = PlotStatus.Empty,
            Soil = 100,
        };
        state.Plots[3] = new Plot
        {
            Status = PlotStatus.Char,
            Char = new CharId("ming2"),
            AppliedRound = 3,
            RemainingRounds = 0,
            Soil = 66,
        };

        state.Hand.Tiles.AddRange(new[] { new GlyphId(1), new GlyphId(4), new GlyphId(9) });
        state.Hand.Capacity = 6;
        state.Hand.DrawPile.AddRange(new[] { new GlyphId(7), new GlyphId(8) });

        state.Village.Count = 4;
        state.Village.Members.Add(new Villager(new EntityId(3)));

        state.Codex.Learned.Add((new GlyphId(1), new GlyphId(4)));
        state.Codex.Learned.Add((new GlyphId(9), new GlyphId(2)));

        state.Season.NightsResolved = 1;
        state.Season.PendingNight = true;

        return state;
    }
}
