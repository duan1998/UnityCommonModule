using SequenceSystem.Core;
using SequenceSystem.Steps.Common;
using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Adapters.ConsoleAdapter;

public static class EnterHouseDemo
{
    public static void Run()
    {
        // 场景数据
        var door = new SimDoor("前门", isOpen: true); // 默认关门
        // 编排演出
        var cutscene = new SequenceStep("进门拿锄头", new ISequenceStep[]
        {
            // 走到门口
            new SimMoveStep("S1-走到门口", "角色A", "门口", 1f),
            
            // 如果门没开 -> 开门（并行：角色推门 + 门打开
            new ConditionStep(
                "S2-检查门",
                condition: ()=> !door.IsOpen,
                ifTrue: new ParallelStep("角色推门", ParallelWaitMode.WaitAll, new ISequenceStep[]
                {
                    new SimAnimStep("角色推门", "角色A", "PushDoor", duration:0.8f),
                    new SimDoorStep("门打开", door, true, 0.8f),
                })),
            
            // 进门
            new SimMoveStep("S3-进门", "角色A", "屋内", 1f),
            
            // 拿锄头 （动画 + 装备
            new SequenceStep("S4-拿锄头", new ISequenceStep[]
            {
                new SimAnimStep("抓取动画", "角色A", "GrabHoe", 0.5f),
                new ActionStep("装备锄头", () => Console.WriteLine($"[Equip] 角色A 装备了锄头")),
            }),
            
            // 转身
            new SimAnimStep("S5-转身", "角色A", "TurnAround", 0.3f),
            
            // 走出去
            new SimMoveStep("S6-出门", "角色A", "门外",1),
            
            // 结束 
            new ActionStep("S7-完成", () => Console.WriteLine("[Cutscene] 演出完成！")),
        });

        var player = new SequencePlayer(new[] { cutscene }, new ConsoleSequenceObserver());
        
        Console.WriteLine("============演出开始============");
        player.Play();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        double last = 0;

        while (player.IsPlaying)
        {
            double now = sw.Elapsed.TotalMilliseconds;
            float dt = (float)(now - last);
            last = now;
            
            player.Tick(dt);
            Thread.Sleep(16); // ~60fps
        }
        
        Console.WriteLine("============演出结束============");
    }
}
