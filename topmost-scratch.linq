<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>WF = System.Windows.Forms</Namespace>
</Query>

internal sealed class TopToggleExperiment : WF.Form {
    internal TopToggleExperiment()
    {
        HandleCreated += TopToggleExperiment_HandleCreated;
        KeyDown += TopToggleExperiment_KeyDown;
    }

    protected override void WndProc(ref WF.Message m)
    {
        base.WndProc(ref m);

        if ((uint)m.Msg == WM_SYSCOMMAND
                && (MyMenuItemId)m.WParam == MyMenuItemId.AlwaysOnTop)
            ToggleTopMost();
    }

    private const uint WM_SYSCOMMAND = 0x112;

    [Flags]
    private enum MenuFlags : uint {
        MF_STRING = 0,
        MF_BYPOSITION = 0x400,
        MF_SEPARATOR = 0x800,
        MF_REMOVE = 0x1000,
        MF_CHECKED = 0x8,
        MF_UNCHECKED = 0,
    }

    private enum MyMenuItemId : uint {
        AlwaysOnTop = 1,
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu,
                                          MenuFlags uFlags,
                                          MyMenuItemId uIDNewItem,
                                          string lpNewItem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool InsertMenu(IntPtr hMenu,
                                          uint uPosition,
                                          MenuFlags uFlags,
                                          MyMenuItemId uIDNewItem,
                                          string lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint CheckMenuItem(IntPtr hMenu,
                                             MyMenuItemId uIDCheckItem,
                                             MenuFlags uCheck);

    private IntPtr MenuHandle => GetSystemMenu(Handle, bRevert: false);

    private void TopToggleExperiment_HandleCreated(object? sender, EventArgs e)
        => InsertMenu(MenuHandle,
                      5,
                      MenuFlags.MF_STRING | MenuFlags.MF_BYPOSITION,
                      MyMenuItemId.AlwaysOnTop,
                      "&Always on top\tAlt+T\u2009");

    private void TopToggleExperiment_KeyDown(object sender, WF.KeyEventArgs e)
    {
        if (e.Modifiers == WF.Keys.Alt && e.KeyCode == WF.Keys.T)
            ToggleTopMost();
    }

    private void ToggleTopMost()
    {
        if (TopMost) {
            TopMost = false;
            CheckMenuItem(MenuHandle,
                          MyMenuItemId.AlwaysOnTop,
                          MenuFlags.MF_UNCHECKED);
        } else {
            TopMost = true;
            CheckMenuItem(MenuHandle,
                          MyMenuItemId.AlwaysOnTop,
                          MenuFlags.MF_CHECKED);
        }
    }
}

[STAThread]
private static void Main() => WF.Application.Run(new TopToggleExperiment());
