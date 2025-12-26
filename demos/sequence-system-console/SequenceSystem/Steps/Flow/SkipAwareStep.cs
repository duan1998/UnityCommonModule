using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class SkipAwareStep : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly ISequenceStep _inner;
    private readonly Func<bool> _isSkipping;

    public SkipAwareStep(string name, ISequenceStep inner, Func<bool> isSkipping)
    {
        Name = name;
        _inner = inner;
        _isSkipping = isSkipping;
    }

    public void Enter()
    {
        IsDone = false;
        _inner.Enter();

        if (_isSkipping() && !_inner.IsDone)
        {
            _inner.Cancel();
            IsDone = true;
        }
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        if (_isSkipping())
        {
            if(!_inner.IsDone) _inner.Cancel();
            IsDone = true;
            return;
        }
        
        _inner.Tick(dt);
        if(_inner.IsDone)
            IsDone = true;
    }

    public void Cancel()
    {
        if (IsDone) return;
        
        _inner.Cancel();
        IsDone = true;
    }
}

/*
var player = new SequencePlayer(...);

// 包装一个"可感知全局 skip"的等待步骤
new SkipAwareStep(
    "SkipAware-Wait",
    inner: new WaitSecondsStep("Wait3s", 3f),
    isSkipping: () => player.IsSkipping
)

*/
