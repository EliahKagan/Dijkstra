<Query Kind="Program">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

internal sealed class LayoutExperiment : Form {
    internal LayoutExperiment()
    {
        SuspendLayout();

        SetFormProperties();
        _close.Click += delegate { Close(); };
        AddChildControls();

        ResumeLayout();
    }

    private void SetFormProperties()
    {
        Text = "Graph Gen'r mock";
        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Size = new Size(0, 0);
        FormBorderStyle = FormBorderStyle.Fixed3D;
        MaximizeBox = false;
    }

    private void AddChildControls()
    {
        _labeledTextBoxes.Controls.Add(_orderLabel);
        _labeledTextBoxes.Controls.Add(_order);
        _labeledTextBoxes.Controls.Add(_sizeLabel);
        _labeledTextBoxes.Controls.Add(_size);
        _labeledTextBoxes.Controls.Add(_weightsLabel);
        _labeledTextBoxes.Controls.Add(_weights);

        _checkBoxes.Controls.Add(_allowLoops);
        _checkBoxes.Controls.Add(_allowParallelEdges);
        _checkBoxes.Controls.Add(_uniqueEdgeWeights);
        _checkBoxes.Controls.Add(_highQualityPrng);

        _inputs.Controls.Add(_labeledTextBoxes);
        _inputs.Controls.Add(_checkBoxes);

        _buttons.Controls.Add(_generate);
        _buttons.Controls.Add(_cancel);
        _buttons.Controls.Add(_close);

        _all.Controls.Add(_inputs);
        _all.Controls.Add(_status);
        _all.Controls.Add(_buttons);

        Controls.Add(_all);
    }

    private readonly FlowLayoutPanel _all = new FlowLayoutPanel {
        FlowDirection = FlowDirection.TopDown,
        AutoSize = true,
        WrapContents = false,
        Margin = new Padding(left: 3, top: 3, right: 0, bottom: 3),
    };

    private readonly FlowLayoutPanel _inputs = new FlowLayoutPanel {
        FlowDirection = FlowDirection.LeftToRight,
        AutoSize = true,
        WrapContents = false,
        Margin = new Padding(left: 3, top: 3, right: 3, bottom: 0),
    };

    private readonly TableLayoutPanel _labeledTextBoxes = new TableLayoutPanel {
        RowCount = 3,
        ColumnCount = 2,
        GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
        AutoSize = true,
        Margin = new Padding(left: 1, top: 3, right: 4, bottom: 3),
    };

    private readonly Label _orderLabel = new Label {
        Text = "Order",
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        TextAlign = ContentAlignment.BottomRight,
        Margin = new Padding(left: 3, top: 3, right: 0, bottom: 3),
    };

    private readonly TextBox _order = new TextBox {
        Text = "10",
        Size = new Size(width: 60, height: 15),
        AutoSize = true,
    };

    private readonly Label _sizeLabel = new Label {
        Text = "Size",
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        TextAlign = ContentAlignment.BottomRight,
        Margin = new Padding(left: 3, top: 3, right: 0, bottom: 3),
    };

    private readonly TextBox _size = new TextBox {
        Text = "25",
        Size = new Size(width: 60, height: 15),
        AutoSize = true,
    };

    private readonly Label _weightsLabel = new Label {
        Text = "Weights",
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        TextAlign = ContentAlignment.BottomRight,
        Margin = new Padding(left: 3, top: 3, right: 0, bottom: 3),
    };

    private readonly TextBox _weights = new TextBox {
        Text = "1-100",
        Size = new Size(width: 60, height: 15),
        AutoSize = true,
    };

    private readonly FlowLayoutPanel _checkBoxes = new FlowLayoutPanel {
        FlowDirection = FlowDirection.TopDown,
        AutoSize = true,
        WrapContents = false,
        Margin = new Padding(left: 4, top: 3, right: 3, bottom: 3),
    };

    private readonly CheckBox _allowLoops = new CheckBox {
        Text = "allow loops",
        AutoSize = true,
        Margin = new Padding(left: 3, top: 2, right: 3, bottom: 1),
        Checked = true,
    };

    private readonly CheckBox _allowParallelEdges = new CheckBox {
        Text = "allow parallel edges",
        AutoSize = true,
        Margin = new Padding(left: 3, top: 2, right: 3, bottom: 1),
        Checked = true,
    };

    private readonly CheckBox _uniqueEdgeWeights = new CheckBox {
        Text = "unique edge weights",
        AutoSize = true,
        Margin = new Padding(left: 3, top: 2, right: 3, bottom: 1),
        Checked = false,
    };

    private readonly CheckBox _highQualityPrng = new CheckBox {
        Text = "high quality PRNG",
        AutoSize = true,
        Margin = new Padding(left: 3, top: 2, right: 3, bottom: 1),
        Checked = false,
    };

    private readonly TextBox _status = new TextBox {
        Text = "Status text goes here in this place yes and it is wide"
             + " to see what happens when it is very very wide",
        AutoSize = true,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(left: 10, top: 1, right: 4, bottom: 1),
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        ForeColor = Color.Brown,
        BackColor = Form.DefaultBackColor,
        Font = new Font(TextBox.DefaultFont, FontStyle.Bold),
        Cursor = Cursors.Arrow,
    };

    private readonly FlowLayoutPanel _buttons = new FlowLayoutPanel {
        FlowDirection = FlowDirection.LeftToRight,
        AutoSize = true,
        WrapContents = false,
        Padding = new Padding(left: 10, top: 3, right: 10, bottom: 3),
    };

    private readonly Button _generate = new Button {
        Text = "Generate",
        //AutoSize = true,
        Size = new Size(width: 80, height: 30),
    };

    private readonly Button _cancel = new Button {
        Text = "Cancel",
        //AutoSize = true,
        Size = new Size(width: 80, height: 30),
        Enabled = false,
    };

    private readonly Button _close = new Button {
        Text = "Close",
        //AutoSize = true,
        Size = new Size(width: 80, height: 30),
        //Anchor = AnchorStyles.Right,
    };
}

[STAThread]
private static void Main() => Application.Run(new LayoutExperiment());
