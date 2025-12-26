using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class SequenceStep : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly List<ISequenceStep> _steps;
    private int _index;

    public SequenceStep(string name, IEnumerable<ISequenceStep> steps)
    {
        Name = name;
        _steps = new List<ISequenceStep>(steps);
    }

    public void Enter()
    {
        IsDone = false;
        _index = 0;

        if (_steps.Count == 0)
        {
            IsDone = true;
            return;
        }

        _steps[_index].Enter();
    }

    public void Tick(float dt)
    {
        if (IsDone) return;
        if (_index < 0 || _index >= _steps.Count)
        {
            IsDone = true;
            return;
        }
        
        var step = _steps[_index];
        step.Tick(dt);

        if (!step.IsDone)
        {
            return;
        }

        _index++;
        if (_index >= _steps.Count)
        {
            IsDone = true;
            return;
        }
        
        _steps[_index].Enter();
    }

    public void Cancel()
    {
        if (IsDone) return;

        if (_index >= 0 && _index < _steps.Count)
        {
            _steps[_index].Cancel();
        }

        IsDone = true;
    }
}
