<Query Kind="Program" />

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
/// A Fibonacci minheap providing priority queue operations for Prim's and
/// Dijkstra's algorithms.
/// </summary>
/// <remarks>O(1) insert/decrease. O(log n) extract-min. (Amortized.)</remarks>
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
        } else if (LessThan(value, node.Value)) {
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
        return KeyValuePair.Create(node.Key, node.Value);
    }

    private sealed class Node {
        internal Node(TKey key, TValue value)
            => (Key, Value, Prev, Next) = (key, value, this, this);

        internal void Disconnect() => Prev = Next = this;

        internal void ConnectAfter(Node prev)
        {
            Prev = prev;
            Next = prev.Next;
            Prev.Next = Next.Prev = this;
        }

        public TKey Key { get; }

        public TValue Value { get; set; }

        public Node? Parent { get; set; } = null;

        public Node Prev { get; set; }

        public Node Next { get; set; }

        public Node? Child { get; set; } = null;

        public int Degree { get; set; } = 0;

        public bool Mark { get; set; } = false;
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
        
        if (_min == null) {
            _min = node;
        } else {
            node.ConnectAfter(_min);
            if (LessThan(node.Value, _min.Value)) _min = node;
        }
    }
    
    private Node ExtractMinNode()
    {
        // FIXME: Factor some parts out into helper methods.
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
            Console.WriteLine("No other roots."); // FIXME: remove
            _min = null;
        } else {
            // Remove the minimum node and consolidate the remaning roots.
            _min = parent.Next;
            Console.WriteLine("Consolidating."); // FIXME: remove
            Consolidate();
            Console.WriteLine("Done consolidating."); // FIXME: remove
        }
        
        _min.Dump();
        return parent;
    }

    private void Consolidate()
    {
        // TODO: When a root is made a child, remove it from the linked list of
        // roots immediately, rather than rebuilding the linked list in a
        // separate pass.

        var by_degree = new Node?[DegreeCeiling + 2]; // FIXME: +1? +2?

        // Link trees together so no two roots have the same degree.
        foreach (var root in GetRoots()) {
            var parent = root;
            var degree = parent.Degree;
            
            while (by_degree[degree] != null) {
                var child = by_degree[degree]!; // FIXME: do this better
                
                if (LessThan(child.Value, parent.Value))
                    (parent, child) = (child, parent);
                
                Link(parent, child);
                by_degree[degree++] = null;
            }
            
            by_degree[degree] = parent;
        }
        
        //foreach (var root in GetRoots()) {
        //    var parent = root;
        //    var degree = parent.Degree;
        //
        //    for (; ; ) {
        //        var child = by_degree[degree];
        //        if (child == null) break;
        //
        //        if (LessThan(child.Value, parent.Value))
        //            (parent, child) = (child, parent);
        //
        //        Link(parent, child);
        //    }
        //
        //    by_degree[degree] = parent;
        //}

        // Rebuild the linked list of roots.
        Console.WriteLine("Rebuilding root list."); // FIXME: remove
        _min = null;
        foreach (var root in by_degree) {
            if (root == null) continue;
            
            // FIXME: Should Disconnect() and ConnectAfter() be responsible
            //        for this?
            if (root.Next != null) {
                Console.WriteLine("Splicing out."); // FIXME: remove
                root.Next.Prev = root.Prev;
                root.Prev.Next = root.Next;
            }

            if (_min == null) {
                root.Disconnect();
                _min = root;
            } else {
                root.ConnectAfter(_min);
                if (LessThan(root.Value, _min.Value)) _min = root;
            }
        }
    }

    /// <summary>Returns an eagerly built list of roots.</summary>
    /// <remarks>Eager, so callers can remove roots while iterating.</remarks>
    private IList<Node> GetRoots()
    {
        Console.WriteLine("Getting roots."); // FIXME: remove
        var roots = new List<Node>();
        if (_min == null) return roots;

        var root = _min;
        do {
            roots.Add(root);
            root = root.Next;
        } while (root != _min);

        Console.WriteLine("Done getting roots."); // FIXME: remove
        return roots;
    }

    private void Link(Node parent, Node child) // FIXME: Hook up .Parent?
    {
        Debug.Assert(child.Parent == null);
        Debug.Assert(child.Next != child);
        
        child.Prev.Next = child.Next;
        child.Next.Prev = child.Prev;
        child.Mark = false;

        if (parent.Child == null) {
            // FIXME: Does this break a linked list of siblings?
            child.Disconnect();
            parent.Child = child;
        } else {
            child.ConnectAfter(parent.Child);
        }

        ++parent.Degree;
    }

    private void Decrease(Node child)
    {
        // FIXME: implement this
    }

    private void Cut(Node parent, Node child)
    {
        // FIXME: implement this
    }

    private void CascadingCut(Node node)
    {
        // FIXME: implement this
    }

    private bool LessThan(TValue lhs, TValue rhs)
        => _comparer.Compare(lhs, rhs) < 0;

    private Node? _min = null;

    private readonly IDictionary<TKey, Node> _map =
        new Dictionary<TKey, Node>();

    private readonly IComparer<TValue> _comparer;
}

private static void Main()
{
    IPriorityQueue<string, int> pq = new FibonacciHeap<string, int>();
    pq.InsertOrDecrease("bar", 48);
    pq.InsertOrDecrease("foo", 23);
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
}
