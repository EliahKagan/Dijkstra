<Query Kind="Program">
  <Namespace>LC = LINQPad.Controls</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

internal sealed class GraphGeneratorDialog : WF.Form {
    internal GraphGeneratorDialog()
    {
        Opacity = 0.9;

        FormClosing += GraphGeneratorDialog_FormClosing;

        _generate.Click += delegate {
            WF.MessageBox.Show("Hello, world!");
        };

        _close.Click += delegate { Hide(); };

        Controls.Add(_orderLabel);
        Controls.Add(_order);
        Controls.Add(_sizeLabel);
        Controls.Add(_size);
        Controls.Add(_generate);
        Controls.Add(_close);
    }

    internal void DisplayDialog()
    {
        if (Visible) Hide();
        Show();
        WindowState = WF.FormWindowState.Normal;
    }

    private void GraphGeneratorDialog_FormClosing(object sender,
                                                  WF.FormClosingEventArgs e)
    {
        if (e.CloseReason == WF.CloseReason.UserClosing) {
            Hide();
            e.Cancel = true;
        }
    }

    private readonly WF.Label _orderLabel = new WF.Label {
        Size = new Size(40, 15),
        Location = new Point(15, 15),
        Text = "Order"
    };

    private readonly WF.TextBox _order = new WF.TextBox {
        Size = new Size(60, 15),
        Location = new Point(60, 13),
        Text = "10"
    };

    private readonly WF.Label _sizeLabel = new WF.Label {
        Size = new Size(30, 15),
        Location = new Point(165, 15),
        Text = "Size"
    };

    private readonly WF.TextBox _size = new WF.TextBox {
        Size = new Size(60, 15),
        Location = new Point(200, 13),
        Text = "25"
    };

    private readonly WF.Button _generate = new WF.Button {
        Size = new Size(80, 30),
        Location = new Point(30, 100),
        Text = "Generate"
    };

    private readonly WF.Button _close = new WF.Button {
        Size = new Size(80, 30),
        Location = new Point(175, 100),
        Text = "Close"
    };
}

//[STAThread]
private static void Main()
{
    //WF.Application.EnableVisualStyles();

    var dialog = new GraphGeneratorDialog();

    new LC.Button("Generate...", delegate { dialog.DisplayDialog(); }).Dump();

    dialog.DisplayDialog();

    //new LC.Button("Generate...", delegate {
    //    if (dialog.Visible) dialog.Hide();
    //    dialog.Show();
    //    dialog.WindowState = WF.FormWindowState.Normal;
    //}).Dump();
    //dialog.Show();

    //WF.Application.Run(new GraphGeneratorDialog());
    //WF.MessageBox.Show("Done");
}
