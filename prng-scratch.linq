<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

// Copyright (C) 2020 Eliah Kagan <degeneracypressure@gmail.com>
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
// SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION
// OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
// CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

#load "./helpers.linq"
#load "./generator.linq"

private static void RunDistinct(LongRandom prng, ulong n, int k)
{
    var sampler = new DistinctSampler(prng, n);

    k.Dump();

    Enumerable.Range(0, k)
              .Select(_ => sampler.Next())
              .Distinct()
              .Count()
              .Dump();
}

#if false
private static void RunDistinctVerbose(LongRandom prng, ulong n, int k)
{
    var sampler = new DistinctSampler(prng, n);

    var samples = Enumerable.Range(0, k)
                            .Select(_ => sampler.Next())
                            .ToList();

    var distinct = samples.Distinct().ToList();

    Util.HorizontalRun(withGaps: true, samples.Count, distinct.Count).Dump();
    Util.HorizontalRun(withGaps: true, samples, distinct).Dump();
}
#endif

private static void TestDistinct()
{
    const ulong n = int.MaxValue * 2uL;
    const int k = 1_000_000;
    var prng = new FastLongRandom();

    RunDistinct(prng, n, k);
    Util.RawHtml("<hr/>").Dump();
    //RunDistinctVerbose(prng, n, k);
    //Util.RawHtml("<hr/>").Dump();
}

private static void RunInt32(LongRandom prng, int min, int max, int k)
{
    new { min, max }.Dump();

    var samples = Enumerable.Range(0, k)
                            .Select(_ => prng.NextInt32(min, max))
                            .ToList();

    var lowest = samples.Min();
    var highest = samples.Max();

    new { lowest, highest }.Dump();

    new {
        lowGap = checked(lowest - min),
        highGap = checked(max - highest)
    }.Dump();
}

private static void TestInt32()
{
    const int k = 5_000_000;
    const int d = 40_000_000;
    const int reps = 5;

    var prng = new GoodLongRandom();

    RunInt32(prng, int.MinValue, int.MaxValue, k);

    Util.RawHtml("<hr/>").Dump();

    for (var i = 0; i < reps; ++i)
        RunInt32(prng, int.MinValue, int.MinValue + d, k);

    Util.RawHtml("<hr/>").Dump();

    for (var i = 0; i < reps; ++i)
        RunInt32(prng, int.MaxValue - d, int.MaxValue, k);

    Util.RawHtml("<hr/>").Dump();
}

private static void Main()
{
    TestDistinct();
    TestInt32();
}
