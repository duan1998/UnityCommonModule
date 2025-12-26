using System.Diagnostics;
using System.Text;

namespace SequenceSystem.Core;

/// <summary>
/// 序列调试器：提供步骤树可视化、耗时统计等调试功能
/// </summary>
public sealed class SequenceDebugger
{
    private readonly Dictionary<ISequenceStep, StepMetrics> _metrics = new();
    private readonly List<string> _executionLog = new();
    private readonly int _maxLogEntries;
    private bool _isEnabled = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public SequenceDebugger(int maxLogEntries = 1000)
    {
        _maxLogEntries = maxLogEntries;
    }

    /// <summary>
    /// 记录 Step 开始执行
    /// </summary>
    public void OnStepEnter(ISequenceStep step)
    {
        if (!_isEnabled) return;
        
        var metrics = GetOrCreateMetrics(step);
        metrics.StartTime = Stopwatch.GetTimestamp();
        metrics.Status = StepStatus.Running;
        
        Log($"[ENTER] {step.Name}");
    }

    /// <summary>
    /// 记录 Step 完成
    /// </summary>
    public void OnStepExit(ISequenceStep step)
    {
        if (!_isEnabled) return;
        
        if (_metrics.TryGetValue(step, out var metrics))
        {
            metrics.EndTime = Stopwatch.GetTimestamp();
            metrics.Status = StepStatus.Completed;
            
            var elapsed = (metrics.EndTime - metrics.StartTime) / (double)Stopwatch.Frequency;
            Log($"[EXIT] {step.Name} ({elapsed:F3}s)");
        }
    }

    /// <summary>
    /// 记录 Step 取消
    /// </summary>
    public void OnStepCancel(ISequenceStep step)
    {
        if (!_isEnabled) return;
        
        if (_metrics.TryGetValue(step, out var metrics))
        {
            metrics.EndTime = Stopwatch.GetTimestamp();
            metrics.Status = StepStatus.Cancelled;
            
            Log($"[CANCEL] {step.Name}");
        }
    }

    /// <summary>
    /// 记录 Step 错误
    /// </summary>
    public void OnStepError(ISequenceStep step, Exception ex)
    {
        if (!_isEnabled) return;
        
        if (_metrics.TryGetValue(step, out var metrics))
        {
            metrics.EndTime = Stopwatch.GetTimestamp();
            metrics.Status = StepStatus.Error;
            metrics.Error = ex;
            
            Log($"[ERROR] {step.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新 Step 的 WaitingFor 信息
    /// </summary>
    public void UpdateWaitingFor(ISequenceStep step, string? waitingFor)
    {
        if (!_isEnabled) return;
        
        var metrics = GetOrCreateMetrics(step);
        metrics.WaitingFor = waitingFor;
        
        if (!string.IsNullOrEmpty(waitingFor))
        {
            Log($"[WAITING] {step.Name} -> {waitingFor}");
        }
    }

    /// <summary>
    /// 打印步骤树
    /// </summary>
    public string PrintStepTree(ISequenceStep rootStep, int maxDepth = 10)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Step Tree ===");
        PrintStepTreeRecursive(sb, rootStep, 0, maxDepth);
        sb.AppendLine("=================");
        return sb.ToString();
    }

    private void PrintStepTreeRecursive(StringBuilder sb, ISequenceStep step, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        
        var indent = new string(' ', depth * 2);
        var metrics = _metrics.GetValueOrDefault(step);
        
        var statusIcon = (metrics?.Status ?? StepStatus.Pending) switch
        {
            StepStatus.Pending => "○",
            StepStatus.Running => "●",
            StepStatus.Completed => "✓",
            StepStatus.Cancelled => "✗",
            StepStatus.Error => "⚠",
            _ => "?"
        };
        
        var waitingInfo = "";
        if (step is IDebuggableStep debuggable && !string.IsNullOrEmpty(debuggable.WaitingFor))
        {
            waitingInfo = $" (waiting: {debuggable.WaitingFor})";
        }
        else if (metrics?.WaitingFor != null)
        {
            waitingInfo = $" (waiting: {metrics.WaitingFor})";
        }
        
        var timeInfo = "";
        if (metrics?.Status == StepStatus.Running)
        {
            var elapsed = (Stopwatch.GetTimestamp() - metrics.StartTime) / (double)Stopwatch.Frequency;
            timeInfo = $" [{elapsed:F2}s]";
        }
        else if (metrics?.Status == StepStatus.Completed || metrics?.Status == StepStatus.Cancelled)
        {
            var elapsed = (metrics.EndTime - metrics.StartTime) / (double)Stopwatch.Frequency;
            timeInfo = $" [{elapsed:F2}s]";
        }
        
        sb.AppendLine($"{indent}{statusIcon} {step.Name}{waitingInfo}{timeInfo}");
        
        // 打印子步骤
        if (step is IDebuggableStep debugStep && debugStep.Children != null)
        {
            foreach (var child in debugStep.Children)
            {
                PrintStepTreeRecursive(sb, child, depth + 1, maxDepth);
            }
        }
        else if (step is IStepContainer container)
        {
            // 对于 IStepContainer，尝试通过反射获取子步骤（简化实现）
        }
    }

    /// <summary>
    /// 获取执行日志
    /// </summary>
    public IReadOnlyList<string> GetExecutionLog() => _executionLog;

    /// <summary>
    /// 打印执行日志
    /// </summary>
    public string PrintExecutionLog(int lastN = 50)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Execution Log ===");
        
        var startIndex = Math.Max(0, _executionLog.Count - lastN);
        for (int i = startIndex; i < _executionLog.Count; i++)
        {
            sb.AppendLine(_executionLog[i]);
        }
        
        sb.AppendLine("=====================");
        return sb.ToString();
    }

    /// <summary>
    /// 获取耗时统计
    /// </summary>
    public string PrintTimingStats()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Timing Stats ===");
        
        var completedSteps = _metrics
            .Where(kv => kv.Value.Status == StepStatus.Completed)
            .OrderByDescending(kv => kv.Value.EndTime - kv.Value.StartTime)
            .Take(20);
        
        foreach (var (step, metrics) in completedSteps)
        {
            var elapsed = (metrics.EndTime - metrics.StartTime) / (double)Stopwatch.Frequency;
            sb.AppendLine($"  {step.Name}: {elapsed:F3}s");
        }
        
        sb.AppendLine("====================");
        return sb.ToString();
    }

    /// <summary>
    /// 清除所有数据
    /// </summary>
    public void Clear()
    {
        _metrics.Clear();
        _executionLog.Clear();
    }

    private StepMetrics GetOrCreateMetrics(ISequenceStep step)
    {
        if (!_metrics.TryGetValue(step, out var metrics))
        {
            metrics = new StepMetrics();
            _metrics[step] = metrics;
        }
        return metrics;
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        _executionLog.Add($"[{timestamp}] {message}");
        
        // 限制日志大小
        while (_executionLog.Count > _maxLogEntries)
        {
            _executionLog.RemoveAt(0);
        }
    }

    private class StepMetrics
    {
        public long StartTime;
        public long EndTime;
        public StepStatus Status = StepStatus.Pending;
        public string? WaitingFor;
        public Exception? Error;
    }
}

/// <summary>
/// 带有 WaitingFor 信息的异步 Step 示例
/// </summary>
public sealed class AsyncLoadStep : ISequenceStep, IDebuggableStep
{
    private readonly string _resourceKey;
    private readonly float _simulatedLoadTime;
    private float _elapsed;

    public string Name { get; }
    public bool IsDone { get; private set; }
    public string? WaitingFor => IsDone ? null : $"asset://{_resourceKey}";
    public IReadOnlyList<ISequenceStep>? Children => null;
    public int CurrentChildIndex => -1;

    public AsyncLoadStep(string name, string resourceKey, float simulatedLoadTime = 1f)
    {
        Name = name;
        _resourceKey = resourceKey;
        _simulatedLoadTime = simulatedLoadTime;
    }

    public void Enter()
    {
        _elapsed = 0;
        IsDone = false;
        Console.WriteLine($"[AsyncLoad] '{Name}' Start loading: {_resourceKey}");
    }

    public void Tick(float dt)
    {
        _elapsed += dt;
        if (_elapsed >= _simulatedLoadTime)
        {
            Console.WriteLine($"[AsyncLoad] '{Name}' Loaded: {_resourceKey}");
            IsDone = true;
        }
    }

    public void Cancel()
    {
        Console.WriteLine($"[AsyncLoad] '{Name}' Cancelled");
        IsDone = true;
    }
}
