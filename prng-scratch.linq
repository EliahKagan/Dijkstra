<Query Kind="Statements">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

#define LOOP_MASK
//#define LOOP_MASK_CHECK
//#define STACK_BUFFER
#define WITHOUT_DO_WHILE

var random = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
var buffer = new byte[sizeof(ulong)];

#if LOOP_MASK
#if LOOP_MASK_CHECK
static ulong CheckMask(ulong max)
{
    var mask = max;
    mask |= mask >> 1;
    mask |= mask >> 2;
    mask |= mask >> 4;
    mask |= mask >> 8;
    mask |= mask >> 16;
    mask |= mask >> 32;
    return mask;
}
#endif // ! LOOP_MASK_CHECK

static ulong Mask(ulong max)
{
    var mask = max;
    for (var i = 0; i != 6; ++i) mask |= mask >> (1 << i);
    //for (var shift = 1; shift != sizeof(ulong) * 8; shift <<= 1)
    //    mask |= mask >> shift;
#if LOOP_MASK_CHECK
    Debug.Assert(mask == CheckMask(max));
#endif
    return mask;
}
#else
static ulong Mask(ulong max)
{
    var mask = max;
    mask |= mask >> 1;
    mask |= mask >> 2;
    mask |= mask >> 4;
    mask |= mask >> 8;
    mask |= mask >> 16;
    mask |= mask >> 32;
    return mask;
}
#endif // ! LOOP_MASK

#if STACK_BUFFER
ulong Next(ulong max)
{
    var mask = Mask(max);

    Span<ulong> res = stackalloc ulong[1];
    do {
        random.NextBytes(MemoryMarshal.AsBytes(res));
    } while ((res[0] &= mask) > max);

    return res[0];
}
#elif WITHOUT_DO_WHILE
ulong Next(ulong max)
{
    var mask = Mask(max);

    for (; ; ) {
        random.NextBytes(buffer);
        var result = BitConverter.ToUInt64(buffer, 0) & mask;
        if (result <= max) return result;
    }
}
#else
ulong Next(ulong max)
{
    var mask = Mask(max);

    ulong result;
    do {
        random.NextBytes(buffer);
        result = BitConverter.ToUInt64(buffer, 0) & mask;
    } while (result > max);

    return result;
}
#endif // ! STACK_BUFFER

const ulong max = (1uL << 63) + 1uL;
var acc = 0uL;
for (var i = 0; i != 100_000_000; ++i) {
    unchecked {
        acc += Next(max);
    }
}
acc.Dump();

//const int max = 20;
//var freqs = new int[max + 1];
//for (var i = 0; i < 10; ++i) ++freqs[(int)Next(max)];
//freqs.Dump(nameof(freqs));
