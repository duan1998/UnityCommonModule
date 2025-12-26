using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public enum ParallelWaitMode
{
    WaitAll,
    WaitAny,
}

public sealed class ParallelStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly List<ISequenceStep> _children;
    private readonly ParallelWaitMode _mode;

    public ParallelStep(string name, ParallelWaitMode mode, IEnumerable<ISequenceStep> children)
    {
        Name = name;
        _mode = mode;
        _children = new List<ISequenceStep>(children);
    }

    public void Enter()
    {
        IsDone = false;

        if (_children.Count == 0)
        {
            IsDone = true;
            return;
        }
        
        foreach(var c in _children)
            c.Enter();
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        int doneCount = 0;
        foreach (var c in _children)
        {
            if (!c.IsDone)
                c.Tick(dt);
            if (c.IsDone)
                doneCount++;
        }

        IsDone = _mode switch
        {
            ParallelWaitMode.WaitAll => doneCount == _children.Count,
            ParallelWaitMode.WaitAny => doneCount > 0,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (IsDone && _mode == ParallelWaitMode.WaitAny)
        {
            foreach (var c in _children)
            {
                if (!c.IsDone) 
                    c.Cancel();
            }
        }
    }

    public void Cancel()
    {
        if (IsDone)
        {
            return;
        }
        
        foreach(var c in _children)
            if (!c.IsDone)
                c.Cancel();
        
        IsDone = true;
    }
}
