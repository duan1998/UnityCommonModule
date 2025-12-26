using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class SwitchStep<TKey>:ISequenceStep where TKey: notnull
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly Func<TKey> _selector;
    private readonly Dictionary<TKey, ISequenceStep> _cases;
    private readonly ISequenceStep? _default;

    private ISequenceStep? _chosen;

    public SwitchStep(string name, Func<TKey> selector, Dictionary<TKey, ISequenceStep> cases,
        ISequenceStep? defaultStep = null)
    {
        Name = name;
        _selector = selector;
        _cases = cases;
        _default = defaultStep;
    }

    public void Enter()
    {
        IsDone = false;
        var key = _selector();

        if (!_cases.TryGetValue(key, out _chosen))
            _chosen = _default;

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
        if(_chosen.IsDone)
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
new SwitchStep<int>(
    "QuestPhase-Branch",
    selector: () => QuestData.CurrentPhase,
    cases: new Dictionary<int, ISequenceStep>
    {
        [0] = new LogStep("Phase0", "intro cutscene"),
        [1] = new LogStep("Phase1", "mid cutscene"),
        [2] = new LogStep("Phase2", "ending cutscene"),
    },
    defaultStep: new LogStep("Unknown", "fallback")
)

*/
