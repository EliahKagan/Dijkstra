<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

internal abstract class LongRandom {
    static LongRandom()
        => Debug.Assert(1 << ShiftCount == BufferSize * BitsPerByte);

    internal virtual ulong Next(ulong max)
    {
        var mask = Mask(max);

        for (; ; ) {
            NextBytes(_buffer);
            var result = BitConverter.ToUInt64(_buffer, 0) & mask;
            if (result <= max) return result;
        }
    }

    private protected abstract void NextBytes(byte[] buffer);

    private const int BitsPerByte = 8;
    private const int BufferSize = sizeof(ulong); // 8
    private const int ShiftCount = 6;

    private static ulong Mask(ulong max)
    {
        var mask = max;
        for (var i = 0; i != ShiftCount; ++i) mask |= mask >> (1 << i);
        return mask;
    }

    private readonly byte[] _buffer = new byte[BufferSize];
}

internal sealed class FastLongRandom : LongRandom {
    internal override ulong Next(ulong max)
        => max < int.MaxValue ? (ulong)_random.Next((int)max + 1)
                              : base.Next(max);

    private protected override void NextBytes(byte[] buffer)
        => _random.NextBytes(buffer);

    private readonly Random _random =
        new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
}

private static void Main()
{
    LongRandom prng = new FastLongRandom();

    //const ulong max = (1uL << 63) + 1uL;
    const ulong max = 1_000_000_000;
    var acc = 0uL;
    for (var i = 0; i != 100_000_000; ++i) {
        unchecked {
            acc += prng.Next(max);
        }
    }
    acc.Dump();

    //const int max = 20;
    //var freqs = new int[max + 1];
    //for (var i = 0; i < 10; ++i) ++freqs[(int)Next(max)];
    //freqs.Dump(nameof(freqs));
}
