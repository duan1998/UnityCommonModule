using SequenceSystem.Adapters.ConsoleAdapter;

Console.WriteLine("选择要运行的 Demo:");
Console.WriteLine("1. 基础功能演示 (SequenceSystemDemo)");
Console.WriteLine("2. 高级功能演示 (AdvancedFeaturesDemo)");
Console.WriteLine("3. Facade 简化 API 演示 (FacadeDemo) ⭐ 推荐");
Console.WriteLine("4. 运行全部");
Console.Write("\n请输入 (1/2/3/4): ");

var input = Console.ReadLine()?.Trim();

switch (input)
{
    case "1":
        SequenceSystemDemo.Run();
        break;
    case "2":
        AdvancedFeaturesDemo.Run();
        break;
    case "3":
        FacadeDemo.Run();
        break;
    case "4":
    default:
        SequenceSystemDemo.Run();
        Console.WriteLine("\n按任意键继续高级功能演示...");
        Console.ReadKey();
        AdvancedFeaturesDemo.Run();
        Console.WriteLine("\n按任意键继续 Facade 演示...");
        Console.ReadKey();
        FacadeDemo.Run();
        break;
}
