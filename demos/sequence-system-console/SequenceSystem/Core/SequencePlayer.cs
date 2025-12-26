using System.Diagnostics;

namespace SequenceSystem.Core;

public sealed class SequencePlayer
{
    private readonly List<ISequenceStep> _steps;
    private readonly ISequenceObserver? _observer;
    private long _totalStartTs;
    private long _stepStartTs;
    
    private int _index = -1;
    public bool IsPlaying { get; private set; }
    public bool IsCanceled { get; private set; }
    
    public bool IsPaused { get; private set; }

    private int _runId = 0;
    public int RunId => _runId;
    
    public bool IsSkipping { get; private set; }
    
    public SequencePlayer(IEnumerable<ISequenceStep> steps, ISequenceObserver? observer = null)
    {
        _steps = new List<ISequenceStep>(steps);
        _observer =  observer;
    }

    public void Play()
    {
        if (_steps.Count <= 0) return;

        _runId++;
        
        _totalStartTs = Stopwatch.GetTimestamp();
        _observer?.OnPlay(_runId, _steps.Count);
        
        IsPlaying = true;
        IsCanceled = false;
        IsPaused = false;
        IsSkipping = false;
        
        _index = 0;
        
        _stepStartTs = Stopwatch.GetTimestamp();
        _observer?.OnStepEnter(_runId, _index, _steps[_index].Name);
        _steps[_index].Enter();
    }

    public void Tick(float dt)
    {
        if (!IsPlaying || IsCanceled || IsPaused) return;
        if (_index < 0 || _index >= _steps.Count) return;

        var step = _steps[_index];
        step.Tick(dt);
        
        if (!step.IsDone) return;

        _index++;
        if (_index >= _steps.Count)
        {
            IsPlaying = false;
            
            _observer?.OnCompleted(_runId, SecondsSince(_totalStartTs));
            
            return;
        }
        
        _stepStartTs = Stopwatch.GetTimestamp();
        _observer?.OnStepEnter(_runId, _index, _steps[_index].Name);
        _steps[_index].Enter();
    }

    public void Cancel()
    {
        if (!IsPlaying) return;
        
        IsCanceled = true;
        if (_index >= 0 && _index < _steps.Count)
        {
            _steps[_index].Cancel();
            _observer?.OnCanceled(_runId, _index, _steps[_index].Name, SecondsSince(_stepStartTs));
        }
        
        IsPlaying = false;
        
    }

    public bool IsActiveRun(int capturedRunId)
    {
        return IsPlaying && !IsCanceled && capturedRunId == _runId;
    }

    public static double SecondsSince(long startTimestamp)
        => (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;


    public void Pause()
    {
        if (!IsPlaying || IsCanceled) return;
        IsPaused = true;
    }

    public void Resume()
    {
        if (!IsPlaying || IsCanceled) return;
        IsPaused = false;
    }

    public void Skip()
    {
        if (!IsPlaying || IsCanceled) return;
        IsSkipping = true;
    }
}
