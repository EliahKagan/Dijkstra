<Query Kind="Program">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

internal sealed class SmallLayoutExperiment : Form {
    internal SmallLayoutExperiment()
    {
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Text = "small layout experiment";

        _upper.Controls.Add(_upperLeft);
        _upper.Controls.Add(_upperRight);

        _lower.Controls.Add(_lowerLeft);
        _lower.Controls.Add(_lowerRight);

        _all.Controls.Add(_upper);
        _all.Controls.Add(_lower);

        Controls.Add(_all);

        ResumeLayout();
    }

    private readonly FlowLayoutPanel _all = new FlowLayoutPanel {
        FlowDirection = FlowDirection.TopDown,
        AutoSize = true,
        WrapContents = false,
    };

    private readonly FlowLayoutPanel _upper = new FlowLayoutPanel {
        FlowDirection = FlowDirection.LeftToRight,
        AutoSize = true,
        WrapContents = false,
    };

    private readonly Button _upperLeft = new Button {
        Text = "Upper Left",
        AutoSize = true,
    };

    private readonly Button _upperRight = new Button {
        Text = "Upper Right",
        AutoSize = true,
    };

    private readonly FlowLayoutPanel _lower = new FlowLayoutPanel {
        FlowDirection = FlowDirection.LeftToRight,
        AutoSize = true,
        WrapContents = false,
    };

    private readonly Button _lowerLeft = new Button {
        Text = "Lower Left",
        AutoSize = true,
    };

    private readonly Button _lowerRight = new Button {
        Text = "Lower Right",
        AutoSize = true,
    };
}

[STAThread]
private static void Main() => Application.Run(new SmallLayoutExperiment());
