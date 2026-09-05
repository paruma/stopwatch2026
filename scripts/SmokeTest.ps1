param(
    [Parameter(Mandatory = $true)]
    [string] $ExecutablePath,

    [Parameter(Mandatory = $true)]
    [string] $DotnetRoot
)

$env:DOTNET_ROOT = $DotnetRoot
Add-Type -AssemblyName UIAutomationClient

function Find-Element([System.Windows.Automation.AutomationElement] $root, [string] $controlType, [string] $automationId) {
    $type = [System.Windows.Automation.ControlType]::$controlType
    $condition = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            $type)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            $automationId)))
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Read-Elapsed([System.Windows.Automation.AutomationElement] $root) {
    $elements = $root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($element in $elements) {
        if ($element.Current.Name -match '^\d{2,}:\d{2}:\d{2}$') {
            return $element.Current.Name
        }
    }
    throw "Elapsed-time label was not found."
}

function Invoke-Button([System.Windows.Automation.AutomationElement] $button) {
    if ($null -eq $button) {
        throw "Expected button was not found."
    }
    $pattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

$process = Start-Process $ExecutablePath -PassThru
try {
    for ($attempt = 0; $attempt -lt 30 -and $process.MainWindowHandle -eq 0; $attempt++) {
        Start-Sleep -Milliseconds 100
        $process.Refresh()
    }
    if ($process.MainWindowHandle -eq 0) {
        throw "The application did not create a main window."
    }

    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    $initial = Read-Elapsed $root
    if ($initial -ne '00:00:00') {
        throw "Unexpected initial time: $initial"
    }

    Invoke-Button (Find-Element $root 'Button' 'StartStopButton')
    Start-Sleep -Milliseconds 1250
    $running = Read-Elapsed $root
    if ($running -eq '00:00:00') {
        throw "Elapsed time did not advance."
    }

    Invoke-Button (Find-Element $root 'Button' 'StartStopButton')
    $stopped = Read-Elapsed $root
    Start-Sleep -Milliseconds 1250
    $stillStopped = Read-Elapsed $root
    if ($stopped -ne $stillStopped) {
        throw "Elapsed time advanced after stopping: $stopped -> $stillStopped"
    }

    [pscustomobject]@{
        Initial = $initial
        Running = $running
        Stopped = $stopped
        StillStopped = $stillStopped
        Result = 'PASS'
    }
}
finally {
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }
}
