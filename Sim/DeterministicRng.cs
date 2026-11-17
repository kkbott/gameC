namespace TianziGe.Sim;

/// <summary>
/// 唯一随机源（F2 R4）。xorshift64*，状态进存档。
/// 禁止 System.Random / Godot 的 RandomNumberGenerator / 时间戳参与任何规则判定。
/// </summary>
public sealed class DeterministicRng
{
    private const ulong NonZeroFallback = 0x9E3779B97F4A7C15UL;
    private const ulong Multiplier = 2685821657736338717UL;

    private ulong _state;

    public DeterministicRng(ulong seed)
    {
        _state = seed == 0 ? NonZeroFallback : seed;
    }

    /// <summary>当前内部状态；写入 FarmState.RngState。</summary>
    public ulong State => _state;

    public void Restore(ulong state)
    {
        if (state == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "RNG 状态不能为 0（xorshift 的零状态会永远输出零）。");
        }

        _state = state;
    }

    public ulong NextUInt64()
    {
        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * Multiplier;
    }

    /// <summary>半开区间 [minInclusive, maxExclusive) 的整数。</summary>
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive 必须大于 minInclusive。");
        }

        var width = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextUInt64() % width));
    }
}
