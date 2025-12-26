using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class SkippableStep: ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly ISequenceStep _inner;
    private bool _skipped;

    public SkippableStep(string name, ISequenceStep inner)
    {
        Name = name;
        _inner = inner;
    }

    public void Enter()
    {
        IsDone = false;
        _skipped = false;
        _inner.Enter(); 
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        if (_skipped)
        {
            if (!_inner.IsDone) _inner.Cancel();
            IsDone = true;
            return;
        }
        
        _inner.Tick(dt);
        
        if (_inner.IsDone) IsDone = true;
    }

    public void Cancel()
    {
        if (IsDone) return;
        
        _inner.Cancel();
        IsDone = true;
    }

    public void Skip()
    {
        _skipped = true;
    }
}
