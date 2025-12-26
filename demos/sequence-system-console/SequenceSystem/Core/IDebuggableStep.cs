namespace SequenceSystem.Core;

/// <summary>
/// 可调试的 Step 接口
/// 实现此接口的 Step 可以提供更详细的调试信息
/// </summary>
public interface IDebuggableStep : ISequenceStep
{
    /// <summary>
    /// 当前正在等待什么（用于调试）
    /// 例如："asset://icon.png", "network://login", "animation://attack"
    /// </summary>
    string? WaitingFor { get; }
    
    /// <summary>
    /// 获取子步骤列表（用于步骤树可视化）
    /// </summary>
    IReadOnlyList<ISequenceStep>? Children { get; }
    
    /// <summary>
    /// 当前正在执行的子步骤索引（-1 表示没有）
    /// </summary>
    int CurrentChildIndex { get; }
}

/// <summary>
/// Step 执行状态
/// </summary>
public enum StepStatus
{
    Pending,    // 等待执行
    Running,    // 正在执行
    Completed,  // 已完成
    Cancelled,  // 已取消
    Error       // 出错
}

/// <summary>
/// Step 调试信息快照
/// </summary>
public class StepDebugInfo
{
    public string Name { get; init; } = "";
    public StepStatus Status { get; init; }
    public string? WaitingFor { get; init; }
    public double ElapsedSeconds { get; init; }
    public int Depth { get; init; }
    public List<StepDebugInfo> Children { get; init; } = new();
    
    public override string ToString()
    {
        var indent = new string(' ', Depth * 2);
        var statusIcon = Status switch
        {
            StepStatus.Pending => "○",
            StepStatus.Running => "●",
            StepStatus.Completed => "✓",
            StepStatus.Cancelled => "✗",
            StepStatus.Error => "⚠",
            _ => "?"
        };
        
        var waitingInfo = string.IsNullOrEmpty(WaitingFor) ? "" : $" (waiting: {WaitingFor})";
        var timeInfo = Status == StepStatus.Running ? $" [{ElapsedSeconds:F2}s]" : "";
        
        return $"{indent}{statusIcon} {Name}{waitingInfo}{timeInfo}";
    }
}
