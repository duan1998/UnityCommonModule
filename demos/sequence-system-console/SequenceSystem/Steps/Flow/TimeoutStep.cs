using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public enum TimeoutAction
{
    CancelInnerAndComplete, // 超时： 取消内部step，并把自己视为完成
    CompleteWithoutCancel, // 超时： 不取消内部（少用，容易污染）
}
public sealed class TimeoutStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly ISequenceStep _inner;
    private readonly float _timeoutSeconds;
    private readonly TimeoutAction _action;

    private float _remain;

    public TimeoutStep(string name, ISequenceStep inner, float timeoutSeconds,
        TimeoutAction action = TimeoutAction.CancelInnerAndComplete)
    {
        Name = name;
        _inner = inner;
        _timeoutSeconds = timeoutSeconds;
        _action = action;
    }

    public void Enter()
    {
        IsDone = false;
        _remain = _timeoutSeconds;
        _inner.Enter();
    }

    public void Tick(float dt)
    {
        if (IsDone)
            return;
        
        if (!_inner.IsDone)
            _inner.Tick(dt);
        if (_inner.IsDone)
        {
            IsDone = true;
            return;
        }

        _remain -= dt;
        if (_remain > 0) return;
        
        // timeout
        if (_action == TimeoutAction.CancelInnerAndComplete && !_inner.IsDone)
            _inner.Cancel();

        IsDone = true;
    }

    public void Cancel()
    {
        if (IsDone)
            return;

        if (!_inner.IsDone)
        {
            _inner.Cancel();
        }

        IsDone = true;
    }
}
