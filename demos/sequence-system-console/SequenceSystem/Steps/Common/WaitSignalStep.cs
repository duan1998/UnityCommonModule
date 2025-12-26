using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public sealed class WaitSignalStep  : ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    public WaitSignalStep(string name)
    {
        Name = name;
    }

    public void Enter()
    {
        Console.WriteLine($"[Step] {Name}: press Space to continue");
    }

    public void Tick(float dt)
    {
    }

    public void Cancel()
    {
        IsDone = true;
    }

    public void Signal()
    {
        IsDone = true;
    }
}
