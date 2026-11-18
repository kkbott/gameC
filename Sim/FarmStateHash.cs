using System.Text;

namespace TianziGe.Sim;

/// <summary>
/// 状态哈希（F3 §4.1 / F2 §4.2）：对规范化 JSON 的 UTF-8 字节做 FNV-1a 64。
/// 这是 M4 回归与将来联机校验的共同地基（F2 R6）；命令日志回放归 T6。
/// </summary>
public static class FarmStateHash
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    /// <summary>同一状态必须永远得到同一个 64 位 hash；不同字段变化应改变 hash。</summary>
    public static ulong Compute(FarmState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        byte[] bytes = Encoding.UTF8.GetBytes(FarmStateSerializer.Serialize(state));
        ulong hash = FnvOffsetBasis;

        foreach (byte value in bytes)
        {
            hash ^= value;
            hash *= FnvPrime;
        }

        return hash;
    }
}
