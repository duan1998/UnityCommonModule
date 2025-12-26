using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class ConditionStep: ISequenceStep
{
    public string Name {get;}
    public bool IsDone { get; private set; }
    
    private readonly Func<bool> _condition;
    private readonly ISequenceStep _ifTrue;
    private readonly ISequenceStep? _ifFalse;
    
    private ISequenceStep? _chosen;

    public ConditionStep(string name, Func<bool> condition, ISequenceStep ifTrue, ISequenceStep? ifFalse = null)
    {
        Name = name;
        _condition = condition;
        _ifTrue = ifTrue;
        _ifFalse = ifFalse;
    }

    public void Enter()
    {
        IsDone = false;
        _chosen = _condition()? _ifTrue : _ifFalse;

        if (_chosen == null)
        {
            IsDone = true;
            return;
        }
        
        _chosen.Enter();
    }

    public void Tick(float dt)
    {
        if (IsDone)
            return;

        if (_chosen == null)
        {
            IsDone = true;
            return;
        }
        
        _chosen.Tick(dt);
        if (_chosen.IsDone)
            IsDone = true;
    }

    public void Cancel()
    {
        if (IsDone)
            return;
        
        _chosen?.Cancel();
        IsDone = true;
    }
}

/*

new ConditionStep(
    "Check-FirstTime",
    condition: () => PlayerData.IsFirstVisit,
    ifTrue: new SequenceStep("Tutorial", new ISequenceStep[] { /* 新手引导步骤 * / }),
    ifFalse: new LogStep("Skip-Tutorial", "not first time, skip")
)

*/
