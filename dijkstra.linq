<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

/// <summary>LINQ-style extension methods.</summary>
internal static class EnumerableExtensions {
    internal static TSource
    MinBy<TSource, TKey>(this IEnumerable<TSource> source,
                         Func<TSource, TKey> keySelector,
                         IComparer<TKey>? comparer = null)
    {
        comparer ??= Comparer<TKey>.Default;

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

/// <summary>Convenience functionality for using reflection.</summary>
internal static class TypeExtensions {
    internal static string GetInformalName(this Type type)
    {
        var name = type.Name;
        var end = name.IndexOf('`');
        if (end != -1) name = name[0..end];

        return string.Join(" ", GetLowerCamelWords(name));
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
internal sealed class UnsortedArrayPriorityQueue<TKey, TValue>
        : IPriorityQueue<TKey, TValue> where TKey : notnull {
    internal UnsortedArrayPriorityQueue() : this(Comparer<TValue>.Default) { }

    internal UnsortedArrayPriorityQueue(IComparer<TValue> comparer)
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

/// <summary>An edge in a weighted directed graph.</summary>
internal readonly struct Edge {
    internal Edge(int src, int dest, int weight)
        => (Src, Dest, Weight) = (src, dest, weight);

    internal int Src { get; }

    internal int Dest { get; }

    internal int Weight { get; }
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

    private readonly Edge _edge;
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

    private readonly IReadOnlyList<MarkedEdge<bool>> _edges;
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

    internal int?[]
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

        return parents;
    }

    private IEnumerable<Edge> Edges
    {
        get {
            foreach (var src in Enumerable.Range(0, Order)) {
                foreach (var (dest, weight) in _adj[src])
                    yield return new Edge(src, dest, weight);
            }
        }
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

    private EdgeSelection
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

/// <summary>Extended functionality for graphs.</summary>
internal static class GraphExtensions {
    private static bool DebugParents => false;
    private static bool DebugEdgeSelection => false;
    private static bool DebugDot => false;

    internal static int?[]
    ShowShortestPaths(this Graph graph,
                      int source,
                      Func<IPriorityQueue<int, long>> pqSupplier,
                      string label)
    {
        var parents = graph.ComputeShortestPaths(source, pqSupplier);
        if (DebugParents) parents.Dump($"Parents via {label}");

        var edgeSelection =
            EmitEdgeSelection(graph.Edges,
                              edge => parents[edge.dest] == edge.src)
                .ToArray();
        if (DebugEdgeSelection)
            edgeSelection.Dump($"Edge selection via {label}", noTotals: true);

        var description = $"Shortest-path tree via {label}";
        var dot = edgeSelection.ToDot(graph.Order, description);
        if (DebugDot) dot.Dump($"DOT code via {label}");
        dot.Visualize(description);

        return parents;
    }

    private static string
    ToDot(this EdgeSelection selection, int order, string description)
    {
        const int indent = 4;
        var margin = new string(' ', indent);
        var builder = new StringBuilder();
        builder.AppendLine($"digraph \"{description}\" {{");

        // Emit the vertices in ascending order, to be drawn as circles.
        foreach (var vertex in Enumerable.Range(0, order))
            builder.AppendLine($"{margin}{vertex} [shape=\"circle\"]");

        builder.AppendLine();

        // Emit the edges in the order given, colorized according to selection.
        foreach (var edge in selection) {
            var endpoints = $"{edge.Src} -> {edge.Dest}";
            var color = $"color=\"{(edge.Mark ? "red" : "gray")}\"";
            var label = $"label=\"{edge.Weight}\"";
            builder.AppendLine($"{margin}{edge} [{color} {label}]");
        }

        return builder.AppendLine("}").ToString();
    }

    private static void Visualize(this string dot, string description)
    {
        var dir = Path.GetTempPath();
        var guid = Guid.NewGuid();
        var dotPath = Path.Combine(dir, $"{guid}.dot");
        var svgPath = Path.Combine(dir, $"{guid}.svg");

        using (var writer = File.CreateText(dotPath))
            writer.Write(dot);

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

        Util.Image(svgPath).Dump(description);
    }
}

/// <summary>UI to accept a graph description and trigger a run.</summary>
internal sealed class Controller {
    internal Controller(params Type[] priorityQueues) : this(
        initialOrder: "7",
        initialEdges: "0 1 10\n0 6 15\n1 2 15\n2 3 12\n6 4 30\n0 2 9\n3 4 16\n4 5 9\n5 0 17\n0 2 8\n1 3 21\n5 6 94\n2 4 14\n3 5 13\n6 4 50\n4 0 20\n5 1 7\n6 3 68\n5 5 1\n",
        initialSource: "0",
        priorityQueues)
    {
    }

    internal Controller(string initialOrder,
                        string initialEdges,
                        string initialSource,
                        params Type[] priorityQueues)
    {
        _order = new TextArea(initialOrder, columns: 10);
        _order.Rows = 1;

        _edges = new TextArea(initialEdges, columns: 50);
        _edges.Rows = 20;

        _source = new TextArea(initialSource, columns: 10);
        _source.Rows = 1;

        PopulateConfig(priorityQueues);

        _run = new Button("Run", OnRun);
        _buttons = new WrapPanel(_run, new Button("Clear", OnClear));
    }

    internal void Show()
    {
        _order.Dump("Order");
        _edges.Dump("Edges");
        _source.Dump("Source");
        _pqConfig.Dump("Priority queues");
        _buttons.Dump();
    }

    internal event Action<Graph, int, Func<IPriorityQueue<int, long>>, string>?
    Run;

    private void OnRun(Button sender)
    {
        // Always build the graph, even if no handler is registered to
        // accept it, so that wrong input will always be reported.
        var graph = BuildGraph();
        var source = int.Parse(_source.Text);

        var run = Run; // Don't respond to handler changes while running.
        if (run == null) return;

        _pqConfig.Children
                 .Cast<CheckBox>()
                 .Where(cb => cb.Checked)
                 .Select(cb => cb.Text)
                 .Select(text => (supplier: _pqSuppliers[text], label: text))
                 .ToList() // Don't respond to config changes while running.
                 .ForEach(row => run(graph, source, row.supplier, row.label));
    }

    private Graph BuildGraph()
    {
        var graph = new Graph(ReadOrder());

        foreach (var (src, dest, weight) in ReadEdges())
            graph.Add(src, dest, weight);

        return graph;
    }

    private int ReadOrder() => int.Parse(_order.Text);

    private (int src, int dest, int weight)[] ReadEdges()
        => _edges.Text
                 .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                 .Where(line => !string.IsNullOrWhiteSpace(line))
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

    private void OnClear(Button sender)
    {
        var order = _order.Text;
        var edges = _edges.Text;
        var source = _source.Text;

        var config = _pqConfig.Children
                              .Cast<CheckBox>()
                              .Select(cb => (cb, cb.Checked))
                              .ToArray();

        Util.ClearResults();

        _order.Text = order;
        _edges.Text = edges;
        _source.Text = source;

        foreach (var (cb, @checked) in config) cb.Checked = @checked;

        Show();
    }

    private void OnConfig(CheckBox? sender)
        => _run.Enabled = _pqConfig.Children
                                   .Cast<CheckBox>()
                                   .Any(cb => cb.Checked);

    void PopulateConfig(Type[] priorityQueues)
    {
        if (priorityQueues.Length == 0) {
            throw new ArgumentException(
                    paramName: nameof(priorityQueues),
                    message: "must pass at least one priority queue type");
        }

        foreach (var type in priorityQueues) {
            var label = type.GetInformalName();
            var boundType = type.MakeGenericType(typeof(int), typeof(long));
            var supplier =
                boundType.CreateSupplier<IPriorityQueue<int, long>>();

            _pqSuppliers.Add(label, supplier);
            _pqConfig.Children.Add(new CheckBox(label, true, OnConfig));
        }
    }

    private readonly TextArea _order;

    private readonly TextArea _edges;

    private readonly TextArea _source;

    private readonly IDictionary<string, Func<IPriorityQueue<int, long>>>
    _pqSuppliers = new Dictionary<string, Func<IPriorityQueue<int, long>>>();

    private readonly WrapPanel _pqConfig = new WrapPanel();

    private readonly Button _run;

    private readonly WrapPanel _buttons;
}

private static void Main()
{
    var controller = new Controller(typeof(UnsortedArrayPriorityQueue<,>),
                                    typeof(BinaryHeap<,>));

    var results = new List<int?[]>();

    controller.Run += (graph, source, supplier, label) => {
        results.Add(graph.ShowShortestPaths(source,
                                            supplier,
                                            label.ToUpper()));
    };

    controller.Show();

    if (results.Count > 1) {
        results.Skip(1).All(res => results[0].SequenceEqual(res))
               .Dump("Same results?");
    }
}
