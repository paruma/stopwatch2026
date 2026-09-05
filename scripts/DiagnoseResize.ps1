Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class ResizeDiagnosticNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
}
'@

$process = Get-Process -Name stopwatch2026 | Sort-Object Id | Select-Object -Last 1
$rect = New-Object ResizeDiagnosticNative+RECT
[ResizeDiagnosticNative]::GetWindowRect($process.MainWindowHandle, [ref]$rect) | Out-Null

$tests = @{
    left = @($rect.Left, [int](($rect.Top + $rect.Bottom) / 2))
    right = @(($rect.Right - 1), [int](($rect.Top + $rect.Bottom) / 2))
    top = @([int](($rect.Left + $rect.Right) / 2), $rect.Top)
    bottom = @([int](($rect.Left + $rect.Right) / 2), ($rect.Bottom - 1))
}

"rect=$($rect.Left),$($rect.Top),$($rect.Right),$($rect.Bottom)"
foreach ($name in $tests.Keys) {
    $point = $tests[$name]
    [long]$lParam = (($point[1] -shl 16) -bor ($point[0] -band 0xffff))
    $hit = [ResizeDiagnosticNative]::SendMessage(
        $process.MainWindowHandle,
        0x0084,
        [IntPtr]::Zero,
        [IntPtr]$lParam).ToInt64()
    "$name=$($point[0]),$($point[1]) hit=$hit"
}
