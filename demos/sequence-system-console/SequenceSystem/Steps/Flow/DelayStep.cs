using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class DelayStep: ISequenceStep
{
    public string Name {get;}
    public bool IsDone {get; private set;}

    private readonly float _duration;
    private float _remain;

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
        if(IsDone)
            return;
        
        _remain -= dt;
        if (_remain <= 0)
            IsDone = true;
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
