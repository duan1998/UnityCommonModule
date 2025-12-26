namespace SequenceSystem.Core;

public interface ISequenceObserver
{
    void OnPlay(int runId, int stepCount);
    void OnStepEnter(int runId, int index, string stepName);
    void OnStepExit(int runId, int index, string stepName, double elapsedSeconds);
    void OnCompleted(int runId, double totalSeconds);
    void OnCanceled(int runId, int index, string stepName, double elapsedSeconds);
}
