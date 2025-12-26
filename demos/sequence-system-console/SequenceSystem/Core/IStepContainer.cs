namespace SequenceSystem.Core;

/// <summary>
/// 容器型 Step： 可以动态追加子步骤
/// </summary>
public interface IStepContainer
{
    void Add(ISequenceStep step);
    bool HasPending { get; }
}
