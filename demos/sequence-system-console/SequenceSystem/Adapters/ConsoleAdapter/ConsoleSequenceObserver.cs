using SequenceSystem.Core;

namespace SequenceSystem.Adapters.ConsoleAdapter;

public class ConsoleSequenceObserver:ISequenceObserver
{
    public void OnPlay(int runId, int stepCount)
    {
        System.Console.WriteLine($"[Obs] play runId={runId}, steps={stepCount}");
    }

    public void OnStepEnter(int runId, int index, string stepName)
    {
        System.Console.WriteLine($"[Obs] enter runId={runId}, i={index}, step={stepName}");
    }

    public void OnStepExit(int runId, int index, string stepName, double elapsedSeconds)
    {
        System.Console.WriteLine($"[Obs] exit runId={runId}, i={index}, step={stepName}, cost={elapsedSeconds:0.000}s");
    }

    public void OnCompleted(int runId, double totalSeconds)
    {
        System.Console.WriteLine($"[Obs] completed runId={runId}, total={totalSeconds:0.000}s");
    }

    public void OnCanceled(int runId, int index, string stepName, double elapsedSeconds)
    {
        System.Console.WriteLine($"[Obs] canceled runId={runId}, i={index}, step={stepName}, cost={elapsedSeconds:0.000}s");
    }
}
