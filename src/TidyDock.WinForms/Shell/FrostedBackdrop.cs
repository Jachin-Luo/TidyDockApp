using System.Runtime.InteropServices;
using TidyDock.WinForms.UI;

namespace TidyDock.WinForms.Shell;

// A separate compositor surface keeps blur out of the layered owner's transparent margins.
internal sealed class FrostedBackdrop : NativeWindow, IDisposable
{
    private readonly Form _owner;
    private readonly Func<Rectangle> _getBounds;
    private readonly BackdropWindow _window;
    private Color _tint;
    private int _radius;
    private bool _requested;
    private bool _synchronizing;
    private bool _disposed;
    private Color? _appliedTint;
    private Size _regionSize;
    private int _regionRadius;
    private bool _transparencyEnabled = DwmBackdrop.TransparencyEnabled;

    public FrostedBackdrop(Form owner, Func<Rectangle> getBounds)
    {
        _owner = owner;
        _getBounds = getBounds;
        _window = new BackdropWindow(owner);
        owner.HandleCreated += OnHandleCreated;
        owner.HandleDestroyed += OnHandleDestroyed;
        owner.VisibleChanged += OnOwnerChanged;
        if (owner.IsHandleCreated)
        {
            AssignHandle(owner.Handle);
        }
    }

    public bool Active { get; private set; }
    internal IntPtr SurfaceHandle => _window.IsHandleCreated ? _window.Handle : IntPtr.Zero;
    public event EventHandler? ActiveChanged;

    public void Update(Color tint, int radius, bool visible = true)
    {
        _tint = tint;
        _radius = radius;
        _requested = visible && tint.A > 0;
        Synchronize();
    }

    private void OnHandleCreated(object? sender, EventArgs e) => AssignHandle(_owner.Handle);

    private void OnHandleDestroyed(object? sender, EventArgs e)
    {
        _window.Hide();
        Active = false;
        ReleaseHandle();
    }

    private void OnOwnerChanged(object? sender, EventArgs e) => Synchronize();

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x0047 or 0x031E or 0x001A) // Position, composition or system settings changed.
        {
            if (m.Msg is 0x031E or 0x001A)
            {
                _appliedTint = null;
                _transparencyEnabled = DwmBackdrop.TransparencyEnabled;
            }
            Synchronize();
        }
    }

    private void Synchronize()
    {
        if (_synchronizing || _disposed)
        {
            return;
        }

        _synchronizing = true;
        var wasActive = Active;
        try
        {
            if (!_requested || !_owner.IsHandleCreated || !_owner.Visible
                || _owner.WindowState == FormWindowState.Minimized || !_transparencyEnabled)
            {
                _window.Hide();
                Active = false;
                return;
            }

            var local = _getBounds();
            if (local.Width < 2 || local.Height < 2)
            {
                _window.Hide();
                Active = false;
                return;
            }
            var bounds = _owner.RectangleToScreen(local);
            if (_appliedTint != _tint)
            {
                Active = DwmBackdrop.TryApplyFrost(_window.Handle, _tint);
                _appliedTint = Active ? _tint : null;
            }
            else
            {
                Active = true;
            }
            if (!Active)
            {
                _window.Hide();
                return;
            }

            if (_window.Bounds != bounds)
            {
                _window.Bounds = bounds;
            }
            if (_window.Owner != _owner.Owner)
            {
                _window.Owner = _owner.Owner;
            }
            if (_window.TopMost != _owner.TopMost)
            {
                _window.TopMost = _owner.TopMost;
            }
            if (_regionSize != bounds.Size || _regionRadius != _radius)
            {
                using var path = CardPanel.RoundedRect(new Rectangle(Point.Empty, bounds.Size),
                    Math.Clamp(_radius, 1, Math.Min(bounds.Width, bounds.Height) / 2));
                var previous = _window.Region;
                _window.Region = new Region(path);
                previous?.Dispose();
                _regionSize = bounds.Size;
                _regionRadius = _radius;
            }
            if (!_window.Visible)
            {
                _window.Show();
            }
            // Reordering an unchanged Acrylic window restarts compositor work on every hover frame.
            if (GetWindow(_owner.Handle, 2) != _window.Handle) // GW_HWNDNEXT
            {
                _ = SetWindowPos(_window.Handle, _owner.Handle, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200);
            }
        }
        finally
        {
            _synchronizing = false;
            if (wasActive != Active)
            {
                ActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Active = false;
        _owner.HandleCreated -= OnHandleCreated;
        _owner.HandleDestroyed -= OnHandleDestroyed;
        _owner.VisibleChanged -= OnOwnerChanged;
        ReleaseHandle();
        _window.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPosition
    {
        public IntPtr Hwnd;
        public IntPtr InsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public uint Flags;
    }

    private sealed class BackdropWindow : Form
    {
        private readonly Form _foreground;

        public BackdropWindow(Form foreground)
        {
            _foreground = foreground;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= NativeMethods.WsExToolWindow | 0x08000000 | 0x00000020;
                return parameters;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0046 && _foreground.IsHandleCreated) // WM_WINDOWPOSCHANGING
            {
                var position = Marshal.PtrToStructure<WindowPosition>(m.LParam);
                if ((position.Flags & 0x0004) == 0 || (position.Flags & 0x0040) != 0)
                {
                    // Show/topmost changes must never put the material above its foreground, even briefly.
                    position.InsertAfter = _foreground.Handle;
                    position.Flags = (position.Flags & ~0x0004u) | 0x0010 | 0x0200;
                    Marshal.StructureToPtr(position, m.LParam, false);
                }
            }
            if (m.Msg == 0x0084)
            {
                m.Result = new IntPtr(-1);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
