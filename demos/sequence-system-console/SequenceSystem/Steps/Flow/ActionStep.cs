using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class ActionStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set;}

    private readonly Action _action;

    public ActionStep(string name, Action action)
    {
        this.Name = name;
        _action = action;
    }

    public void Enter()
    {
        IsDone = false;
        _action();
        IsDone = true;
    }

    public void Tick(float dt)
    {
        
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
