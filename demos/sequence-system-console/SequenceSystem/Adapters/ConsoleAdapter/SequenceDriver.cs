using System.Diagnostics;
using SequenceSystem.Core;
using SequenceSystem.Steps.Common;
using  System;

namespace SequenceSystem.Adapters.ConsoleAdapter;

public sealed class SequenceDriver
{
    private readonly SequencePlayer _player;
    private readonly WaitSignalStep _wait;

    public SequenceDriver()
    {
        _wait = new WaitSignalStep("S3-WaitSignal");
        var _steps = new ISequenceStep[]
        {
            new LogStep("S1-Log", "start"),
            new WaitSecondsStep("S2-Wait1s", 1f),
            _wait,
            new LogStep("S4-Log", "end"),
        };
        _player = new SequencePlayer(_steps, new ConsoleSequenceObserver());
    }

    public void Run()
    {
        System.Console.WriteLine("SequenceDrive: Space=continue, Esc=cancel");
        _player.Play();
        
        var sw = Stopwatch.StartNew();
        double last = sw.Elapsed.TotalSeconds;

        while (_player.IsPlaying)
        {
            while (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(true).Key;
                if (key == ConsoleKey.Spacebar) _wait.Signal();
                if (key == ConsoleKey.Escape) _player.Cancel(); 
            }

            double now = sw.Elapsed.TotalSeconds;
            float dt = (float)(now - last);
            last = now;
            
            _player.Tick(dt);
            
            Thread.Sleep(10);
        }
        
        System.Console.WriteLine("SequenceDrive: done");
    }
}
