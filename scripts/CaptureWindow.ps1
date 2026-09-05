param(
    [string] $ProcessName,

    [int] $ProcessId,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class WindowCaptureNative
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
    public static extern bool GetWindowRect(IntPtr handle, out RECT rectangle);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr handle, IntPtr deviceContext, uint flags);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr handle);
}
'@

$process = if ($ProcessId -gt 0) {
    Get-Process -Id $ProcessId -ErrorAction Stop
}
else {
    if ([string]::IsNullOrWhiteSpace($ProcessName)) {
        throw "Specify ProcessName or ProcessId."
    }
    Get-Process $ProcessName -ErrorAction Stop | Select-Object -First 1
}
$process.Refresh()
if ($process.MainWindowHandle -eq 0) {
    throw "The process does not have a main window."
}

[WindowCaptureNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 300

$rectangle = New-Object WindowCaptureNative+RECT
if (-not [WindowCaptureNative]::GetWindowRect($process.MainWindowHandle, [ref] $rectangle)) {
    throw "GetWindowRect failed."
}

$width = $rectangle.Right - $rectangle.Left
$height = $rectangle.Bottom - $rectangle.Top
$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $deviceContext = $graphics.GetHdc()
    try {
        if (-not [WindowCaptureNative]::PrintWindow($process.MainWindowHandle, $deviceContext, 2)) {
            throw "PrintWindow failed."
        }
    }
    finally {
        $graphics.ReleaseHdc($deviceContext)
    }
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}

[pscustomobject]@{
    Width = $width
    Height = $height
    Dpi = [WindowCaptureNative]::GetDpiForWindow($process.MainWindowHandle)
    Title = $process.MainWindowTitle
    OutputPath = $OutputPath
}
