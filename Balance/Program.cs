using TianziGe.Sim;

namespace TianziGe.Balance;

/// <summary>
/// M4 平衡工具的 T-001 骨架：验证 Sim 可以在不启动 Godot 的情况下被驱动。
/// 真正的策略、字表读取与报告在后续任务里实现；本程序只跑一条最小命令序列并输出确定性 hash。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (!TryParsePositive(args, "--runs", 20, out var runs))
        {
            Console.Error.WriteLine("用法: dotnet run --project Balance -- [--runs N] [--seed-base N]");
            return 2;
        }

        if (!TryParseNonNegative(args, "--seed-base", 0, out var seedBase))
        {
            Console.Error.WriteLine("用法: dotnet run --project Balance -- [--runs N] [--seed-base N]");
            return 2;
        }

        var finalHash = FnvOffsetBasis;
        for (var i = 0; i < runs; i++)
        {
            var seed = (ulong)seedBase + (ulong)i + 1;
            var state = new FarmState { RngState = seed };
            var context = new StepContext(new DeterministicRng(state.RngState));
            var command = new DemoGainMoneyCommand(3);

            var rejection = command.Validate(state, context);
            if (rejection is not null)
            {
                Console.Error.WriteLine($"种子 {seed}: 命令被拒绝 ({rejection.Value.Code})。");
                return 1;
            }

            command.Apply(state, context);
            state.RngState = context.Rng.State;
            state.TotalSteps++;

            finalHash = Mix(finalHash, state);
        }

        Console.WriteLine($"Balance skeleton OK: runs={runs}, seed_base={seedBase}, final_hash=0x{finalHash:X16}");
        return 0;
    }

    private static bool TryParsePositive(string[] args, string name, int fallback, out int value)
    {
        if (!TryParseNonNegative(args, name, fallback, out value))
        {
            return false;
        }

        if (value < 1)
        {
            return false;
        }

        return true;
    }

    private static bool TryParseNonNegative(string[] args, string name, int fallback, out int value)
    {
        value = fallback;
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out value) || value < 0)
            {
                return false;
            }

            return true;
        }

        return true;
    }

    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    private static ulong Mix(ulong hash, FarmState state)
    {
        hash = Mix(hash, (ulong)state.SaveVersion);
        hash = Mix(hash, state.RngState);
        hash = Mix(hash, (ulong)state.SeasonIndex);
        hash = Mix(hash, (ulong)state.RoundIndex);
        hash = Mix(hash, (ulong)state.TotalSteps);
        hash = Mix(hash, (ulong)state.Money);
        hash = Mix(hash, (ulong)state.QiOfSui);
        return hash;
    }

    private static ulong Mix(ulong hash, ulong value)
    {
        for (var i = 0; i < 8; i++)
        {
            hash ^= (byte)(value >> (i * 8));
            hash *= FnvPrime;
        }

        return hash;
    }

    private sealed class DemoGainMoneyCommand : ICommand
    {
        private readonly int _amount;

        public DemoGainMoneyCommand(int amount) => _amount = amount;

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
