<Query Kind="Program" />

/// <summary>An edge in a weighted directed graph.</summary>
internal readonly struct Edge {
    internal Edge(int src, int dest, int weight)
        => (Src, Dest, Weight) = (src, dest, weight);

    internal int Src { get; }

    internal int Dest { get; }

    internal int Weight { get; }

    private object ToDump() => new { Src, Dest, Weight };
}
