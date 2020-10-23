<Query Kind="Program">
  <Namespace>LC = LINQPad.Controls</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

internal sealed class SeparateWindow : WF.Form {
    internal SeparateWindow()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = WF.AutoScaleMode.Font;
        AutoSize = true;
        Size = new Size(0, 0);
        TopMost = true;
        Opacity = 0.9;

        _all.Controls.Add(_separateBufferLabel);
        _all.Controls.Add(_separateBuffer);
        Controls.Add(_all);

        Shown += SeparateWindow_Shown;
        FormClosing += SeparateWindow_FormClosing;

        ResumeLayout();
    }

    private void SeparateWindow_Shown(object? sender, EventArgs e)
        => Text = "Separate Window, Thread "
                    + Thread.CurrentThread.ManagedThreadId;

    private void SeparateWindow_FormClosing(object sender,
                                            WF.FormClosingEventArgs e)
    {
        if (e.CloseReason == WF.CloseReason.UserClosing) {
            Hide();
            e.Cancel = true;
        }
    }

    private readonly WF.FlowLayoutPanel _all = new WF.FlowLayoutPanel {
        FlowDirection = WF.FlowDirection.TopDown,
        AutoSize = true,
        WrapContents = false,
    };

    private readonly WF.Label _separateBufferLabel = new WF.Label {
        Text = "OK, see if you can type in HERE while a thing is being done.",
        Font = new Font(WF.Label.DefaultFont.FontFamily, 12, FontStyle.Bold),
        AutoSize = true,
    };

    private readonly WF.TextBox _separateBuffer = new WF.TextBox {
        AutoSize = true,
        Multiline = true,
        Height = 250,
        Width = 460,
    };
}

internal sealed class DumpTriggeredAction {
    internal DumpTriggeredAction(Action action) => _action = action;

    private object ToDump()
    {
        _action();
        return "Did action.";
    }

    private readonly Action _action;
}

internal sealed class DumpTriggeredAsyncAction {
    internal DumpTriggeredAsyncAction(Action action) => _action = action;

    private object ToDump() => Task.Run(_action);

    private readonly Action _action;
}

internal static class Program {
    private static void DoThing()
    {
        var ti = Stopwatch.StartNew();
        while (ti.ElapsedMilliseconds < 5000) { }
    }

    private static void doSynchronousThingDirectly_Click(LC.Button sender)
    {
        sender.Enabled = false;
        sender.Text = "Doing synchronous thing directly...";
        try {
            DoThing();
        } finally {
            sender.Text = "Redo Synchronous Thing Directly";
            sender.Enabled = true;
        }
    }

    private static void doSynchronousThingViaDump_Click(LC.Button sender)
    {
        sender.Enabled = false;
        sender.Text = "Doing sychronous thing via dump...";
        try {
            new DumpTriggeredAction(DoThing)
                .Dump("Synchronous dump-triggered action");
        } finally {
            sender.Text = "Redo Synchronous Thing Via Dump";
            sender.Enabled = true;
        }
    }

    private static async void
    doAsynchronousThingDirectly_Click(LC.Button sender)
    {
        sender.Enabled = false;
        sender.Text = "Doing asynchronous thing directly...";
        try {
            await Task.Run(DoThing);
        } finally {
            sender.Text = "Redo Asynchronous Thing Directly";
            sender.Enabled = true;
        }
    }

    private static void doAsynchronousThingViaDump_Click(LC.Button sender)
    {
        sender.Enabled = false;
        sender.Text = "Doing asynchronous thing via dump...";
        try {
            // FIXME: This is wrong (and thus finishes too quickly).
            new DumpTriggeredAsyncAction(DoThing)
                .Dump("Asynchronous dump-triggered action");
        } finally {
            sender.Text = "Redo Asynchronous Thing Via Dump";
            sender.Enabled = true;
        }
    }

    private static string MaybePlural(int count, string regularNoun)
        => count == 1 ? $"{count} {regularNoun}" : $"{count} {regularNoun}s";

    private static void Main()
    {
        var dumpLinqPadUiThreadId =
            new LC.Button(
                "Dump LINQPad UI Thread ID",
                delegate {
                    Thread.CurrentThread.ManagedThreadId
                        .Dump("LINQPad UI Thread ID");
                });

        var dumpThreadIdOfAMultithreadedControlsHandlerThread =
            new LC.Button(
                "Dump Thread ID of a Multithreaded Control's Handler Thread",
                delegate {
                    Thread.CurrentThread.ManagedThreadId
                        .Dump("A multithreaded control's handler thread ID");
                }) { IsMultithreaded = true };

        var threadIdDumpers =
            new LC.StackPanel(
                    horizontal: true,
                    dumpLinqPadUiThreadId,
                    dumpThreadIdOfAMultithreadedControlsHandlerThread);

        var staticBuffer = new LC.TextArea();

        var dynamicBufferStats =
            new LC.Label("(Start typing to see statistics.)");

        var dynamicBuffer = new LC.TextArea(onTextInput: sender => {
            //$"[{sender.Text}]".Dump();
            var text = sender.Text.Replace(Environment.NewLine, "\n");

            var lines = text.Count(ch => ch == '\n');
            if (!text.EndsWith('\n') && text.Length != 0) ++lines;

            var words = text.Split(default(char[]),
                                   StringSplitOptions.RemoveEmptyEntries)
                            .Length;

            var chars = text.Length;

            var lineReport = MaybePlural(lines, "line");
            var wordReport = MaybePlural(words, "word");
            var charReport = MaybePlural(chars, "character");

            dynamicBufferStats.Text =
                $"{lineReport}, {wordReport}, {charReport}";
        });

        var dynamicBufferPanel = new LC.StackPanel(horizontal: false,
                                                   dynamicBuffer,
                                                   dynamicBufferStats);

        var doSynchronousThingDirectly =
            new LC.Button("Do Synchronous Thing Directly",
                          doSynchronousThingDirectly_Click) {
                //IsMultithreaded = true
            };

        var doSynchronousThingViaDump =
            new LC.Button("Do Synchronous Thing Via Dump",
                          doSynchronousThingViaDump_Click);

        var doAsynchronousThingDirectly =
            new LC.Button("Do Asynchronous Thing Directly",
                          doAsynchronousThingDirectly_Click);

        var doAsynchronousThingViaDump =
            new LC.Button("Do Asynchronous Thing Via Dump",
                          doAsynchronousThingViaDump_Click);

        var triggerButtons =
            new LC.StackPanel(horizontal: true,
                              new LC.StackPanel(horizontal: false,
                                                doSynchronousThingDirectly,
                                                doSynchronousThingViaDump),
                              new LC.StackPanel(horizontal: false,
                                                doAsynchronousThingDirectly,
                                                doAsynchronousThingViaDump));

        var sameThreadWindow = new SeparateWindow();

        var openSeparateWindowThisThread =
            new LC.Button("Open Separate Window, This Thread",
                          delegate { sameThreadWindow.Show(); });

        sameThreadWindow.VisibleChanged += delegate {
            openSeparateWindowThisThread.Enabled = !sameThreadWindow.Visible;
        };

        var otherThreadWindow = new SeparateWindow();

        var otherThread = new Thread(() =>
            WF.Application.Run(otherThreadWindow));

        otherThread.SetApartmentState(ApartmentState.STA);

        var otherThreadLocker = new object();

        var openSeparateWindowOtherThread =
            new LC.Button("Open Separate Window, Other Thread", delegate {
                lock (otherThreadLocker) {
                    if (otherThread.ThreadState
                            == System.Threading.ThreadState.Unstarted) {
                        otherThread.Start();
                        return;
                    }
                }

                var invoker = new WF.MethodInvoker(otherThreadWindow.Show);
                otherThreadWindow.BeginInvoke(invoker);
            }) { IsMultithreaded = true };

        otherThreadWindow.VisibleChanged += delegate {
            openSeparateWindowOtherThread.Enabled =
                !otherThreadWindow.Visible;
        };

        var separateWindowOpeners =
            new LC.StackPanel(horizontal: true,
                              openSeparateWindowThisThread,
                              openSeparateWindowOtherThread);

        threadIdDumpers.Dump();

        staticBuffer
            .Dump("See if you can type in here while a thing is being done.");

        dynamicBufferPanel.Dump("Now try this one, which reacts as you type.");

        triggerButtons.Dump("Try these...");

        separateWindowOpeners
            .Dump("Cool... but how is a separate window affected?");

        Util.RawHtml("<hr/>").Dump();
    }
}
