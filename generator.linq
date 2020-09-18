<Query Kind="Program">
  <Namespace>LC = LINQPad.Controls</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Numerics</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

#load "./helpers.linq"

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
        new Regex(@"^\s*(-?[^-\s]+)\s*-\s*(-?[^-\s]+)\s*$");

    private static readonly Regex NonNegativeIntervalSplitter =
        new Regex(@"^([^-]+)-([^-]+)$"); // OK, int.Parse tolerates whitespace.
}

/// <summary>
/// Randomly generates a graph description from specified constraints.
/// </summary>
internal sealed class GraphGenerator {
    internal ClosedInterval Orders
    {
        set => Set(out _orders, value);
    }

    internal ClosedInterval Sizes
    {
        set => Set(out _sizes, value);
    }

    internal ClosedInterval Weights
    {
        set => Set(out _weights, value);
    }

    internal bool AllowLoops
    {
        set => Set(out _allowLoops, value);
    }

    internal bool AllowParallelEdges
    {
        set => Set(out _allowParallelEdges, value);
    }

    internal bool UniqueWeights
    {
        set => Set(out _uniqueWeights, value);
    }

    internal bool AllowNegativeWeights
    {
        set => Set(out _allowNegativeWeights, value);
    }

    internal string? Error
    {
        get {
            Check();
            return _error;
        }
    }

    // FIXME: Maybe lock in values once set to avoid race conditions?

    internal IEnumerable<Edge> Generate(Func<int, int, int> prng)
    {
        // FIXME: implement this!
        throw new NotImplementedException();
    }

    private void Set<T>(out T field, T value)
    {
        field = value;
        _checked = false;
    }

    private void Check()
    {
        if (_checked) return;

        _error = CheckEachInterval()
              ?? CheckEachCardinality()
              ?? CheckSizeAgainstOrder()
              ?? CheckWeightRange();

        _checked = true;
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
                (1, 1)         => $"1 vertex allows only 1 edge",
                (1, var m)     => $"1 vertex allows only {m} edges",
                (var n, 1)     => $"{n} vertices allow only 1 edge", // Unused.
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
                (1, 1) => $"1 edge but only 1 weight", // Unused.
                (1, var w) => $"1 edge but only {w} weights", // Unused.
                (var n, 1) => $"{n} edges but only 1 weight",
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

    private ClosedInterval _orders;
    private ClosedInterval _sizes;
    private ClosedInterval _weights;

    private bool _allowLoops;
    private bool _allowParallelEdges;
    private bool _uniqueWeights;
    private bool _allowNegativeWeights;

    private bool _checked = false;
    private string? _error = null;
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
        var value = ParseValue(text);
        if (value != null) return value.Value.ToString();

        var interval = ClosedInterval.ParseNonNegative(text);
        if (interval == null) return text;

        return interval.Value.Min == interval.Value.Max
                ? interval.Value.Min.ToString() // Collapse n-n to n.
                : interval.Value.ToString();
    }

    private static string NormalizeAsClosedInterval(string text)
    {
        var interval = ClosedInterval.ParseNonNegative(text);
        if (interval != null) return interval.Value.ToString();

        var value = ParseValue(text);
        if (value == null) return text;

        // Expand n to n-n.
        return new ClosedInterval(value.Value, value.Value).ToString();
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

    private void generate_Click(object? sender, EventArgs e)
    {
        // FIXME: actually implement this
        WF.MessageBox.Show("Hello, world!");
    }

    private void cancel_Click(object? sender, EventArgs e)
    {
        // FIXME: implement the actual cancellation logic!

        _cancel.Enabled = false;
        ReadState(); // TODO: Do I want this, or a cancellation notice?
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

        var orders = ReadClosedInterval(_order, _orderLabel, integerOrRange);
        if (orders == null) return null;

        var sizes = ReadClosedInterval(_size, _sizeLabel, integerOrRange);
        if (sizes == null) return null;

        var weights = ReadClosedInterval(_weights, _weightsLabel, rangeOrInteger);
        if (weights == null) return null;

        var gen = new GraphGenerator {
            Orders = orders.Value,
            Sizes = sizes.Value,
            Weights = weights.Value,
            AllowLoops = _allowLoops.Checked,
            AllowParallelEdges = _allowParallelEdges.Checked,
            UniqueWeights = _uniqueEdgeWeights.Checked,
            AllowNegativeWeights = false
        };

        if (gen.Error == null)
            StatusOk();
        else
            StatusError(gen.Error);

        return gen;
    }

    private ClosedInterval? ReadClosedInterval(WF.TextBox textBox,
                                               WF.Label label,
                                               string requirement)
    {
        var input = textBox.Text;

        var singleton = ParseValue(input);
        if (singleton != null)
            return new ClosedInterval(singleton.Value, singleton.Value);

        var interval = ClosedInterval.Parse(input);
        if (interval != null) return interval;

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
        if (!_cancel.Enabled) _generate.Enabled = true;
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
                "Failure showing generator status caret".Dump();
        } else {
            if (HideCaret(_status.Handle))
                _haveStatusCaret = false;
            else
                "Failure hiding generator status caret".Dump();
        }
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
}

/// <summary>
/// Test harness for <see cref="GraphGeneratorDialog"/> and
/// <see cref="GraphGenerator"/>.
/// </summary>
private static void Main()
{
    var dialog = new GraphGeneratorDialog();
    new LC.Button("Generate...", delegate { dialog.DisplayDialog(); }).Dump();
    dialog.DisplayDialog();
}
