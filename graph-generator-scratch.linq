<Query Kind="Program">
  <Namespace>LC = LINQPad.Controls</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

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
        => ParseViaSplitter(text, MaybeNegativeIntervalSplitter);

    internal static ClosedInterval? ParseNonNegative(string text)
        => ParseViaSplitter(text, NonNegativeIntervalSplitter);

    internal ClosedInterval(int start, int end)
        => (Start, End) = (start, end);

    public override string ToString() => $"{Start}-{End}";

    internal int Start { get; }

    internal int End { get; }

    private static ClosedInterval? ParseViaSplitter(string text, Regex splitter)
    {
        var match = splitter.Match(text);

        return match.Success && int.TryParse(match.Group(1), out var start)
                             && int.TryParse(match.Group(2), out var end)
            ? new ClosedInterval(start, end)
            : default(ClosedInterval?);
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
    internal sealed class Builder { // FIXME: Use a less awkward design.
        internal ClosedInterval Orders { set => _orders = value; }
        internal ClosedInterval Sizes { set => _sizes = value; }
        internal ClosedInterval Weights { set => _weights = value; }

        internal bool Loops { set => _loops = value; }
        internal bool ParallelEdges { set => _parallelEdges = value; }
        internal bool AllowNegativeWeights
            { set => _allowNegativeWeights = value; }

        internal bool HighQualityRandomness
        {
            set { } // FIXME: Assign appropriate generator delegate instance.
        }

        internal (GraphGenerator? generator, string? error) Build()
        {
            // FIXME: implement this
        }

        private ClosedInterval? _orders;
        private ClosedInterval? _sizes;
        private ClosedInterval? _weights;

        private bool? _loops;
        private bool? _parallelEdges;
        private bool? _allowNegativeWeights;

        private Func<int, int, int>? _prng;
    }

    private GraphGenerator(ClosedInterval orders,
                           ClosedInterval sizes,
                           ClosedInterval weights,
                           bool allowLoops,
                           bool allowParallelEdges,
                           Func<int, int, int> prng)
    {
        _orders = orders;
        _sizes = sizes;
        _weights = weights;

        _allowLoops = allowLoops;
        _allowParallelEdges = allowParallelEdges;

        _prng = prng;
    }

    private readonly ClosedInterval _orders;
    private readonly ClosedInterval _sizes;
    private readonly ClosedInterval _weights;

    private readonly bool _allowLoops;
    private readonly bool _allowParallelEdges;

    private readonly Func<int, int, int> _prng;
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

    private static void SubscribeNormalizer(WF.TextBox textBox,
                                            Func<string, string> normalizer)
        => textBox.LostFocus += delegate {
            textBox.Text = normalizer(textBox.Text);
        };

    private static string NormalizeAsValueOrClosedInterval(string text)
    {
        var value = ParseValue(text);
        if (value != null) return $"{value}";
        return NormalizeAsClosedInterval(text);
    }

    private static string AggressiveNormalizeAsClosedInterval(string text)
    {
        var value = ParseValue(text);
        if (value != null) return value < 0 ? $"{value}" : $"{value}-{value}";
        return NormalizeAsClosedInterval(text);
    }

    private static string NormalizeAsClosedInterval(string text)
    {
        var interval = ClosedInterval.ParseNonNegative(text);
        if (interval != null) return interval.Value.ToString();
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
    }

    private void SubscribeFormEvents()
    {
        Shown += GraphGeneratorDialog_FormShown;
        FormClosing += GraphGeneratorDialog_FormClosing;
        Move += delegate { Opacity = ReducedOpacity; };
        Resize += delegate { Opacity = RegularOpacity; };
        ResizeEnd += delegate { Opacity = RegularOpacity; };
    }

    private void SubscribeChildControlEvents()
    {
        _order.TextChanged += StateChanged;
        _size.TextChanged += StateChanged;
        _weights.TextChanged += StateChanged;
        _allowLoops.CheckedChanged += StateChanged;
        _allowParallelEdges.CheckedChanged += StateChanged;
        _highQualityRandomness.CheckedChanged += StateChanged;

        SubscribeNormalizer(_order, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_size, NormalizeAsValueOrClosedInterval);
        SubscribeNormalizer(_weights, AggressiveNormalizeAsClosedInterval);

        _status.GotFocus += delegate { HideCaret(_status.Handle); };
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

    private void generate_Click(object? sender, EventArgs e)
    {
        // FIXME: actually implement this
        WF.MessageBox.Show("Hello, world!");
    }

    private void cancel_Click(object? sender, EventArgs e)
    {
        // FIXME: implement this
    }

    private void SetToolTip(WF.Control control, string text)
        => _toolTip.SetToolTip(control, text);

    private void SetToolTips(string text, params WF.Control[] controls)
        => Array.ForEach(controls, control => SetToolTip(control, text));

    private void StateChanged(object? sender, EventArgs e)
    {
        if (_formShownBefore) ReadState();
    }

    private GraphGenerator? ReadState()
    {
        // (1) Text must represent a single value or a range.
        // (2) The lower bound of range must not exceed the upper.
        // (3) No numbers may be negative (but for different reasons).

        var orders = ReadClosedInterval(_orderLabel, _order);
        if (orders == null) return null;

        var sizes = ReadClosedInterval(_sizeLabel, _size);
        if (sizes == null) return null;

        var weights = ReadClosedInterval(_weightsLabel, _weights);
        if (weights == null) return null;

        // (4) A graph of order 0 must also be of size 0.
        // (5) If parallel edges are disallowed but loops are allowed, the
        //     maximum order must not exceed the square of the size.
        // (6) If parallel edges are disallowed and loops are disallowed, the
        //     maximum order must be strictly less than the square of the size.
        // [For (5) and (6), "maximum order" means the single value given for
        //  order or, if a range is given, the upper bound of the range.]

        var (generator, error) = new GraphGenerator.Builder {
            Orders = orders.Value,
            Sizes = sizes.Value,
            Weights = weights.Value,
            Loops = _allowLoops.Checked,
            ParallelEdges = _allowParallelEdges.Checked,
            HighQualityRandomness = _highQualityRandomness.Checked,
            AllowNegativeWeights = false
        }.Build();

        if (error == null)
            StatusOk();
        else
            StatusError(error);

        return generator;
    }

    private ClosedInterval? ReadClosedInterval(WF.Label label,
                                               WF.TextBox textBox)
    {
        var singleton = ParseValue(textBox.Text);

        if (singleton != null)
            return new ClosedInterval(singleton.Value, singleton.Value);

        var interval = ClosedInterval.Parse(textBox.Text);

        if (interval == null)
            StatusError($"{label} must be an integer value or range");

        return interval;
    }

    private void StatusOk()
    {
        _status.ForeColor = Color.Green;
        _status.Text = "OK";
    }

    private void StatusError(string message)
    {
        _status.ForeColor = Color.Red;
        _status.Text = message;
    }

    private readonly WF.Label _orderLabel = new WF.Label {
        Text = "Order",
        Location = new Point(x: 8, y: 17),
        Size = new Size(width: 45, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _order = new WF.TextBox {
        Text = "10",
        Location = new Point(x: 55, y: 13),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.Label _sizeLabel = new WF.Label {
        Text = "Size",
        Location = new Point(x: 8, y: 46),
        Size = new Size(width: 45, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _size = new WF.TextBox {
        Text = "25",
        Location = new Point(x: 55, y: 42),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.Label _weightsLabel = new WF.Label {
        Text = "Weights",
        Location = new Point(x: 8, y: 75),
        Size = new Size(width: 45, height: 15),
        TextAlign = ContentAlignment.MiddleCenter,
    };

    private readonly WF.TextBox _weights = new WF.TextBox {
        Text = "1-100",
        Location = new Point(x: 55, y: 71),
        Size = new Size(width: 60, height: 15),
    };

    private readonly WF.CheckBox _allowLoops = new WF.CheckBox {
        Text = "allow loops",
        Location = new Point(x: 135, y: 13),
        Size = new Size(width: 130, height: 20),
        Checked = true,
    };

    private readonly WF.CheckBox _allowParallelEdges = new WF.CheckBox {
        Text = "allow parallel edges",
        Location = new Point(x: 135, y: 42),
        Size = new Size(width: 130, height: 20),
        Checked = true,
    };

    private readonly WF.CheckBox _highQualityRandomness = new WF.CheckBox {
        Text = "high quality PRNG",
        Location = new Point(x: 135, y: 71),
        Size = new Size(width: 130, height: 20),
        Checked = false,
    };

    private readonly WF.TextBox _status = new WF.TextBox {
        Text = "Loading...",
        Location = new Point(x: 15, y: 102),
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
}

private static void Main()
{
    var dialog = new GraphGeneratorDialog();
    new LC.Button("Generate...", delegate { dialog.DisplayDialog(); }).Dump();
    dialog.DisplayDialog();
}
