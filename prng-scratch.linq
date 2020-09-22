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

    /// <summary>Quickly picks an integer from a small range.</summary>
    /// <remarks>
    /// Assumes <c>min</c> is no greater than <c>max</c> and the number of
    /// values in this (inclusive) range is strictly less than
    /// <c>int.MaxValue</c>.
    /// </remarks>
    internal virtual int NextInt32(int min, int max)
        => min + (int)Next((ulong)(max - min));

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
    internal FastLongRandom()
        : this(RandomNumberGenerator.GetInt32(int.MaxValue)) { }

    internal FastLongRandom(int seed) => _random = new Random(seed);

    internal override ulong Next(ulong max)
        => max < int.MaxValue ? (ulong)_random.Next((int)max + 1).Dump("special")
                              : base.Next(max).Dump("general");

    internal override int NextInt32(int min, int max)
        => _random.Next(min, max + 1);

    private protected override void NextBytes(byte[] buffer)
        => _random.NextBytes(buffer);

    private readonly Random _random;
}

internal sealed class GoodLongRandom : LongRandom {
    private protected override void NextBytes(byte[] buffer)
        => _random.GetBytes(buffer);

    private readonly RandomNumberGenerator _random =
        RandomNumberGenerator.Create();
}

internal sealed class DistinctSampler {
    internal DistinctSampler(LongRandom prng, ulong upperExclusive)
        => (_prng, _size) = (prng, upperExclusive);

    internal ulong Next()
    {
        if (_size == 0)
            throw new InvalidOperationException("sample space exhausted");

        var key = _prng.Next(--_size);
        var value = _remap.GetValueOrDefault(key, key);
        _remap[key] = _remap.GetValueOrDefault(_size, _size);
        return value;
    }

    private readonly LongRandom _prng;

    private readonly Dictionary<ulong, ulong> _remap =
        new Dictionary<ulong, ulong>();

    /// <summary>The number of values remaining to hand out.</summary>
    private ulong _size;
}

private static void Main()
{
    const ulong n = int.MaxValue + 50uL;
    const int k = 100;

    var sampler = new DistinctSampler(new FastLongRandom(), n);

#if false
    k.Dump();

    Enumerable.Range(0, k)
              .Select(_ => sampler.Next())
              .Distinct()
              .Count()
              .Dump();
#else
    var samples = Enumerable.Range(0, k)
                            .Select(_ => sampler.Next())
                            .ToList();

    var distinct = samples.Distinct().ToList();

    Util.HorizontalRun(withGaps: true, samples.Count, distinct.Count).Dump();
    Util.HorizontalRun(withGaps: true, samples, distinct).Dump();
#endif
}
