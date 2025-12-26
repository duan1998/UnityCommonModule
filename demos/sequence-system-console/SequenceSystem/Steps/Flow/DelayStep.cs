using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

/// <summary>
/// 延迟步骤 - 等待指定时间
/// </summary>
public sealed class DelayStep : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly float _duration;
    private float _remain;

    /// <summary>
    /// 创建延迟步骤（自动生成名称）
    /// </summary>
    public DelayStep(float seconds) : this($"Delay({seconds}s)", seconds) { }

    /// <summary>
    /// 创建延迟步骤（指定名称）
    /// </summary>
    public DelayStep(string name, float seconds)
    {
        Name = name;
        _duration = seconds;
    }

    public void Enter()
    {
        IsDone = false;
        _remain = _duration;
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        _remain -= dt;
        if (_remain <= 0)
            IsDone = true;
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
