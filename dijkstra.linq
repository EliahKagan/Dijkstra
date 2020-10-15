<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

#load "./helpers.linq"
#load "./generator.linq"

/// <summary>Configuration options not exposed by the controller.</summary>
internal static class Options {
    internal static bool DisableInputWhileProcessing => true;
    internal static bool OfferWrongQueue => false;
    internal static bool DebugFibonacciHeap => false;
}

/// <summary>LINQ-style extension methods.</summary>
internal static class EnumerableExtensions {
    internal static TSource
    MinBy<TSource, TKey>(this IEnumerable<TSource> source,
                         Func<TSource, TKey> keySelector)
        => source.MinBy(keySelector, Comparer<TKey>.Default);

    internal static TSource
    MinBy<TSource, TKey>(this IEnumerable<TSource> source,
                         Func<TSource, TKey> keySelector,
                         IComparer<TKey> comparer)
    {
        using var en = source.GetEnumerator();

        if (!en.MoveNext())
            throw new InvalidOperationException("Source contains no elements");

        var min = en.Current;
        var minKey = keySelector(min);

        while (en.MoveNext()) {
            var curKey = keySelector(en.Current);

            if (comparer.Compare(curKey, minKey) < 0) {
                min = en.Current;
                minKey = curKey;
            }
        }

        return min;
    }
}

/// <summary>
/// String parsing methods used in mutliple (conceptually unrealted) places.
/// </summary>
internal static class StringExtensions {
    internal static string Before(this string text, char delimiter)
    {
        var end = text.IndexOf(delimiter);
        return end == -1 ? text : text[0..end];
    }
}

/// <summary>
/// Supplies a custom informal name for a data structure.
/// <see cref="TypeExtensions.GetInformalName(System.Type)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
                Inherited = false, AllowMultiple = false)]
internal sealed class InformalNameAttribute : Attribute {
    internal InformalNameAttribute(string informalName)
        => InformalName = informalName;

    internal string InformalName { get; }
}

/// <summary>Convenience functionality for using reflection.</summary>
internal static class TypeExtensions {
    internal static string GetInformalName(this Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(InformalNameAttribute),
                                             inherit: false);
        if (attrs.Length != 0)
            return ((InformalNameAttribute)attrs[0]).InformalName;

        return string.Join(" ", GetLowerCamelWords(type.Name.Before('`')));
    }

    internal static Func<T> CreateSupplier<T>(this Type type) where T : notnull
        => () => type.CreateAsNonnull<T>();

    private static IEnumerable<string> GetLowerCamelWords(string name)
        => camelParser.Matches(name).Select(match => match.Value.ToLower());

    private static T CreateAsNonnull<T>(this Type type) where T : notnull
    {
        var instance = Activator.CreateInstance(type, nonPublic: true);

        if (instance == null) {
            throw new NotSupportedException(
                    "Bug: non-nullable type instantiated null");
        }

        return (T)instance;
    }

    private static readonly Regex camelParser =
        new Regex(@"(?:^.|\p{Lu})\P{Lu}*");
}

/// <summary>
/// Priority queue operations for Prim's and Dijkstra's algorithms.
/// </summary>
internal interface IPriorityQueue<TKey, TValue> {
    /// <summary>The number of mappings stored in the heap.</summary>
    int Count { get; }

    /// <summary>
    /// Maps <c>key</c> to <c>value</c> if it is not already present with
    /// a smaller value.
    /// </summary>
    /// <returns>
    /// <c>true</c> iff a mapping was inserted or modified (decreased).
    /// </returns>
    bool InsertOrDecrease(TKey key, TValue value);

    /// <summary>Extracts a mapping of minimal value.</summary>
    /// <returns>The extracted mapping.</returns>
    KeyValuePair<TKey, TValue> ExtractMin();
}

/// <summary>
/// A naive priority queue for Prim's and Dijkstra's algorithms.
/// </summary>
/// <remarks>O(1) insert/decrease. O(n) extract-min.</remarks>
internal sealed class UnsortedPriorityQueue<TKey, TValue>
        : IPriorityQueue<TKey, TValue> where TKey : notnull {
    internal UnsortedPriorityQueue() : this(Comparer<TValue>.Default) { }

    internal UnsortedPriorityQueue(IComparer<TValue> comparer)
        => _comparer = comparer;

    public int Count => _entries.Count;

    public bool InsertOrDecrease(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var oldValue)
                && _comparer.Compare(oldValue, value) <= 0)
            return false;

        _entries[key] = value;
        return true;
    }

    public KeyValuePair<TKey, TValue> ExtractMin()
    {
        var entry = _entries.MinBy(entry => entry.Value, _comparer);
        _entries.Remove(entry);
        return entry;
    }

    private readonly IComparer<TValue> _comparer;

    private readonly IDictionary<TKey, TValue> _entries =
        new Dictionary<TKey, TValue>();
}

/// <summary>
/// A priority queue for Prim's and Dijkstra's algorithms, based on a
/// self-balancing binary search tree.
/// </summary>
/// <remarks>
/// O(log n) insert/decrease. O(log n) extract-min.
/// <para>
/// Prefer <see cref="BinaryHeap"/> to this. They have the same asymptotic
/// runtimes, but this is likely to be slower by a constant factor.
/// </para>
/// <para>
/// This uses <see cref="System.Collections.Generic.SortedSet"/>, which is
/// implemented as a red-black tree.
/// </para>
/// </remarks>
[InformalName("red-black tree")]
internal sealed class SortedSetPriorityQueue<TKey, TValue>
        : IPriorityQueue<TKey, TValue> where TKey : notnull {
    // TODO: Reimplement this to use a tree multiset (instead of a tree set, as
    // it does now) to lift the strange reliance on TKey being totally ordered.
    // .NET doesn't ship one, but one could be pulled in as a nuget dependency,
    // implemented as a general-purpose sorted multiset elsewhere in this
    // program, or (this might be best) implemented in a special-purpose way in
    // this class, so _tree hands out node references for _map to map keys to.

    internal SortedSetPriorityQueue() : this(Comparer<TValue>.Default) { }

    internal SortedSetPriorityQueue(IComparer<TValue> comparer)
    {
        _valueComparer = comparer;

        _tree = new SortedSet<KeyValuePair<TKey, TValue>>(
                        GetEntryComparer(comparer));
    }

    public int Count => _tree.Count;

    public bool InsertOrDecrease(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var oldValue)) {
            if (_valueComparer.Compare(oldValue, value) <= 0) return false;
            _tree.Remove(KeyValuePair.Create(key, oldValue));
        }

        _map[key] = value;
        _tree.Add(KeyValuePair.Create(key, value));
        return true;
    }

    public KeyValuePair<TKey, TValue> ExtractMin()
    {
        if (Count == 0)
            throw new InvalidOperationException("Nothing to extract");

        var min = _tree.Min;
        _tree.Remove(min);
        _map.Remove(min.Key);
        return min;
    }

    private static IComparer<KeyValuePair<TKey, TValue>>
    GetEntryComparer(IComparer<TValue> valueComparer)
        => Comparer<KeyValuePair<TKey, TValue>>.Create((lhs, rhs) => {
            var byValue = valueComparer.Compare(lhs.Value, rhs.Value);
            if (byValue != 0) return byValue;
            return Comparer<TKey>.Default.Compare(lhs.Key, rhs.Key);
        });

    private readonly IComparer<TValue> _valueComparer;

    private readonly SortedSet<KeyValuePair<TKey, TValue>> _tree;

    private readonly IDictionary<TKey, TValue> _map =
        new Dictionary<TKey, TValue>();
}

/// <summary>
/// A binary minheap providing priority queue operations for Prim's and
/// Dijkstra's algorithms. Sometimes called a "heap + map" data structure.
/// </summary>
/// <remarks>O(log n) insert/decrease. O(log n) extract-min.</remarks>
internal sealed class BinaryHeap<TKey, TValue> : IPriorityQueue<TKey, TValue>
        where TKey : notnull {
    internal BinaryHeap() : this(Comparer<TValue>.Default) { }

    internal BinaryHeap(IComparer<TValue> comparer) => _comparer = comparer;

    public int Count => _heap.Count;

    public bool InsertOrDecrease(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var index)) {
            // Stop if the stored value is no greater than the given value.
            if (OrderOK(_heap[index].Value, value)) return false;

            _heap[index] = KeyValuePair.Create(key, value);
        } else {
            index = Count;
            _heap.Add(KeyValuePair.Create(key, value));
        }

        SiftUp(index);
        return true;
    }

    public KeyValuePair<TKey, TValue> ExtractMin()
    {
        if (Count == 0)
            throw new InvalidOperationException("Nothing to extract");

        var entry = _heap[0];
        var last = Count - 1;

        if (last == 0) {
            _heap.Clear();
            _map.Clear();
        } else {
            _map.Remove(entry.Key);
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            SiftDown(0);
        }

        return entry;
    }

    private const int None = -1;

    private void SiftUp(int child)
    {
        var entry = _heap[child];

        while (child != 0) {
            var parent = (child - 1) / 2;
            if (OrderOK(_heap[parent].Value, entry.Value)) break;

            Set(child, _heap[parent]);
            child = parent;
        }

        Set(child, entry);
    }

    private void SiftDown(int parent)
    {
        var entry = _heap[parent];

        for (; ; ) {
            var child = PickChild(parent);
            if (child == None || OrderOK(entry.Value, _heap[child].Value))
                break;

            Set(parent, _heap[child]);
            parent = child;
        }

        Set(parent, entry);
    }

    private int PickChild(int parent)
    {
        var left = parent * 2 + 1;
        if (left >= Count) return None;

        var right = left + 1;
        return right == Count || OrderOK(_heap[left].Value, _heap[right].Value)
            ? left
            : right;
    }

    private bool OrderOK(TValue parentValue, TValue childValue)
        => _comparer.Compare(parentValue, childValue) <= 0;

    private void Set(int index, KeyValuePair<TKey, TValue> entry)
    {
        _heap[index] = entry;
        _map[entry.Key] = index;
    }

    private readonly IComparer<TValue> _comparer;

    private readonly IList<KeyValuePair<TKey, TValue>> _heap =
        new List<KeyValuePair<TKey, TValue>>();

    private readonly IDictionary<TKey, int> _map = new Dictionary<TKey, int>();
}

/// <summary>
/// A Fibonacci minheap providing priority queue operations for Prim's and
/// Dijkstra's algorithms.
/// </summary>
/// <remarks>O(1) insert/decrease. O(log n) extract-min. (Amortized.)</remarks>
[InformalName("Fibonacci heap")] // Otherwise it would not show up capitalized.
internal sealed class FibonacciHeap<TKey, TValue>
        : IPriorityQueue<TKey, TValue> where TKey : notnull {
    internal FibonacciHeap() : this(Comparer<TValue>.Default) { }

    internal FibonacciHeap(IComparer<TValue> comparer)
        => _comparer = comparer;

    public int Count => _map.Count;

    public bool InsertOrDecrease(TKey key, TValue value)
    {
        if (!_map.TryGetValue(key, out var node)) {
            Insert(key, value);
        } else if (_comparer.Compare(value, node.Value) < 0) {
            node.Value = value;
            Decrease(node);
        } else {
            return false;
        }

        return true;
    }

    public KeyValuePair<TKey, TValue> ExtractMin()
    {
        if (Count == 0)
            throw new InvalidOperationException("Nothing to extract");

        var node = ExtractMinNode();
        _map.Remove(node.Key);
        if (Options.DebugFibonacciHeap) this.Dump(noTotals: true);
        return KeyValuePair.Create(node.Key, node.Value);
    }

    private sealed class Node {
        internal Node(TKey key, TValue value)
            => (Key, Value, Prev, Next) = (key, value, this, this);

        internal TKey Key { get; }

        internal TValue Value { get; set; }

        internal Node? Parent { get; set; } = null;

        internal Node Prev { get; set; }

        internal Node Next { get; set; }

        internal Node? Child { get; set; } = null;

        internal int Degree { get; set; } = 0;

        internal bool Mark { get; set; } = false;

        private object ToDump() => new {
            Key,
            Value,
            Degree,
            Mark,
            Children = NodesInChain(Child)
        };
    }

    /// <summary>Yields this node (if any) and its siblings, lazily.</summary>
    /// <remarks>
    /// Call <c>ToList</c> if adding or removing nodes during iteration.
    /// </remarks>
    private static IEnumerable<Node> NodesInChain(Node? node)
    {
        if (node == null) yield break;

        yield return node;

        for (var sibling = node.Next; sibling != node; sibling = sibling.Next)
            yield return sibling;
    }

    private static readonly double GoldenRatio = (1.0 + Math.Sqrt(5.0)) / 2.0;

    /// <summary>The maximum degree is no more than this.</summary>
    private int DegreeCeiling => (int)Math.Log(a: Count, newBase: GoldenRatio);

    private void Insert(TKey key, TValue value)
    {
        var node = new Node(key, value);
        InsertNode(node);
        _map.Add(key, node);
    }

    private void InsertNode(Node node)
    {
        Debug.Assert((_min == null) == (Count == 0));
        Debug.Assert(node.Next == node && node.Parent == null);

        if (_min == null) {
            _min = node;
        } else {
            node.Prev = _min;
            node.Next = _min.Next;
            node.Prev.Next = node.Next.Prev = node;

            if (_comparer.Compare(node.Value, _min.Value) < 0) _min = node;
        }
    }

    private Node ExtractMinNode()
    {
        // TODO: Factor some parts out into helper methods.
        Debug.Assert(_min != null);
        var parent = _min;

        if (parent.Child != null) {
            var child = parent.Child;

            // Tell the children their root is about to go away.
            do { // for each child
                child.Parent = null;
                child = child.Next;
            } while (child != parent.Child);

            // Splice the children up into the root chain.
            child.Prev.Next = parent.Next;
            parent.Next.Prev = child.Prev;
            child.Prev = parent;
            parent.Next = child;
        }

        if (parent == parent.Next) {
            // There are no other roots, so just make the forest empty.
            _min = null;
        } else {
            // Remove the minimum node.
            _min = parent.Prev.Next = parent.Next;
            parent.Next.Prev = parent.Prev;
            parent.Prev = parent.Next = parent; // to avoid confusion

            Consolidate();
        }

        return parent;
    }

    private void Consolidate()
    {
        var roots_by_degree = new Node?[DegreeCeiling + 1];

        // Link trees together so no two roots have the same degree.
        foreach (var root in NodesInChain(_min).ToList()) {
            var parent = root;
            var degree = parent.Degree;

            for (; ; ) {
                var child = roots_by_degree[degree];
                if (child == null) break;

                if (_comparer.Compare(child.Value, parent.Value) < 0)
                    (parent, child) = (child, parent);

                Link(parent, child);
                roots_by_degree[degree++] = null;
            }

            roots_by_degree[degree] = parent;
        }

        // Reattach the linked list of roots, at the minimum node.
        _min = roots_by_degree
                .OfType<Node>() // skip nulls
                .MinBy(node => node.Value, _comparer);
    }

    private void Link(Node parent, Node child)
    {
        Debug.Assert(parent.Parent == null && child.Parent == null);

        child.Prev.Next = child.Next;
        child.Next.Prev = child.Prev;
        child.Parent = parent;
        child.Mark = false;

        if (parent.Child == null) {
            parent.Child = child.Prev = child.Next = child;
        } else {
            child.Prev = parent.Child;
            child.Next = parent.Child.Next;
            child.Prev.Next = child.Next.Prev = child;
        }

        ++parent.Degree;
    }

    private void Decrease(Node child)
    {
        if (child.Parent != null) {
            var parent = child.Parent; // Remember it for CascadingCut.
            if (_comparer.Compare(child.Value, parent.Value) < 0) {
                Cut(child);
                CascadingCut(parent);
            }
        }

        Debug.Assert(_min != null);
        if (_comparer.Compare(child.Value, _min.Value) < 0) _min = child;
    }

    private void Cut(Node child)
    {
        Debug.Assert(child.Parent != null);
        Debug.Assert(_min != null);

        if (child == child.Next) {
            Debug.Assert(child.Parent.Child == child);
            child.Parent.Child = null;
        } else {
            if (child.Parent.Child == child) child.Parent.Child = child.Next;
            child.Prev.Next = child.Next;
            child.Next.Prev = child.Prev;
        }

        --child.Parent.Degree;
        child.Parent = null;
        child.Mark = false;

        child.Prev = _min;
        child.Next = _min.Next;
        child.Prev.Next = child.Next.Prev = child;
    }

    private void CascadingCut(Node node)
    {
        // TODO: Maybe implement this iteratively.

        if (node.Parent == null) return;

        if (node.Mark) {
            var parent = node.Parent; // Remember it for the recursive call.
            Cut(node);
            CascadingCut(parent);
        } else {
            node.Mark = true;
        }
    }

    private object ToDump() => new { Count, Roots = NodesInChain(_min) };

    private Node? _min = null;

    private readonly IDictionary<TKey, Node> _map =
        new Dictionary<TKey, Node>();

    private readonly IComparer<TValue> _comparer;
}

/// </summary>
/// A fake priority queue that swallows all input, for testing.
/// </summary>
[InformalName("wrongqueue is wrooooooong")]
internal sealed class WrongQueue<TKey, TValue> : IPriorityQueue<TKey, TValue> {
    public int Count => 0;

    public bool InsertOrDecrease(TKey key, TValue value) => false;

    public KeyValuePair<TKey, TValue> ExtractMin()
        => throw new InvalidOperationException(
                $"Can't extract from {nameof(WrongQueue<TKey, TValue>)},"
                + " which only pretends to take input");
}

/// <summary>Convenience functions for marked edges.</summary>
internal static class MarkedEdge {
    internal static MarkedEdge<T> Create<T>(Edge edge, T mark)
        => new MarkedEdge<T>(edge, mark);
}

/// <summary>A marked edge in a weighted directed graph.</summary>
internal readonly struct MarkedEdge<T> {
    internal MarkedEdge(Edge edge, T mark)
        => (_edge, Mark) = (edge, mark);

    internal int Src => _edge.Src;

    internal int Dest => _edge.Dest;

    internal int Weight => _edge.Weight;

    internal T Mark { get; }

    private object ToDump() => new { Src, Dest, Weight, Mark };

    private readonly Edge _edge;
}

/// <summary>
/// A weighted directed graph represented as an adjacency list.
/// </summary>
internal sealed class Graph {
    internal Graph(int order)
    {
        if (order < 0) {
            throw new ArgumentOutOfRangeException(
                    paramName: nameof(order),
                    message: "Can't have negatively many vertices");
        }

        _adj = new List<IList<(int dest, int weight)>>(capacity: order);

        for (var vertex = 0; vertex != order; ++vertex)
            _adj.Add(new List<(int, int)>());
    }

    internal int Order => _adj.Count;

    internal void Add(int src, int dest, int weight)
    {
        CheckVertex(nameof(src), src);
        CheckVertex(nameof(dest), dest);

        if (weight < 0) {
            throw new ArgumentException(
                    paramName: nameof(weight),
                    message: "Negative weights are not supported");
        }

        _adj[src].Add((dest, weight));
    }

    internal ParentsTree
    ComputeShortestPaths(int start, Func<IPriorityQueue<int, long>> pqSupplier)
    {
        CheckVertex(nameof(start), start);

        var parents = new int?[Order];
        var done = new BitArray(Order);
        var heap = pqSupplier();

        for (heap.InsertOrDecrease(start, 0L); heap.Count != 0; ) {
            var (src, cost) = heap.ExtractMin();
            done[src] = true;

            foreach (var (dest, weight) in _adj[src]) {
                if (!done[dest] && heap.InsertOrDecrease(dest, cost + weight))
                    parents[dest] = src;
            }
        }

        return new ParentsTree(this, parents);
    }

    internal IEnumerable<Edge> Edges
    {
        get {
            foreach (var src in Enumerable.Range(0, Order)) {
                foreach (var (dest, weight) in _adj[src])
                    yield return new Edge(src, dest, weight);
            }
        }
    }

    internal EdgeSelection
    SelectEdges(Func<(int src, int dest), bool> predicate)
    {
        var parallels = GroupParallelEdges();

        IEnumerable<MarkedEdge<bool>> Emit()
        {
            foreach (var (endpoints, group) in parallels) {
                if (predicate(endpoints)) {
                    var indices = Enumerable.Range(0, group.Count);
                    var bestIndex = indices.MinBy(i => group[i].Weight);

                    foreach (var index in indices) {
                        yield return MarkedEdge.Create(group[index],
                                                       index == bestIndex);
                    }
                } else {
                    foreach (var edge in group)
                        yield return MarkedEdge.Create(edge, false);
                }
            }
        }

        return new EdgeSelection(Order, Emit());
    }

    private IReadOnlyDictionary<(int src, int dest), IReadOnlyList<Edge>>
    GroupParallelEdges()
    {
        var parallels = new Dictionary<(int src, int dest),
                                       IReadOnlyList<Edge>>();

        foreach (var group in Edges.GroupBy(edge => (edge.Src, edge.Dest)))
            parallels.Add(group.Key, group.ToList());

        return parallels;
    }

    private void CheckVertex(string paramName, int vertex)
    {
        if (!(0 <= vertex && vertex < Order)) {
            throw new ArgumentOutOfRangeException(
                    paramName: paramName,
                    message: $"Vertex {vertex} out of range");
        }
    }

    private readonly IList<IList<(int dest, int weight)>> _adj;
}

/// <summary>A tree in a graph, represented as a parents list.</summary>
internal sealed class ParentsTree : IEquatable<ParentsTree>,
                                    IReadOnlyList<int?> {
    /// <summary>Constructs a parents tree.</summary>
    /// <remarks>Does not range-check or cycle-check the parents.</remarks>
    internal ParentsTree(Graph graph, IReadOnlyList<int?> parents)
        => (Supergraph, _parents) = (graph, parents);

    internal Graph Supergraph { get; }

    internal int Order => _parents.Count;

    public bool Equals(ParentsTree? other)
        => other != null
            && Supergraph == other.Supergraph
            && _parents.SequenceEqual(other._parents);

    public override bool Equals(object? other)
        => Equals(other as ParentsTree);

    public override int GetHashCode()
    {
        const int seed = 17;
        const int multiplier = 8191;

        var code = seed;

        unchecked {
            code = code * multiplier + Supergraph.GetHashCode();

            foreach (var parent in _parents)
                code = code * multiplier + parent.GetHashCode();
        }

        return code;
    }

    public int? this[int child] => _parents[child];

    int IReadOnlyCollection<int?>.Count => Order;

    public IEnumerator<int?> GetEnumerator() => _parents.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal EdgeSelection ToEdgeSelection()
        => Supergraph.SelectEdges(edge => _parents[edge.dest] == edge.src);

    private object ToDump()
        => _parents.Select((Parent, Child) => new { Child, Parent });

    private readonly IReadOnlyList<int?> _parents;
}

/// <summary>An immutable list of edges with boolean markings.</summary>
internal sealed class EdgeSelection : IReadOnlyList<MarkedEdge<bool>> {
    internal EdgeSelection(int order, IEnumerable<MarkedEdge<bool>> edges)
        => (Order, _edges) = (order, edges.ToList());

    public MarkedEdge<bool> this[int index] => _edges[index];

    public int Count => _edges.Count;

    public IEnumerator<MarkedEdge<bool>> GetEnumerator()
        => _edges.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal int Order { get; }

    internal DotCode ToDotCode(string description)
    {
        const int indent = 4;
        var margin = new string(' ', indent);
        var builder = new StringBuilder();
        builder.AppendLine($"digraph \"{description}\" {{");

        // Emit the vertices in ascending order, to be drawn as circles.
        foreach (var vertex in Enumerable.Range(0, Order))
            builder.AppendLine($"{margin}{vertex} [shape=\"circle\"]");

        builder.AppendLine();

        // Emit the edges in the order given, colorized according to selection.
        foreach (var edge in _edges) {
            var endpoints = $"{edge.Src} -> {edge.Dest}";
            var color = $"color=\"{(edge.Mark ? "red" : "gray")}\"";
            var label = $"label=\"{edge.Weight}\"";
            builder.AppendLine($"{margin}{endpoints} [{color} {label}]");
        }

        builder.AppendLine("}");

        return new DotCode(builder.ToString());
    }

    private readonly IReadOnlyList<MarkedEdge<bool>> _edges;
}

/// <summary>DOT code for input to GraphViz.</summary>
/// <remarks>
/// Currently this just represents DOT as raw text (not an AST or anything).
/// </remarks>
internal sealed class DotCode {
    internal DotCode(string code) => Code = code;

    internal string Code { get; }

    /// <summary>Runs <c>dot</c> to create a temporary SVG file.</summary>
    internal object ToSvg()
    {
        var dir = Path.GetTempPath();
        var guid = Guid.NewGuid();
        var dotPath = Path.Combine(dir, $"{guid}.dot");
        var svgPath = Path.Combine(dir, $"{guid}.svg");

        using (var writer = File.CreateText(dotPath))
            writer.Write(Code);

        var proc = new Process();

        proc.StartInfo.ArgumentList.Add("-Tsvg");
        proc.StartInfo.ArgumentList.Add("-o");
        proc.StartInfo.ArgumentList.Add(svgPath);
        proc.StartInfo.ArgumentList.Add(dotPath);

        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.FileName = "dot";
        proc.StartInfo.RedirectStandardInput = false;
        proc.StartInfo.RedirectStandardOutput = false;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.UseShellExecute = false;

        proc.Start();
        // FIXME: Read standard error?
        proc.WaitForExit();
        // FIXME: Look at exit code?

        // TODO: Offer to run "dot -c" for the user, if dot fails and reports:
        //   Format: "svg" not recognized. Use one of:
        // (When this problem happens, usually that is the full message.)

        return Util.RawHtml(File.ReadAllText(svgPath));
    }

    private object ToDump() => new TextArea(Code, columns: 40);
}

/// <summary>UI to accept a graph description and trigger a run.</summary>
internal sealed class Controller {
    internal sealed class Builder {
        internal Builder Order(int order)
            => SetField(nameof(order), ref _order, order);

        internal Builder Edge(int src, int dest, int weight)
        {
            _edges.Add(new Edge(src, dest, weight));
            return this;
        }

        internal Builder Source(int source)
            => SetField(nameof(source), ref _source, source);

        internal Builder PQ(Type type, bool selected = true)
        {
            _priorityQueues.Add(new PriorityQueueItem(type, selected));
            return this;
        }

        internal Controller Build()
            => new Controller(initialOrder: GetField("order", _order),
                              initialEdges: BuildEdgesText(),
                              initialSource: GetField("source", _source),
                              priorityQueues: _priorityQueues);

        private Builder SetField(string name, ref int? field, int value)
        {
            if (field != null) {
                throw new ArgumentException(
                        paramName: name,
                        message: "property already set");
            }

            field = value;
            return this;
        }

        private string GetField(string name, int? field)
            => field?.ToString()
                ?? throw new InvalidOperationException(
                        $"can't build with {name} property unset");

        private string BuildEdgesText()
        {
            static string MakeLine(Edge edge)
                => $"{edge.Src} {edge.Dest} {edge.Weight}";

            return string.Join(Environment.NewLine, _edges.Select(MakeLine));
        }

        private int? _order = null;

        private readonly IList<Edge> _edges = new List<Edge>();

        private int? _source = null;

        private readonly IList<PriorityQueueItem> _priorityQueues =
            new List<PriorityQueueItem>();
    }

    internal void Show()
    {
        _generate.Dump();

        _order.Dump("Order");
        _edges.Dump("Edges");
        _source.Dump("Source");

        _pqConfig.Dump("Priority queues");
        _outputConfig.Dump("Output");

        _buttons.Dump();
    }

    internal event Action<Graph, int, Func<IPriorityQueue<int, long>>, string>?
    SingleRun;

    internal event Action? RunsCompleted;

    internal bool ParentsTableOn => _parentsTable.Checked;

    internal bool EdgeSelectionOn => _edgeSelection.Checked;

    internal bool DotCodeOn => _dotCode.Checked;

    internal bool DrawingOn => _drawing.Checked;

    private readonly struct PriorityQueueItem {
        internal PriorityQueueItem(Type type, bool selected)
            => (Type, Selected) = (type, selected);

        internal Type Type { get; }

        internal bool Selected { get; }
    }

    private Controller(string initialOrder,
                       string initialEdges,
                       string initialSource,
                       IList<PriorityQueueItem> priorityQueues)
    {
        _generatorDialog.Generating += generatorDialog_Generating;
        _generatorDialog.Generated += generatorDialog_Generated;

        _generate = new Button("Generate a graph...",
                               delegate { _generatorDialog.DisplayDialog(); });

        _order = new TextBox(initialOrder, width: "60px");
        _edges = new TextArea(initialEdges, columns: 50) { Rows = 20 };
        _source = new TextBox(initialSource, width: "60px");

        PopulatePriorityQueueControls(priorityQueues);

        _parentsTable = new CheckBox("parents table", false, Configure);
        _edgeSelection = new CheckBox("edge selection", false, Configure);
        _dotCode = new CheckBox("DOT code", false, Configure);
        _drawing = new CheckBox("graph drawing", true, Configure);

        _outputConfig = new WrapPanel(_parentsTable,
                                      _edgeSelection,
                                      _dotCode,
                                      _drawing);

        _run = new Button("Run", run_Click);
        _buttons = new WrapPanel(_run, new Button("Clear", clear_Click));
    }

    private static string StripCommentsAndWhitespace(string line)
        => line.Before('#').Trim();

    private static int ParseValue(TextBox textBox)
        => int.Parse(StripCommentsAndWhitespace(textBox.Text));

    private void
    PopulatePriorityQueueControls(IList<PriorityQueueItem> priorityQueues)
    {
        if (priorityQueues.Count == 0) {
            throw new ArgumentException(
                    paramName: nameof(priorityQueues),
                    message: "must pass at least one priority queue type");
        }

        foreach (var pq in priorityQueues) {
            var label = pq.Type.GetInformalName();
            var boundType = pq.Type.MakeGenericType(typeof(int), typeof(long));
            var supplier =
                boundType.CreateSupplier<IPriorityQueue<int, long>>();

            _pqSuppliers.Add(label, supplier);

            var pqCheckBox = new CheckBox(label, pq.Selected, Configure);
            _pqConfig.Children.Add(pqCheckBox);
        }
    }

    private void generatorDialog_Generating(object sender,
                                            GraphGeneratingEventArgs e)
    {
        //_generatorDialog.Generated -= generatorDialog_Generated;

        _order.Text = e.Order.ToString();
        ExplainEdgeDelay("Generating", e.Size);
        // TODO: What should source be set to, if anything?

        MaybeDisableInputControls();
    }

    // TODO: Populating the edges can lag the UI. Would setting IsMultithreaded
    // on the textarea ameliorate this? Would it introduce other problems?
    private void generatorDialog_Generated(object sender,
                                           GraphGeneratedEventArgs e)
    {
        _order.Text = e.Order.ToString();

        Debug.Assert(e.Edges.Count == e.Size);
        ExplainEdgeDelay("Populating", e.Size);

        //_generatorDialog.Generated += generatorDialog_Generated;

        // FIXME: DRY (Something like this is already elsewhere, right?)
        var lines = from edge in e.Edges
                    select $"{edge.Src} {edge.Dest} {edge.Weight}";

        _edges.Text = string.Join(Environment.NewLine, lines);

        MaybeEnableInputControls();
    }

    private void run_Click(Button sender)
    {
        MaybeDisableInputControls();
        try {
            // Fail fast on malformed graph input.
            var graph = BuildGraph();
            var source = ParseValue(_source);

            // Don't respond to handler changes while running.
            var singleRun = SingleRun;
            var runsCompleted = RunsCompleted;

            _pqConfig.Children
                     .Cast<CheckBox>()
                     .Where(cb => cb.Checked)
                     .Select(cb => cb.Text)
                     .Select(text => (supplier: _pqSuppliers[text],
                                      label: text))
                     .ToList() // Never respond to config changes in mid-run.
                     .ForEach(row => singleRun?.Invoke(graph,
                                                       source,
                                                       row.supplier,
                                                       row.label));

            runsCompleted?.Invoke();
        } finally {
            MaybeEnableInputControls();
        }
    }

    /// <summary>
    /// Clears all output including the controller, and redraws the controller.
    /// </summary>
    private void clear_Click(Button sender)
    {
        // Save the controller state. For some reason, LINQPad controls
        // sometimes lose the latest changes when Util.ClearResults is called.
        var order = _order.Text;
        var edges = _edges.Text;
        var source = _source.Text;
        var config = CheckBoxes.Select(cb => (cb, cb.Checked)).ToList();

        Util.ClearResults();

        // Restore the controller state, except don't repopulate the edges just
        // yet. LINQPad will not dump more than a fixed amount of data. If the
        // contents are too long (which happens at about a million edges),
        // LINQPad outputs "<limit of graph>" and the controller is not fully
        // drawn. If it were not for that problem, doing it this way would
        // still have the benefit of avoiding unexplained lag when Clear is
        // clicked.
        _order.Text = order;
        ExplainEdgeDelay("Repopulating", EdgeStrings(edges).Count());
        _source.Text = source;
        foreach (var (cb, selected) in config) cb.Checked = selected;

        Show();
        _edges.Text = edges;
    }

    private void Configure(CheckBox sender)
    {
        static bool AnyChecked(WrapPanel panel)
            => panel.Children.Cast<CheckBox>().Any(cb => cb.Checked);

        _run.Enabled = AnyChecked(_pqConfig) && AnyChecked(_outputConfig);
    }

    private Graph BuildGraph()
    {
        var graph = new Graph(ParseValue(_order));

        foreach (var (src, dest, weight) in ReadEdges())
            graph.Add(src, dest, weight);

        return graph;
    }

    private (int src, int dest, int weight)[] ReadEdges()
        => EdgeStrings(_edges.Text)
            .Select(line =>
                line.Split(default(char[]?),
                           StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray())
            .Select(vals =>
                vals.Length == 3
                    ? (src: vals[0], dest: vals[1], weight: vals[2])
                    : throw new InvalidOperationException(
                            message: "wrong record length"))
            .ToArray();

    private IEnumerable<string> EdgeStrings(string edgeInput)
        => edgeInput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(StripCommentsAndWhitespace)
                    .Where(line => !string.IsNullOrWhiteSpace(line));

    private void MaybeDisableInputControls()
    {
        if (Options.DisableInputWhileProcessing)
            foreach (var control in InputControls) control.Enabled = false;
    }

    private void MaybeEnableInputControls()
    {
        if (Options.DisableInputWhileProcessing)
            foreach (var control in InputControls) control.Enabled = true;
    }

    private IEnumerable<Control> InputControls
        => TextControls.Concat(CheckBoxes);

    private IEnumerable<Control> TextControls
    {
        get {
            yield return _order;
            yield return _edges;
            yield return _source;
        }
    }

    private IEnumerable<CheckBox> CheckBoxes
        => _pqConfig.Children
            .Concat(_outputConfig.Children)
            .Cast<CheckBox>();

    private void ExplainEdgeDelay(string verb, int size)
        => _edges.Text = (size == 1 ? $"{verb} 1 edge..."
                                    : $"{verb} {size} edges...");

    private readonly GraphGeneratorDialog _generatorDialog =
        new GraphGeneratorDialog();

    private readonly Button _generate;

    private readonly TextBox _order;

    private readonly TextArea _edges;

    private readonly TextBox _source;

    private readonly IDictionary<string, Func<IPriorityQueue<int, long>>>
    _pqSuppliers = new Dictionary<string, Func<IPriorityQueue<int, long>>>();

    private readonly WrapPanel _pqConfig = new WrapPanel();

    private readonly CheckBox _parentsTable;

    private readonly CheckBox _edgeSelection;

    private readonly CheckBox _dotCode;

    private readonly CheckBox _drawing;

    private readonly WrapPanel _outputConfig;

    private readonly Button _run;

    private readonly WrapPanel _buttons;
}

private static Controller BuildController()
{
    var builder = new Controller.Builder()
        .Order(7)
        .Edge(0, 1, 10)
        .Edge(0, 6, 15)
        .Edge(1, 2, 15)
        .Edge(2, 3, 12)
        .Edge(6, 4, 30)
        .Edge(0, 2, 9)
        .Edge(3, 4, 16)
        .Edge(4, 5, 9)
        .Edge(5, 0, 17)
        .Edge(0, 2, 8)
        .Edge(1, 3, 21)
        .Edge(5, 6, 94)
        .Edge(2, 4, 14)
        .Edge(3, 5, 13)
        .Edge(6, 4, 50)
        .Edge(4, 0, 20)
        .Edge(5, 1, 7)
        .Edge(6, 3, 68)
        .Edge(5, 5, 1)
        .Source(0)
        .PQ(typeof(UnsortedPriorityQueue<,>))
        .PQ(typeof(SortedSetPriorityQueue<,>))
        .PQ(typeof(BinaryHeap<,>))
        .PQ(typeof(FibonacciHeap<,>));

    if (Options.OfferWrongQueue)
        builder.PQ(typeof(WrongQueue<,>), selected: false);

    return builder.Build();
}

private static string BuildDumpLabel(string description,
                                     IEnumerable<string> pqLabels)
{
    const int indent = 4;
    var margin = new string(' ', indent);

    var builder = new StringBuilder($"{description} via:");

    foreach (var label in pqLabels) {
        builder.AppendLine()
               .AppendLine() // Make extra vertical space for readability.
               .Append(margin)
               .Append(label.ToUpper());
    }

    return builder.ToString();
}

private static void Main()
{
    var controller = BuildController();
    var results = new List<(string label, ParentsTree parents)>();

    IEnumerable<(ParentsTree parents, List<string> labels)>
    GroupedResults()
        => from result in results
           group result.label by result.parents into grp
           select (parents: grp.Key, labels: grp.ToList());

    // TODO: Do the work asynchronously, primarily for responsiveness, but also
    // so the computations on different priority queues are done in parallel.
    controller.SingleRun += (graph, source, supplier, label) => {
        var parents = graph.ComputeShortestPaths(source, supplier);
        results.Add((label, parents));
    };

    controller.RunsCompleted += () => {
        Util.RawHtml("<hr/>").Dump();

        var groups = GroupedResults().ToList();

        (groups.Count switch {
            0 => "No results at all. BUG?",
            1 when groups[0].labels.Count > 1
              => "YES, multiple results are consistent.",
            1 => "Technically yes, but there is only one set of results.",
            _ => "NO! Multiple results are inconsistent!"
                    + Environment.NewLine
                    + "(This is okay if there is more than one solution.)"
         }).Dump("Same results with all data structures?");

        foreach (var (parents, labels) in groups) {
            void Display<T>(T content, string description)
                => content.Dump(BuildDumpLabel(description, labels),
                                noTotals: true);

            if (controller.ParentsTableOn) Display(parents, "Parents");

            var selection = parents.ToEdgeSelection();
            if (controller.EdgeSelectionOn)
                Display(selection, "Edge selection");

            const string description = "Full graph, shortest paths in red";

            var dot = selection.ToDotCode(description);
            if (controller.DotCodeOn) Display(dot, "DOT code");

            if (controller.DrawingOn) Display(dot.ToSvg(), $"{description},");
        }

        results.Clear();
    };

    controller.Show();
}
