namespace Stopwatch2026;

/// <summary>
/// リサイズ可能な時間表示。計測と描画を同じピクセル単位で扱うため、
/// WindowsのDPI倍率にかかわらず一行に収まる最大文字サイズを選ぶ。
/// </summary>
internal sealed class ElapsedDisplay : Control
{
    private const TextFormatFlags DisplayFlags =
        TextFormatFlags.HorizontalCenter |
        TextFormatFlags.VerticalCenter |
        TextFormatFlags.SingleLine |
        TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix;

    public ElapsedDisplay()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.StaticText;
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        AccessibleName = Text;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (string.IsNullOrEmpty(Text) || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        Rectangle bounds = Rectangle.Inflate(ClientRectangle, -4, -3);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float low = 8F;
        float high = Math.Max(low, bounds.Height);
        for (int i = 0; i < 10; i++)
        {
            float middle = (low + high) / 2;
            using var trial = new Font("Consolas", middle, FontStyle.Bold, GraphicsUnit.Pixel);
            Size measured = TextRenderer.MeasureText(
                e.Graphics,
                Text,
                trial,
                new Size(int.MaxValue, int.MaxValue),
                DisplayFlags);
            if (measured.Width <= bounds.Width && measured.Height <= bounds.Height)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        using var font = new Font("Consolas", low, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(e.Graphics, Text, font, bounds, ForeColor, DisplayFlags);
    }
}
