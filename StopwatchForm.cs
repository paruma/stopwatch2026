using System.Runtime.InteropServices;

namespace Stopwatch2026;

internal sealed class StopwatchForm : Form
{
    private const int WmSizing = 0x0214;
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 2;
    private const int WsThickFrame = 0x00040000;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsSysMenu = 0x00080000;
    private const double WindowAspectRatio = 8.0 / 3.0;

    private readonly Label _titleLabel = new();
    private readonly TextBox _titleEditor = new();
    private readonly Label _targetLabel = new();
    private readonly TextBox _targetEditor = new();
    private readonly ElapsedDisplay _elapsedLabel = new();
    private readonly Button _startStopButton = new();
    private readonly Button _resetButton = new();
    private readonly TableLayoutPanel _layout = new();
    private readonly TableLayoutPanel _topLayout = new();
    private readonly Panel _bottomLayout = new();
    private readonly FlowLayoutPanel _buttonPanel = new();
    private readonly Panel _titleSlot = new();
    private readonly Panel _targetSlot = new();
    private readonly System.Windows.Forms.Timer _displayTimer = new();
    private readonly ContextMenuStrip _windowMenu = new();

    private string _title = string.Empty;
    private string _titleBeforeEdit = string.Empty;
    private long? _targetMinutes;
    private long? _targetBeforeEdit;
    private long _accumulatedMilliseconds;
    private long _startedAtTick;
    private bool _running;
    private bool _closingConfirmed;

    public StopwatchForm()
    {
        Text = string.Empty;
        BackColor = ColorTranslator.FromHtml("#FFF59D");
        ForeColor = ColorTranslator.FromHtml("#202020");
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(600, 225);
        MinimumSize = new Size(540, 203);
        TopMost = true;
        ShowInTaskbar = true;
        KeyPreview = false;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        ConfigureLayout();
        ConfigureLabels();
        ConfigureEditors();
        ConfigureButtons();
        ConfigureMenu();

        _titleSlot.Controls.Add(_titleLabel);
        _titleSlot.Controls.Add(_titleEditor);
        _targetSlot.Controls.Add(_targetLabel);
        _targetSlot.Controls.Add(_targetEditor);
        _topLayout.Controls.Add(_titleSlot, 0, 0);
        _topLayout.Controls.Add(_targetSlot, 1, 0);
        _buttonPanel.Controls.Add(_resetButton);
        _buttonPanel.Controls.Add(_startStopButton);
        _bottomLayout.Controls.Add(_elapsedLabel);
        _bottomLayout.Controls.Add(_buttonPanel);
        _layout.Controls.Add(_topLayout, 0, 0);
        _layout.Controls.Add(_bottomLayout, 0, 1);
        Controls.Add(_layout);

        RegisterMouseWheel(this);
        RegisterOutsideClick(this);
        _elapsedLabel.MouseDown += BeginWindowDrag;
        _layout.MouseDown += BeginWindowDrag;
        _bottomLayout.MouseDown += BeginWindowDrag;
        _buttonPanel.MouseDown += BeginWindowDrag;

        _displayTimer.Interval = 100;
        _displayTimer.Tick += (_, _) => UpdateElapsedDisplay();
        _displayTimer.Start();

        FormClosing += OnFormClosing;
        UpdateTitleDisplay();
        UpdateTargetDisplay();
        UpdateElapsedDisplay();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _displayTimer.Dispose();
            _windowMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    // タイトルバーは表示しない。一方で Windows 標準のリサイズ枠を保持し、
    // 四辺・四隅のサイズ変更とタスクバー最小化は OS に任せる。
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.Style |= WsThickFrame | WsMinimizeBox | WsSysMenu;
            return parameters;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmSizing)
        {
            ApplyAspectRatio(m.WParam.ToInt32(), m.LParam);
        }

        base.WndProc(ref m);
    }

    private void ConfigureLabels()
    {
        _titleLabel.AutoSize = false;
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.AutoEllipsis = true;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Cursor = Cursors.IBeam;
        _titleLabel.Click += (_, _) => BeginTitleEdit();

        _targetLabel.AutoSize = false;
        _targetLabel.Dock = DockStyle.Fill;
        _targetLabel.TextAlign = ContentAlignment.MiddleCenter;
        _targetLabel.Cursor = Cursors.IBeam;
        _targetLabel.Click += (_, _) => BeginTargetEdit();

        _elapsedLabel.AutoSize = false;
        _elapsedLabel.Dock = DockStyle.Fill;
        _elapsedLabel.Cursor = Cursors.SizeAll;
    }

    private void ConfigureLayout()
    {
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(8);
        _layout.Margin = Padding.Empty;
        _layout.BackColor = BackColor;
        _layout.ColumnCount = 1;
        _layout.RowCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _topLayout.Dock = DockStyle.Fill;
        _topLayout.Margin = Padding.Empty;
        _topLayout.BackColor = BackColor;
        _topLayout.ColumnCount = 2;
        _topLayout.RowCount = 1;
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
        _topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _bottomLayout.Dock = DockStyle.Fill;
        _bottomLayout.Margin = Padding.Empty;
        _bottomLayout.BackColor = BackColor;

        _buttonPanel.AutoSize = false;
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.WrapContents = false;
        _buttonPanel.Margin = Padding.Empty;
        _buttonPanel.Padding = Padding.Empty;
        _buttonPanel.BackColor = BackColor;

        _titleSlot.Dock = DockStyle.Fill;
        _titleSlot.Margin = Padding.Empty;
        _titleSlot.BackColor = BackColor;
        _targetSlot.Dock = DockStyle.Fill;
        _targetSlot.Margin = Padding.Empty;
        _targetSlot.BackColor = BackColor;
    }

    private void ConfigureEditors()
    {
        _titleEditor.Visible = false;
        _titleEditor.Dock = DockStyle.Fill;
        _titleEditor.Margin = Padding.Empty;
        _titleEditor.BorderStyle = BorderStyle.FixedSingle;
        _titleEditor.MaxLength = 200;
        _titleEditor.KeyDown += TitleEditorKeyDown;
        _titleEditor.Leave += (_, _) => CommitTitleEdit();

        _targetEditor.Visible = false;
        _targetEditor.Dock = DockStyle.Fill;
        _targetEditor.Margin = Padding.Empty;
        _targetEditor.BorderStyle = BorderStyle.FixedSingle;
        _targetEditor.TextAlign = HorizontalAlignment.Center;
        _targetEditor.MaxLength = 20;
        _targetEditor.KeyDown += TargetEditorKeyDown;
        _targetEditor.Leave += (_, _) => CommitTargetEdit();
    }

    private void ConfigureButtons()
    {
        _startStopButton.Name = "StartStopButton";
        _resetButton.Name = "ResetButton";
        ConfigureFlatButton(_startStopButton, "開始");
        ConfigureFlatButton(_resetButton, "リセット");
        _startStopButton.Margin = new Padding(1, 0, 1, 0);
        _resetButton.Margin = new Padding(1, 0, 0, 0);
        _startStopButton.Click += (_, _) => ToggleTimer();
        _resetButton.Click += (_, _) => ResetTimer();
        _buttonPanel.Height = Math.Max(
            _startStopButton.PreferredSize.Height,
            _resetButton.PreferredSize.Height) + 2;
    }

    private void ConfigureMenu()
    {
        var minimizeItem = new ToolStripMenuItem("最小化");
        minimizeItem.Click += (_, _) => WindowState = FormWindowState.Minimized;
        var closeItem = new ToolStripMenuItem("閉じる");
        closeItem.Click += (_, _) => Close();
        _windowMenu.Items.Add(minimizeItem);
        _windowMenu.Items.Add(closeItem);
        ContextMenuStrip = _windowMenu;
        _titleLabel.ContextMenuStrip = _windowMenu;
        _targetLabel.ContextMenuStrip = _windowMenu;
        _elapsedLabel.ContextMenuStrip = _windowMenu;
        _startStopButton.ContextMenuStrip = _windowMenu;
        _resetButton.ContextMenuStrip = _windowMenu;
        _layout.ContextMenuStrip = _windowMenu;
        _topLayout.ContextMenuStrip = _windowMenu;
        _bottomLayout.ContextMenuStrip = _windowMenu;
        _buttonPanel.ContextMenuStrip = _windowMenu;
        _titleSlot.ContextMenuStrip = _windowMenu;
        _targetSlot.ContextMenuStrip = _windowMenu;
    }

    private static void ConfigureFlatButton(Button button, string text)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#B8A94C");
        button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#F2DF68");
        button.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#E4CF4F");
        button.BackColor = ColorTranslator.FromHtml("#F8E879");
        button.ForeColor = ColorTranslator.FromHtml("#202020");
        button.UseVisualStyleBackColor = false;
        button.TabStop = false;
        button.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Padding = new Padding(5, 1, 5, 1);
    }

    private void BeginTitleEdit()
    {
        _titleBeforeEdit = _title;
        _titleEditor.Text = _title;
        _titleLabel.Visible = false;
        _titleEditor.Visible = true;
        _titleEditor.Focus();
        _titleEditor.SelectAll();
    }

    private void CommitTitleEdit()
    {
        if (!_titleEditor.Visible)
        {
            return;
        }

        _title = _titleEditor.Text.Trim();
        EndTitleEdit();
    }

    private void CancelTitleEdit()
    {
        _title = _titleBeforeEdit;
        EndTitleEdit();
    }

    private void EndTitleEdit()
    {
        _titleEditor.Visible = false;
        _titleLabel.Visible = true;
        UpdateTitleDisplay();
    }

    private void TitleEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            CommitTitleEdit();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            CancelTitleEdit();
        }
    }

    private void UpdateTitleDisplay()
    {
        bool empty = string.IsNullOrWhiteSpace(_title);
        _titleLabel.Text = empty ? "タイトル" : _title;
        _titleLabel.ForeColor = empty ? Color.FromArgb(115, 105, 70) : ForeColor;
    }

    private void BeginTargetEdit()
    {
        _targetBeforeEdit = _targetMinutes;
        _targetEditor.Text = _targetMinutes is null ? string.Empty : FormatTarget(_targetMinutes.Value);
        _targetLabel.Visible = false;
        _targetEditor.Visible = true;
        _targetEditor.Focus();
        _targetEditor.SelectAll();
    }

    private void CommitTargetEdit()
    {
        if (!_targetEditor.Visible)
        {
            return;
        }

        if (!TryParseTarget(_targetEditor.Text, out long? minutes))
        {
            System.Media.SystemSounds.Beep.Play();
            _targetMinutes = _targetBeforeEdit;
            EndTargetEdit();
            return;
        }

        _targetMinutes = minutes;
        EndTargetEdit();
    }

    private void CancelTargetEdit()
    {
        _targetMinutes = _targetBeforeEdit;
        EndTargetEdit();
    }

    private void EndTargetEdit()
    {
        _targetEditor.Visible = false;
        _targetLabel.Visible = true;
        UpdateTargetDisplay();
        UpdateElapsedDisplay();
    }

    private void TargetEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            CommitTargetEdit();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            CancelTargetEdit();
        }
    }

    private void UpdateTargetDisplay()
    {
        _targetLabel.Text = _targetMinutes is null ? "00:00" : FormatTarget(_targetMinutes.Value);
        _targetLabel.ForeColor = _targetMinutes is null ? Color.FromArgb(115, 105, 70) : ForeColor;
    }

    private static bool TryParseTarget(string text, out long? minutes)
    {
        minutes = null;
        text = text.Trim();
        if (text.Length == 0 || text == "00:00")
        {
            return true;
        }

        try
        {
            if (!text.Contains(':'))
            {
                if (!long.TryParse(text, out long totalMinutes) || totalMinutes < 0)
                {
                    return false;
                }

                minutes = totalMinutes == 0 ? null : totalMinutes;
                return true;
            }

            string[] parts = text.Split(':');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], out long hours) || hours < 0 ||
                !int.TryParse(parts[1], out int minutePart) || minutePart is < 0 or > 59)
            {
                return false;
            }

            long total = checked((hours * 60) + minutePart);
            minutes = total == 0 ? null : total;
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static string FormatTarget(long totalMinutes)
    {
        return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    private void ToggleTimer()
    {
        if (_running)
        {
            _accumulatedMilliseconds = GetElapsedMilliseconds();
            _running = false;
            _startStopButton.Text = "再開";
        }
        else
        {
            _startedAtTick = Environment.TickCount64;
            _running = true;
            _startStopButton.Text = "停止";
        }

        UpdateElapsedDisplay();
    }

    private void ResetTimer()
    {
        if (GetElapsedMilliseconds() > 0 && MessageBox.Show(
                this,
                "経過時間をリセットしますか？",
                "stopwatch2026",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        _running = false;
        _accumulatedMilliseconds = 0;
        _startedAtTick = 0;
        _startStopButton.Text = "開始";
        UpdateElapsedDisplay();
    }

    private long GetElapsedMilliseconds()
    {
        if (!_running)
        {
            return _accumulatedMilliseconds;
        }

        long delta = Environment.TickCount64 - _startedAtTick;
        return delta > long.MaxValue - _accumulatedMilliseconds
            ? long.MaxValue
            : _accumulatedMilliseconds + Math.Max(0, delta);
    }

    private void UpdateElapsedDisplay()
    {
        long elapsedMilliseconds = GetElapsedMilliseconds();
        long totalSeconds = elapsedMilliseconds / 1000;
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds / 60) % 60;
        long seconds = totalSeconds % 60;
        string newText = $"{hours:00}:{minutes:00}:{seconds:00}";

        _elapsedLabel.Text = newText;

        bool overTarget = _targetMinutes is long target &&
            elapsedMilliseconds / 60_000L >= target;
        _elapsedLabel.ForeColor = overTarget
            ? ColorTranslator.FromHtml("#C62828")
            : ForeColor;
    }

    private void RegisterMouseWheel(Control control)
    {
        control.MouseWheel += ChangeOpacity;
        foreach (Control child in control.Controls)
        {
            RegisterMouseWheel(child);
        }
    }

    private void RegisterOutsideClick(Control control)
    {
        if (control is not TextBox)
        {
            control.MouseDown += (_, _) => FinishActiveEditor();
        }

        foreach (Control child in control.Controls)
        {
            RegisterOutsideClick(child);
        }
    }

    private void FinishActiveEditor()
    {
        if (_titleEditor.Visible)
        {
            CommitTitleEdit();
        }

        if (_targetEditor.Visible)
        {
            CommitTargetEdit();
        }
    }

    private void ChangeOpacity(object? sender, MouseEventArgs e)
    {
        if (e.Delta == 0)
        {
            return;
        }

        double direction = Math.Sign(e.Delta) * 0.05;
        Opacity = Math.Clamp(Math.Round((Opacity + direction) * 20) / 20, 0.20, 1.00);
    }

    private void BeginWindowDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    private int ScaleLogical(int value)
    {
        // AutoScaleMode.Dpi により、フォームと子コントロールの座標系は
        // すでに現在のDPIへ変換されている。ここで再度倍率を掛けない。
        return value;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closingConfirmed || GetElapsedMilliseconds() == 0)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                "閉じると計測内容は失われます。閉じますか？",
                "stopwatch2026",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
        {
            _closingConfirmed = true;
            return;
        }

        e.Cancel = true;
    }

    private void ApplyAspectRatio(int edge, IntPtr rectanglePointer)
    {
        var rectangle = Marshal.PtrToStructure<NativeMethods.Rect>(rectanglePointer);
        int width = rectangle.Right - rectangle.Left;
        int height = rectangle.Bottom - rectangle.Top;
        bool horizontalEdge = edge is 1 or 2;

        if (horizontalEdge)
        {
            int newHeight = Math.Max(MinimumSize.Height, (int)Math.Round(width / WindowAspectRatio));
            rectangle.Bottom = rectangle.Top + newHeight;
        }
        else
        {
            int newWidth = Math.Max(MinimumSize.Width, (int)Math.Round(height * WindowAspectRatio));
            if (edge is 4 or 5 or 7)
            {
                rectangle.Left = rectangle.Right - newWidth;
            }
            else
            {
                rectangle.Right = rectangle.Left + newWidth;
            }
        }

        Marshal.StructureToPtr(rectangle, rectanglePointer, false);
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    }
}
