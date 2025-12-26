using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

public sealed class RepeatStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly Func<ISequenceStep> _stepFactory;
    private readonly Func<bool> _untilCondition;
    private readonly int _maxIterations;
    
    private ISequenceStep? _current;
    private int _iteration;

    public RepeatStep(string name, Func<ISequenceStep> stepFactory, Func<bool> untilCondition,
        int maxIterations)
    {
        Name = name;
        _stepFactory = stepFactory;
        _untilCondition = untilCondition;
        _maxIterations = maxIterations;
    }
    
    public void Enter()
    {
        IsDone = false;
        _iteration = 0;

        if (_untilCondition())
        {
            IsDone = true;
            return;
        }
        
        _current = _stepFactory();
        _current.Enter();
    }

    public void Tick(float dt)
    {
        if (IsDone)
            return;

        if (_current != null && !_current.IsDone) 
            _current.Tick(dt);

        if (_current == null || !_current.IsDone)
            return;

        _iteration++;

        if (_untilCondition() || _iteration >= _maxIterations)
        {
            IsDone = true;
            return;
        }
        
        _current = _stepFactory();
        _current.Enter();
    }

    public void Cancel()
    {
        if (IsDone)
            return;
        
        _current?.Cancel();
        IsDone = true;
    }
}

/*


// 每秒检查一次，直到资源加载完成
new RepeatStep(
    "WaitResourceReady",
    stepFactory: () => new DelayStep("Check", 1f),
    untilCondition: () => ResourceManager.IsReady("cutscene_assets")
)

*/
