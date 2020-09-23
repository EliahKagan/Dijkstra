<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

#load "./helpers.linq"
#load "./generator.linq"

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
