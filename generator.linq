<Query Kind="Program">
  <Namespace>LC = LINQPad.Controls</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Numerics</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

#load "./helpers.linq"

/// <summary>Extensions for clearer and more compact regex usage.</summary>
internal static class MatchExtensions {
    internal static string Group(this Match match, int index)
        => match.Groups[index].ToString();
}

/// <summary>
/// An inclusive range represented by a pair of integer endpoints.
/// </summary>
/// <remarks>
/// This differs from <see cref="System.Range"/> by being closed and not
/// supporting <c>FromEnd</c> (endpoints are absolute, i.e., from start).
/// </remarks>
internal readonly struct ClosedInterval {
    internal static ClosedInterval? Parse(string text)
        => SplitParse(text, MaybeNegativeIntervalSplitter);

    internal static ClosedInterval? ParseNonNegative(string text)
        => SplitParse(text, NonNegativeIntervalSplitter);

    internal ClosedInterval(int min, int max) => (Min, Max) = (min, max);

    public override string ToString() => $"{Min}-{Max}";

    internal int Min { get; }

    internal int Max { get; }

    internal long Count => Max < Min ? 0L : (long)Max - (long)Min + 1L;

    private static ClosedInterval? SplitParse(string text, Regex splitter)
    {
        var match = splitter.Match(text);

        if (match.Success && int.TryParse(match.Group(1), out var start)
                          && int.TryParse(match.Group(2), out var end))
            return new ClosedInterval(start, end);

        return null;
    }

    private static readonly Regex MaybeNegativeIntervalSplitter =
        new Regex(@"^\s*(-?[^-\s]+)\s*-\s*(-?[^-\s]+)\s*$",
                  RegexOptions.Compiled);

    // int.Parse tolerates whitespace, no need to parse around it.
    private static readonly Regex NonNegativeIntervalSplitter =
        new Regex(@"^([^-]+)-([^-]+)$", RegexOptions.Compiled);
}

/// <summary>
/// Random number generator of <see cref="System.UInt64"/> values.
/// </summary>
/// <remarks>
/// Supports sampling from arbitrary large closed intervals, including the
/// while range of <c>ulong</c>.
/// </remarks>
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

/// <summary>
/// <see cref="System.Random"/>-based random number generator of
/// <see cref="System.UInt64"/> values.
/// </summary>
internal sealed class FastLongRandom : LongRandom {
    internal FastLongRandom()
        : this(RandomNumberGenerator.GetInt32(int.MaxValue)) { }

    internal FastLongRandom(int seed) => _random = new Random(seed);

    internal override ulong Next(ulong max)
        => max < int.MaxValue ? (ulong)_random.Next((int)max + 1)
                              : base.Next(max);

    private protected override void NextBytes(byte[] buffer)
        => _random.NextBytes(buffer);

    private readonly Random _random;
}

/// <summary>
/// <see cref="System.Security.Cryptography.RandomNumberGenerator"/>-based
/// random number generator of <see cref="System.UInt64"/> values.
/// </summary>
internal sealed class GoodLongRandom : LongRandom {
    private protected override void NextBytes(byte[] buffer)
        => _random.GetBytes(buffer);

    private readonly RandomNumberGenerator _random =
        RandomNumberGenerator.Create();
}

/// <summary>Extension methods for <see cref="LongRandom"/>.</summary>
internal static class LongRandomExtensions {
    internal static int NextInt32(this LongRandom prng, int min, int max)
    {
        if (max < min) {
            throw new ArgumentOutOfRangeException(
                    paramName: nameof(min),
                    message: "can't sample from empty range");
        }

        var zeroBasedMax = (ulong)((long)max - (long)min);
        var value = (long)min + (long)prng.Next(zeroBasedMax);
        return (int)value;
    }
}

internal sealed class DistinctSampler {
    internal DistinctSampler(LongRandom prng, ulong upperExclusive)
        => (_prng, _size) = (prng, upperExclusive);

    internal ulong Next()
    {
        if (_size == 0)
            throw new InvalidOperationException("sample space exhausted");

        var key = _prng.Next(max: --_size);
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

/// <summary></summary>
internal readonly struct EdgeList {
    internal EdgeList(int order, int size, IEnumerable<Edge> edges)
        => (Order, Size, Edges) = (order, size, edges);

    internal void Deconstruct(out int order, out int size,
                              out IEnumerable<Edge> edges)
        => (order, size, edges) = (Order, Size, Edges);

    internal int Order { get; }

    // TODO: Should giant graphs, of over int.MaxValue edges, be supported?
    internal int Size { get; }

    internal IEnumerable<Edge> Edges { get; }

    private object ToDump() => new { Order, Size, Edges };
}

/// <summary>
/// Randomly generates a graph description from specified constraints.
/// </summary>
internal sealed class GraphGenerator {
    internal GraphGenerator(ClosedInterval orders,
                            ClosedInterval sizes,
                            ClosedInterval weights,
                            bool allowLoops,
                            bool allowParallelEdges,
                            bool uniqueWeights,
                            bool allowNegativeWeights,
                            LongRandom prng)
    {
        _orders = orders;
        _sizes = sizes;
        _weights = weights;
        _allowLoops = allowLoops;
        _allowParallelEdges = allowParallelEdges;
        _uniqueWeights = uniqueWeights;
        _allowNegativeWeights = allowNegativeWeights;
        _prng = prng;

        Error = CheckEachInterval()
             ?? CheckEachCardinality()
             ?? CheckSizeAgainstOrder()
             ?? CheckWeightRange();
    }

    internal string? Error { get; }

    // TODO: Figure out if this should use IObservable instead.
    internal EdgeList Generate()
    {
        if (Error != null) throw new InvalidOperationException(Error);

        var order = _prng.NextInt32(_orders.Min, _orders.Max);
        var size = _prng.NextInt32(_sizes.Min, ComputeMaxSize(order));
        return new EdgeList(order, size, EmitEdges(order, size));
    }

    private IEnumerable<Edge> EmitEdges(int order, int size)
    {
        Debug.Assert(order >= 0 && size >= 0);

        var nextEndpoints = CreateEndpointsGenerator(order);
        var nextWeight = CreateWeightGenerator();

        for (var i = 0; i < size; ++i) {
            var (src, dest) = nextEndpoints();
            yield return new Edge(src, dest, nextWeight());
        }
    }

    private Func<(int src, int dest)> CreateEndpointsGenerator(int order)
    {
        var decode = CreateEndpointsDecoder(order);
        var next = CreateEncodedEndpointsGenerator(order);
        return () => decode(next());
    }

    private Func<ulong> CreateEncodedEndpointsGenerator(int order)
    {
        var cardinality = (ulong)ComputeCompleteSize(order);

        if (_allowParallelEdges)
            return () => _prng.Next(max: cardinality - 1);

        return new DistinctSampler(_prng, upperExclusive: cardinality).Next;
    }

    private Func<ulong, (int src, int dest)>
    CreateEndpointsDecoder(int order)
    {
        var longOrder = (ulong)order;

        if (_allowLoops) {
            return encodedEndpoints => {
                var src = encodedEndpoints / longOrder;
                var dest = encodedEndpoints % longOrder;
                return (src: (int)src, dest: (int)dest);
            };
        }

        return encodedEndpoints => {
            var src = encodedEndpoints / (longOrder - 1);
            var dest = encodedEndpoints % (longOrder - 1);
            if (src <= dest) ++dest;
            return (src: (int)src, dest: (int)dest);
        };
    }

    private Func<int> CreateWeightGenerator()
    {
        var count = (ulong)_weights.Count;

        int Bias(ulong zeroBasedWeight)
            => (int)(_weights.Min + (long)zeroBasedWeight);

        if (_uniqueWeights) {
            var sampler = new DistinctSampler(_prng, upperExclusive: count);
            return () => Bias(sampler.Next());
        }

        return () => Bias(_prng.Next(max: count - 1));
    }

    private string? CheckEachInterval()
    {
        if (_orders.Count == 0)
            return "Range of orders contains no values";
        if (_sizes.Count == 0)
            return "Range of sizes contains no values";
        if (_weights.Count == 0)
            return "Range of weights contains no values";

        return null;
    }

    private string? CheckEachCardinality()
    {
        if (_orders.Min < 0) return "Order (vertex count) can't be negative";
        if (_sizes.Min < 0) return "Size (edge count) can't be negative";
        return null;
    }

    private string? CheckSizeAgainstOrder()
    {
        var maxSize = ComputeMaxSize(_orders.Max);

        if (_sizes.Min <= maxSize) return null;

        return (_orders.Max, maxSize) switch {
            (1, 1) => $"1 vertex allows only 1 edge",
            (1, _) => $"1 vertex allows only {maxSize} edges",
            (_, 1) => $"{_orders.Max} vertices allow only 1 edge", // Unused.
            (_, _) => $"{_orders.Max} vertices allow only {maxSize} edges"
        };
    }

    private string? CheckWeightRange()
    {
        if (!_allowNegativeWeights && _weights.Min < 0)
            return "Negative edge weights not supported";

        // If weights must be unique, ensure the *whole* size range is okay.
        if (_uniqueWeights && _weights.Count < _sizes.Max) {
            return (_sizes.Max, _weights.Count) switch {
                (1,         1) => $"1 edge but only 1 weight", // Unused.
                (1,     var w) => $"1 edge but only {w} weights", // Unused.
                (var n,     1) => $"{n} edges but only 1 weight",
                (var n, var w) => $"{n} edges but only {w} weights"
            };
        }

        return null;
    }

    private int ComputeMaxSize(int order)
    {
        if (order == 0) return 0;
        if (_allowParallelEdges) return _sizes.Max;
        return (int)Math.Min(_sizes.Max, ComputeCompleteSize(order));
    }

    private long ComputeCompleteSize(long order)
        => order * (_allowLoops ? order : order - 1);

    private readonly ClosedInterval _orders;
    private readonly ClosedInterval _sizes;
    private readonly ClosedInterval _weights;

    private readonly bool _allowLoops;
    private readonly bool _allowParallelEdges;
    private readonly bool _uniqueWeights;
    private readonly bool _allowNegativeWeights;

    private readonly LongRandom _prng;
}

/// <summary></summary>
internal sealed class GraphGeneratingEventArgs : EventArgs {
    internal GraphGeneratingEventArgs(int order, int size)
        => (Order, Size) = (order, size);

    internal int Order { get; }

    internal int Size { get; }
}

/// <summary></summary>
internal sealed class GraphGeneratedEventArgs : EventArgs {
    internal GraphGeneratedEventArgs(int order, int size,
                                     IReadOnlyList<Edge> edges)
        => (Order, Size, Edges) = (order, size, edges);

    internal int Order { get; }

    internal int Size { get; }

    internal IReadOnlyList<Edge> Edges { get; }
}

/// <summary></summary>
internal delegate void
GraphGeneratingEventHandler(object sender, GraphGeneratingEventArgs e);

/// <summary></summary>
internal delegate void
GraphGeneratedEventHandler(object sender, GraphGeneratedEventArgs e);

/// <summary>Graphical frontend for GraphGenerator.</summary>
internal sealed class GraphGeneratorDialog : WF.Form {
    internal GraphGeneratorDialog()
    {
        SetFormProperties();
        SubscribeFormEvents();
        SubscribeChildControlEvents();
        SetAllToolTips();
        AddChildControls();
    }

    internal void DisplayDialog()
        => RunOrBeginInvoke(delegate {
            if (Visible) Hide();
            Show();
            WindowState = WF.FormWindowState.Normal;
        });

    internal event GraphGeneratingEventHandler? Generating = null;

    internal event GraphGeneratedEventHandler Generated
    {
        add {
            if (value == null)
                throw new ArgumentNullException(paramName: nameof(value));

            bool wasNull;

            lock (_sinksLocker) {
                wasNull = _sinks == null;
                _sinks += value;
            }

            if (wasNull) RunOrBeginInvoke(InvalidateGenerator);
        }

        remove {
            bool becameNull;

            lock (_sinksLocker) {
                var wasNull = _sinks == null;
                _sinks -= value;
                becameNull = !wasNull && _sinks == null;
            }

            if (becameNull) RunOrBeginInvoke(InvalidateGenerator);
        }
    }

    private const double RegularOpacity = 0.9;

    private const double ReducedOpacity = 0.6;

    [DllImport("user32.dll")]
    private static extern bool HideCaret(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowCaret(IntPtr hWnd);

    private static void SubscribeNormalizer(WF.TextBox textBox,
                                            Func<string, string> normalizer)
        => textBox.LostFocus += delegate {
            var normalized = normalizer(textBox.Text);
            if (textBox.Text != normalized) textBox.Text = normalized;
        };

    private static string NormalizeAsValueOrClosedInterval(string text)
    {
        if (ParseValue(text) is int value) return value.ToString();

        if (ClosedInterval.ParseNonNegative(text) is ClosedInterval interval) {
            return interval.Min == interval.Max
                    ? interval.Min.ToString() // Collapse n-n to n.
                    : interval.ToString();
        }

        return text;
    }

    private static string NormalizeAsClosedInterval(string text)
    {
        if (ClosedInterval.ParseNonNegative(text) is ClosedInterval interval)
            return interval.ToString();

        if (ParseValue(text) is int value)
            return new ClosedInterval(value, value).ToString();

        return text;
    }

    private static int? ParseValue(string text)
        => int.TryParse(text, out var value) ? value : default(int?);

    private void SetFormProperties()
    {
        Text = "Graph Generator";
        Size = new Size(width: 300, height: 210);
        FormBorderStyle = WF.FormBorderStyle.Fixed3D;
        MaximizeBox = false;
        Opacity = RegularOpacity;
        KeyPreview = true;
    }

    private void SubscribeFormEvents()
    {
        Shown += GraphGeneratorDialog_FormShown;
        FormClosing += GraphGeneratorDialog_FormClosing;
        Move += delegate { Opacity = ReducedOpacity; };
        Resize += delegate { Opacity = RegularOpacity; };
        ResizeEnd += delegate { Opacity = RegularOpacity; };
        KeyDown += GraphGeneratorDialog_KeyDown;
    }

    private void SubscribeChildControlEvents()
    {
        _order.TextChanged += InvalidateGenerator;
        _size.TextChanged += InvalidateGenerator;
        _weights.TextChanged += InvalidateGenerator;
        _allowLoops.CheckedChanged += InvalidateGenerator;
        _allowParallelEdges.CheckedChanged += InvalidateGenerator;
        _uniqueEdgeWeights.CheckedChanged += InvalidateGenerator;
        _highQualityRandomness.CheckedChanged += InvalidateGenerator;

        SubscribeNormalizer(_order, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_size, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_weights, NormalizeAsClosedInterval);

        _status.GotFocus += status_GotFocus;
        _generate.Click += generate_Click;
        _cancel.Click += cancel_Click;
        _close.Click += delegate { Hide(); };
    }

    private void SetAllToolTips()
    {
        SetToolTips("number of vertices", _orderLabel, _order);
        SetToolTips("number of edges", _sizeLabel, _size);
        SetToolTips("range of possible edge weights", _weightsLabel, _weights);

        SetToolTip(_allowLoops,
                   "May the graph have self-edges, i.e., loops?\n"
                    + "A self-edge is an edge from a vertex to itself.");
        SetToolTip(_allowParallelEdges,
                   "May the graph have parallel edges?\n"
                    + "These are multiple edges from the same source\n"
                    + "vertex to the same destination vertex. Note that\n"
                    + "edges in opposite directions between the same\n"
                    + "vertices are always permitted.");
        SetToolTip(_uniqueEdgeWeights,
                   "Must no two edges have the same weight?");
        SetToolTip(_highQualityRandomness,
                   "slower but higher quality pseudorandom number generation");

        SetToolTip(_status, "status");
        SetToolTip(_generate,
                   "generate a random graph meeting these parameters");
        SetToolTip(_cancel, "cancel the current graph generation operation");
        SetToolTip(_close, "dismiss this dialog");
    }

    private void AddChildControls()
    {
        Controls.Add(_orderLabel);
        Controls.Add(_order);
        Controls.Add(_sizeLabel);
        Controls.Add(_size);
        Controls.Add(_weightsLabel);
        Controls.Add(_weights);
        Controls.Add(_allowLoops);
        Controls.Add(_allowParallelEdges);
        Controls.Add(_uniqueEdgeWeights);
        Controls.Add(_highQualityRandomness);
        Controls.Add(_status);
        Controls.Add(_generate);
        Controls.Add(_cancel);
        Controls.Add(_close);
    }

    private void GraphGeneratorDialog_FormShown(object? sender, EventArgs e)
    {
        if (!_formShownBefore) {
            _formShownBefore = true;
            ReadState();
        }
    }

    private void GraphGeneratorDialog_FormClosing(object sender,
                                                  WF.FormClosingEventArgs e)
    {
        if (e.CloseReason == WF.CloseReason.UserClosing) {
            Hide();
            e.Cancel = true;
        }
    }

    private void GraphGeneratorDialog_KeyDown(object sender, WF.KeyEventArgs e)
    {
        if (e.KeyCode == WF.Keys.F7) ToggleStatusCaretPreference();
    }

    private void status_GotFocus(object? sender, EventArgs e)
        => ApplyStatusCaretPreference();

    // TODO: Maybe break up this method somehow.
    private async void generate_Click(object? sender, EventArgs e)
    {
        ReadState(); // Use latest input even if it was given very strangely.
        var generator = _generator;

        if (generator == null || generator.Error != null) {
            Warn("Bug? \"Generate\" button enabled with unusable parameters.");
            return;
        }

        var sinks = _sinks;
        if (sinks == null) {
            Warn("Bug? \"Generate\" button enabled with no data sink.");
            return;
        }

        _working = true;
        KnobsEnabled = false;
        _generate.Text = "Working...";
        _generate.Enabled = false;
        var (order, size, edges) = generator.Generate();
        StatusWaiting($"Generating {order} vertices, {size} edges");
        _cancel.Enabled = true;
        try {
            await Task.Run(() => {
                Generating?.Invoke(this,
                                   new GraphGeneratingEventArgs(order, size));

                sinks(this, new GraphGeneratedEventArgs(order, size,
                                                        edges.ToList()));
            });
        } finally {
            _cancel.Text = "Cancel";
            _cancel.Enabled = false;
            _generate.Text = "Generate";
            KnobsEnabled = true;
            _working = false;
            ReadState();
        }
    }

    private void cancel_Click(object? sender, EventArgs e)
    {
        _cancel.Text = "Cancelling...";
        _cancel.Enabled = false;
        // FIXME: implement the actual cancellation logic!
    }

    private void SetToolTip(WF.Control control, string text)
        => _toolTip.SetToolTip(control,
                               text.Replace("\n", Environment.NewLine));

    private void SetToolTips(string text, params WF.Control[] controls)
        => Array.ForEach(controls, control => SetToolTip(control, text));

    private void InvalidateGenerator(object? sender, EventArgs e)
    {
        if (_formShownBefore) ReadState();
    }

    private void ReadState()
    {
        const string intOrRange = "must be an integer (or range)";
        const string rangeOrInt = "must be a range (or integer)";

        if (_working ||
                !(ReadClosedInterval(_order, _orderLabel, intOrRange)
                        is ClosedInterval orders
                  && ReadClosedInterval(_size, _sizeLabel, intOrRange)
                        is ClosedInterval sizes
                  && ReadClosedInterval(_weights, _weightsLabel, rangeOrInt)
                        is ClosedInterval weights)) {
            _generator = null;
            return;
        }

        _generator = new GraphGenerator(
                orders: orders,
                sizes: sizes,
                weights: weights,
                allowLoops: _allowLoops.Checked,
                allowParallelEdges: _allowParallelEdges.Checked,
                uniqueWeights: _uniqueEdgeWeights.Checked,
                allowNegativeWeights: false,
                prng: _highQualityRandomness.Checked ? _goodPrng : _fastPrng);

        if (_generator.Error != null)
            StatusError(_generator.Error);
        else if (_sinks == null)
            StatusWaiting("Data sink busy/unavailable");
        else
            StatusOk();
    }

    private ClosedInterval? ReadClosedInterval(WF.TextBox textBox,
                                               WF.Label label,
                                               string requirement)
    {
        var input = textBox.Text;

        if (ParseValue(input) is int value)
            return new ClosedInterval(value, value);

        if (ClosedInterval.Parse(input) is ClosedInterval interval)
            return interval;

        // FIXME: Interval notation with out-of-range numbers should also
        // probably report errors like "... cannot exceed ...".

        if (string.IsNullOrWhiteSpace(input))
            StatusError($"{label.Text} not specified");
        else if (!BigInteger.TryParse(input, out var bigValue))
            StatusError($"{label.Text} {requirement}");
        else if (bigValue.Sign == -1)
            StatusError($"{label.Text} is a huge negative number!");
        else
            StatusError($"{label.Text} cannot exceed {int.MaxValue}");

        return null;
    }

    private void StatusOk()
    {
        _status.ForeColor = Color.Green;
        _status.Text = "OK";
        SetStatusToolTip();
        _generate.Enabled = true;
    }

    private void StatusWaiting(string message)
    {
        _status.ForeColor = Color.Brown;
        _status.Text = message;
        SetStatusToolTip();
        _generate.Enabled = false;
    }

    private void StatusError(string message)
    {
        _status.ForeColor = Color.Red;
        _status.Text = message;
        SetStatusToolTip();
        _generate.Enabled = false;
    }

    private void SetStatusToolTip()
    {
        var caretHelp = (_wantStatusCaret ? "Press F7 to disable caret."
                                          : "Press F7 to enable caret.");

        SetToolTip(_status, $"status: {_status.Text}\n({caretHelp})");
    }

    private void ToggleStatusCaretPreference()
    {
        _wantStatusCaret = !_wantStatusCaret;
        SetStatusToolTip();
        if (_status.ContainsFocus) ApplyStatusCaretPreference();
    }

    private void ApplyStatusCaretPreference()
    {
        if (_haveStatusCaret == _wantStatusCaret) return;

        if (_wantStatusCaret) {
            if (ShowCaret(_status.Handle))
                _haveStatusCaret = true;
            else
                Warn("Failure showing generator status caret");
        } else {
            if (HideCaret(_status.Handle))
                _haveStatusCaret = false;
            else
                Warn("Failure hiding generator status caret");
        }
    }

    private IEnumerable<WF.Control> Knobs
    {
        get {
            yield return _order;
            yield return _size;
            yield return _weights;

            yield return _allowLoops;
            yield return _allowParallelEdges;
            yield return _uniqueEdgeWeights;
            yield return _highQualityRandomness;
        }
    }

    private bool KnobsEnabled
    {
        set {
            foreach (var control in Knobs) control.Enabled = value;
        }
    }

    private void Warn(string message)
        => message.Dump($"Warning ({nameof(GraphGeneratorDialog)})");

    private void RunOrBeginInvoke(EventHandler method)
    {
        // TODO: Investigate if our use cases ever trigger the race condition.
        if (IsHandleCreated)
            BeginInvoke(method);
        else
            method(this, EventArgs.Empty);
    }

    private readonly WF.Label _orderLabel = new WF.Label {
        Text = "Order",
        Location = new Point(x: 8, y: 17),
        Size = new Size(width: 45, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _order = new WF.TextBox {
        Text = "10",
        Location = new Point(x: 60, y: 13),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.Label _sizeLabel = new WF.Label {
        Text = "Size",
        Location = new Point(x: 8, y: 46),
        Size = new Size(width: 50, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _size = new WF.TextBox {
        Text = "25",
        Location = new Point(x: 60, y: 42),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.Label _weightsLabel = new WF.Label {
        Text = "Weights",
        Location = new Point(x: 8, y: 75),
        Size = new Size(width: 50, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _weights = new WF.TextBox {
        Text = "1-100",
        Location = new Point(x: 60, y: 71),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.CheckBox _allowLoops = new WF.CheckBox {
        Text = "allow loops",
        Location = new Point(x: 135, y: 12),
        Size = new Size(width: 140, height: 20),
        Checked = true,
    };

    private readonly WF.CheckBox _allowParallelEdges = new WF.CheckBox {
        Text = "allow parallel edges",
        Location = new Point(x: 135, y: 33),
        Size = new Size(width: 140, height: 20),
        Checked = true,
    };

    private readonly WF.CheckBox _uniqueEdgeWeights = new WF.CheckBox {
        Text = "unique edge weights",
        Location = new Point(x: 135, y: 54),
        Size = new Size(width: 140, height: 20),
        Checked = false,
    };

    private readonly WF.CheckBox _highQualityRandomness = new WF.CheckBox {
        Text = "high quality PRNG",
        Location = new Point(x: 135, y: 76),
        Size = new Size(width: 140, height: 20),
        Checked = false,
    };

    private readonly WF.TextBox _status = new WF.TextBox {
        Text = "Loading...",
        Location = new Point(x: 10, y: 102),
        Size = new Size(width: 250, height: 15),
        ReadOnly = true,
        BorderStyle = WF.BorderStyle.None,
        ForeColor = Color.Brown,
        BackColor = WF.Form.DefaultBackColor,
        Font = new Font(WF.TextBox.DefaultFont, FontStyle.Bold),
        Cursor = WF.Cursors.Arrow,
        //TabStop = false, // Avoid, as this worsens accessibility.
    };

    private readonly WF.Button _generate = new WF.Button {
        Text = "Generate",
        Location = new Point(x: 15, y: 125),
        Size = new Size(width: 80, height: 30),
        Enabled = false,
    };

    private readonly WF.Button _cancel = new WF.Button {
        Text = "Cancel",
        Location = new Point(x: 100, y: 125),
        Size = new Size(width: 80, height: 30),
        Enabled = false,
    };

    private readonly WF.Button _close = new WF.Button {
        Text = "Close",
        Location = new Point(x: 185, y: 125),
        Size = new Size(width: 80, height: 30),
    };

    private readonly WF.ToolTip _toolTip = new WF.ToolTip();

    private bool _formShownBefore = false;
    private bool _wantStatusCaret = false;
    private bool _haveStatusCaret = true;
    private bool _working = false;

    private readonly LongRandom _fastPrng = new FastLongRandom();
    private readonly LongRandom _goodPrng = new GoodLongRandom();

    private GraphGenerator? _generator = null;
    private GraphGeneratedEventHandler? _sinks = null;
    private readonly object _sinksLocker = new object();
}

/// <summary>
/// Test harness for <see cref="GraphGeneratorDialog"/> and
/// <see cref="GraphGenerator"/>.
/// </summary>
internal sealed class TestHarness {
    internal TestHarness()
    {
        _openGraphGenerator.Click += delegate { _dialog.DisplayDialog(); };
        _clearResults.Click += clearResults_Click;
        _toggleSubscription.Click += toggleSubscription_Click;
        _specifyBigTest.Click += specifyBigTest_Click;

        _panel = new LC.WrapPanel(_openGraphGenerator,
                                  _clearResults,
                                  _toggleSubscription,
                                  _specifyBigTest);
    }

    internal void Show(bool displayDialog)
    {
        _panel.Dump();
        if (displayDialog) _dialog.DisplayDialog();
    }

    private void clearResults_Click(object? sender, EventArgs e)
    {
        Util.ClearResults();
        _panel.Dump();
    }

    private void toggleSubscription_Click(object? sender, EventArgs e)
    {
        if (_subscribed) {
            _subscribed = false;
            _dialog.Generated -= dialog_Generated;
            _toggleSubscription.Text = "Subscribe";
        } else {
            _subscribed = true;
            _dialog.Generated += dialog_Generated;
            _toggleSubscription.Text = "Unsubscribe";
        }
    }

    private void specifyBigTest_Click(object? sender, EventArgs e)
        => _dialog.BeginInvoke(new WF.MethodInvoker(delegate {
            dynamic dialog = _dialog.Uncapsulate();

            dialog._order.Text = "1000";
            dialog._size.Text = "1000000";
            dialog._weights.Text = "1-1000000";

            dialog._allowLoops.Checked = true;
            dialog._allowParallelEdges.Checked = false;
            dialog._uniqueEdgeWeights.Checked = true;
            dialog._highQualityRandomness.Checked = true;
        }));

    private void dialog_Generated(object sender, GraphGeneratedEventArgs e)
        => new EdgeList(e.Order, e.Size, e.Edges).Dump(noTotals: true);

    private readonly GraphGeneratorDialog _dialog = new GraphGeneratorDialog();

    private readonly LC.Button _openGraphGenerator =
        new LC.Button("Open Graph Generator");

    private readonly LC.Button _clearResults = new LC.Button("Clear Results");

    private readonly LC.Button _toggleSubscription =
        new LC.Button("Subscribe");

    private readonly LC.Button _specifyBigTest =
        new LC.Button("Specify Big Test");

    private readonly LC.WrapPanel _panel;

    private bool _subscribed = false;
}

private static void Main() => new TestHarness().Show(displayDialog: true);
