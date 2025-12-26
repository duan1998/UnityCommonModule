using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public sealed class FakeAsyncStep: ISequenceStep
{
    public string Name {get; }
    public bool IsDone {get; private set; }

    private readonly SequencePlayer _player;
    private readonly int _delayMs;
    private int _capturedRunId;

    public FakeAsyncStep(string name, SequencePlayer player, int delayMs)
    {
        Name = name;
        _player = player;
        _delayMs = delayMs;
    }

    public void Enter()
    {
        IsDone = false;
        _capturedRunId = _player.RunId;

        _ = RunAsync(_capturedRunId);
    }

    private async Task RunAsync(int runId)
    {
        await Task.Delay(_delayMs);

        if (!_player.IsActiveRun(runId))
            return;
        
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
