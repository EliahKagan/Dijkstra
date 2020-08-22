<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

/// <summary>
/// Priority queue operations for Prim's and Dijkstra's algorithms.
/// </summary>
internal interface IPrimHeap<TKey, TValue> {
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
internal sealed class NaivePrimHeap<TKey, TValue> : IPrimHeap<TKey, TValue>
        where TKey : notnull {
    public NaivePrimHeap() : this(Comparer<TValue>.Default) { }
        
    public NaivePrimHeap(IComparer<TValue> comparer)
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
        // TODO: Implement a MinBy extension method and refactor to use it.
        
        KeyValuePair<TKey, TValue> entry;
        
        using (var en = _entries.GetEnumerator()) {
            if (!en.MoveNext())
                throw new InvalidOperationException("nothing to extract");
            
            entry = en.Current;
            
            while (en.MoveNext()) {
                if (_comparer.Compare(en.Current.Value, entry.Value) < 0)
                    entry = en.Current;
            }
        }
        
        _entries.Remove(entry.Key);
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
internal sealed class BinaryPrimHeap<TKey, TValue> : IPrimHeap<TKey, TValue>
        where TKey : notnull {
    public BinaryPrimHeap() : this(Comparer<TValue>.Default) { }
        
    public BinaryPrimHeap(IComparer<TValue> comparer)
        => _comparer = comparer;
    
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
            throw new InvalidOperationException("nothing to extract");
        
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
    
    private readonly IDictionary<TKey, int> _map =
        new Dictionary<TKey, int>();
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
                    message: "can't have negatively many vertices");
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
                    message: "negative weights are not supported");
        }
        
        _adj[src].Add((dest, weight));
    }
    
    internal long?[] ComputeShortestPaths<THeap>(int start)
        where THeap : IPrimHeap<int, long>, new()
    {
        CheckVertex(nameof(start), start);
    
        var parents = new long?[Order];
        var done = new BitArray(Order);
        var heap = new THeap();
        
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
    
    private void CheckVertex(string paramName, int vertex)
    {
        if (!(0 <= vertex && vertex < Order)) {
            throw new ArgumentOutOfRangeException(
                    paramName: paramName,
                    message: $"vertex {vertex} out of range");
        }
    }

    private readonly IList<IList<(int dest, int weight)>> _adj;
}

/// <summary>UI to accept a graph description and trigger a run.</summary>
internal sealed class Controller {
    internal Controller() : this(
        initialOrder: "6",
        initialEdges: "1 2 17\n3 0 12\n0 5 19\n1 5 8\n4 3 100\n5 4 2\n4 2 60\n1 4 7\n2 1 69")
    {
    }

    internal Controller(string initialOrder, string initialEdges)
    {
        _order.Rows = 1;
        _order.Cols = 10;
        _order.Text = initialOrder;
        
        _edges.Rows = 20;
        _edges.Cols = 50;
        _edges.Text = initialEdges;
        
        _run.Click += delegate {
            // Always build the graph, even if no handler is registered to
            // accept it, so that wrong input will always be reported.
            var graph = BuildGraph();
            
            Run?.Invoke(graph);
        };
        
        _order.Dump("Order");
        _edges.Dump("Edges");
        _run.Dump();
    }
    
    internal event Action<Graph>? Run;
    
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

    private readonly TextArea _order = new TextArea();
    
    private readonly TextArea _edges = new TextArea();
    
    private readonly Button _run = new Button("Run");
}

private static void Run(Graph graph)
{
    var naive = graph.ComputeShortestPaths<NaivePrimHeap<int, long>>(0);
    naive.Dump("Parents, by Dijkstra's algorithm with a NAIVE PRIORITY QUEUE");
    
    var binary = graph.ComputeShortestPaths<BinaryPrimHeap<int, long>>(0);
    binary.Dump("Parents, by Dijkstra's algorithm with a BINARY MINHEAP");
    
    naive.SequenceEqual(binary).Dump("Same result?");
}

private static void Main()
{
    var controller = new Controller();
    controller.Run += Run;
}
