namespace SequenceSystem.Core;

public interface ISequenceStep
{
    string Name { get; }
    void Enter();
    void Tick(float dt);
    bool IsDone { get; }
    void Cancel();
}
