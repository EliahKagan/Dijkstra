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

internal abstract class LongRandom {
    static LongRandom()
        => Debug.Assert(1 << ShiftCount == BufferSize * BitsPerByte);

    internal ulong Next(ulong max)
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

internal sealed class FastLongRandom : LongRandom {
    private protected override void NextBytes(byte[] buffer)
        => _random.NextBytes(buffer);

    private readonly Random _random =
        new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
}

internal sealed class GoodLongRandom : LongRandom {
    private protected override void NextBytes(byte[] buffer)
        => _random.GetBytes(buffer);

    private readonly RandomNumberGenerator _random =
        RandomNumberGenerator.Create();
}

/// <summary>Extensions for clearer and more complex regex usage.</summary>
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

/// <summary></summary>
internal readonly struct LazyGraph {
    internal LazyGraph(int order, int size, IEnumerable<Edge> edges)
        => (Order, Size, Edges) = (order, size, edges);

    internal int Order { get; }

    // TODO: Should giant graphs, of over int.MaxValue edges, be supported?
    internal int Size { get; }

    internal IEnumerable<Edge> Edges { get; }
}

/// <summary>
/// Randomly generates a graph description from specified constraints.
/// </summary>
// FIXME: It seems like this should be a reference type. Figure that out.
internal readonly struct GraphGenerator {
    internal GraphGenerator(ClosedInterval orders,
                            ClosedInterval sizes,
                            ClosedInterval weights,
                            bool allowLoops,
                            bool allowParallelEdges,
                            bool uniqueWeights,
                            bool allowNegativeWeights)
    {
        _orders = orders;
        _sizes = sizes;
        _weights = weights;
        _allowLoops = allowLoops;
        _allowParallelEdges = allowParallelEdges;
        _uniqueWeights = uniqueWeights;
        _allowNegativeWeights = allowNegativeWeights;

        // Assign a dummy value, so the Check... methods can be called.
        Error = $"Bug: {nameof(GraphGenerator)} not fully constructed";

        Error = CheckEachInterval()
             ?? CheckEachCardinality()
             ?? CheckSizeAgainstOrder()
             ?? CheckWeightRange();
    }

    internal string? Error { get; }

    internal LazyGraph Generate(LongRandom prng)
    {
        if (Error != null) throw new InvalidOperationException(Error);

        // FIXME: implement this!
        Thread.Sleep(2000);
        throw new NotImplementedException();
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
        if (MaxSize < _sizes.Min) { // Note: This is a *lifted* < comparison.
            return (_orders.Max, MaxSize) switch {
                (1,         1) => $"1 vertex allows only 1 edge",
                (1,     var m) => $"1 vertex allows only {m} edges",
                (var n,     1) => $"{n} vertices allow only 1 edge", // Unused.
                (var n, var m) => $"{n} vertices allow only {m} edges"
            };
        }

        return null;
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

    private long? MaxSize
    {
        get {
            if (_orders.Max == 0) return 0;
            if (_allowParallelEdges) return null;
            var maxOrder = (long)_orders.Max;
            return maxOrder * (_allowLoops ? maxOrder : maxOrder - 1);
        }
    }

    private readonly ClosedInterval _orders;
    private readonly ClosedInterval _sizes;
    private readonly ClosedInterval _weights;

    private readonly bool _allowLoops;
    private readonly bool _allowParallelEdges;
    private readonly bool _uniqueWeights;
    private readonly bool _allowNegativeWeights;
}

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
    {
        if (Visible) Hide();
        Show();
        WindowState = WF.FormWindowState.Normal;
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
            textBox.Text = normalizer(textBox.Text);
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
        _order.TextChanged += StateChanged;
        _size.TextChanged += StateChanged;
        _weights.TextChanged += StateChanged;
        _allowLoops.CheckedChanged += StateChanged;
        _allowParallelEdges.CheckedChanged += StateChanged;
        _uniqueEdgeWeights.CheckedChanged += StateChanged;
        _highQualityRandomness.CheckedChanged += StateChanged;

        SubscribeNormalizer(_order, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_size, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_weights, NormalizeAsClosedInterval);

        _status.GotFocus += status_GotFocus;
        _status.KeyDown += status_KeyDown;
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
        if (e.KeyCode == WF.Keys.F7) {
            _wantStatusCaret = !_wantStatusCaret;
            SetStatusToolTip();
        }
    }

    private void status_GotFocus(object? sender, EventArgs e)
        => ApplyStatusCaretPreference();

    private void status_KeyDown(object sender, WF.KeyEventArgs e)
    {
        if (e.KeyCode == WF.Keys.F7) ApplyStatusCaretPreference();
    }

    private async void generate_Click(object? sender, EventArgs e)
    {
        if (!(ReadState() is GraphGenerator generator)) {
            Warn("\"Generate\" button enabled with unusable parameters");
            return;
        }

        TurnKnobsOff();
        _generate.Text = "Generating...";
        _generate.Enabled = false;
        _cancel.Enabled = true;
        try {
            var prng = GetPrng();

            // FIXME: This needs to actually get the results!
            await Task.Run(() => generator.Generate(prng));
        } finally {
            _cancel.Text = "Cancel";
            _cancel.Enabled = false;
            _generate.Enabled = true;
            _generate.Text = "Generate";
            TurnKnobsOn();
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

    private void StateChanged(object? sender, EventArgs e)
    {
        if (_formShownBefore) ReadState();
    }

    private GraphGenerator? ReadState()
    {
        const string integerOrRange = "must be an integer (or range)";
        const string rangeOrInteger = "must be a range (or integer)";

        if (!(ReadClosedInterval(_order, _orderLabel, integerOrRange)
                        is ClosedInterval orders
                && ReadClosedInterval(_size, _sizeLabel, integerOrRange)
                        is ClosedInterval sizes
                && ReadClosedInterval(_weights, _weightsLabel, rangeOrInteger)
                        is ClosedInterval weights))
            return null;

        var generator = new GraphGenerator(
                orders: orders,
                sizes: sizes,
                weights: weights,
                allowLoops: _allowLoops.Checked,
                allowParallelEdges: _allowParallelEdges.Checked,
                uniqueWeights: _uniqueEdgeWeights.Checked,
                allowNegativeWeights: false);

        if (generator.Error == null)
            StatusOk();
        else
            StatusError(generator.Error);

        return generator;
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

    void TurnKnobsOff()
    {
        foreach (var control in Knobs) control.Enabled = false;
    }

    void TurnKnobsOn()
    {
        foreach (var control in Knobs) control.Enabled = true;
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

    private LongRandom GetPrng()
    {
        if (_highQualityRandomness.Checked)
            return _goodPrng ??= new GoodLongRandom();
        else
            return _fastPrng ??= new FastLongRandom();
    }

    private void Warn(string message)
        => message.Dump($"Warning ({nameof(GraphGeneratorDialog)})");

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

    private FastLongRandom? _fastPrng = null;
    private GoodLongRandom? _goodPrng = null;
}

/// <summary>
/// Test harness for <see cref="GraphGeneratorDialog"/> and
/// <see cref="GraphGenerator"/>.
/// </summary>
private static void Main()
{
    var dialog = new GraphGeneratorDialog();

    new LC.Button("Open Graph Generator...", delegate {
        dialog.DisplayDialog();
    }).Dump();

    dialog.DisplayDialog();
}
