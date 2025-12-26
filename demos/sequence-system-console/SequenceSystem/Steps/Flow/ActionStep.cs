using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

/// <summary>
/// 同步动作步骤 - 执行一个同步 Action
/// </summary>
public sealed class ActionStep : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly Action _action;

    /// <summary>
    /// 创建动作步骤（自动生成名称）
    /// </summary>
    public ActionStep(Action action) : this("Action", action) { }

    /// <summary>
    /// 创建动作步骤（指定名称）
    /// </summary>
    public ActionStep(string name, Action action)
    {
        Name = name;
        _action = action;
    }

    public void Enter()
    {
        IsDone = false;
        _action();
        IsDone = true;
    }

    public void Tick(float dt) { }

    public void Cancel()
    {
        IsDone = true;
    }
}

/// <summary>
/// 异步动作步骤 - 支持条件完成
/// </summary>
public sealed class AsyncActionStep : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly Action _onEnter;
    private readonly Func<bool>? _isDoneCondition;

    /// <summary>
    /// 创建异步动作步骤
    /// </summary>
    /// <param name="onEnter">进入时执行的动作</param>
    /// <param name="isDone">完成条件（每帧检查），为 null 则立即完成</param>
    public AsyncActionStep(Action onEnter, Func<bool>? isDone = null)
        : this("AsyncAction", onEnter, isDone) { }

    public AsyncActionStep(string name, Action onEnter, Func<bool>? isDone = null)
    {
        Name = name;
        _onEnter = onEnter;
        _isDoneCondition = isDone;
    }

    public void Enter()
    {
        IsDone = false;
        _onEnter();
        
        // 如果没有完成条件，立即完成
        if (_isDoneCondition == null)
            IsDone = true;
    }

    public void Tick(float dt)
    {
        if (IsDone) return;
        
        if (_isDoneCondition?.Invoke() == true)
            IsDone = true;
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
