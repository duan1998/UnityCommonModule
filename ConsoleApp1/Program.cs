using ConsoleApp1;
using ConsoleApp1.System;

static void PrintBadge(RedDotTree tree, string id)
{
    var b = tree.GetBadgeState(id);
    Console.WriteLine($"{id,-14} badge={b.Kind} count={b.Count}  dot={tree.GetValue(id, BadgeType.Dot)} new={tree.GetValue(id, BadgeType.New)} super={tree.GetValue(id, BadgeType.Super)}");
}

var path = Path.Combine(AppContext.BaseDirectory, "Configs", "reddot.json");
var json = File.ReadAllText(path);
var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var cfg = System.Text.Json.JsonSerializer.Deserialize<RedDotConfig>(json, options)
          ?? throw new Exception($"Invalid config: {path}");

var tree = new RedDotTree();
tree.RegisterFromConfig(cfg);

// 订阅入口（用 BadgeState，看最终展示）
var owner = new SubscriptionOwner();
tree.SubscribeBadge("Root", b => Console.WriteLine($"[Notify] Root => {b.Kind}({b.Count})"), fireImmediately: true);
tree.SubscribeBadge("Mail", b => Console.WriteLine($"[Notify] Mail => {b.Kind}({b.Count})"), fireImmediately: true);
tree.SubscribeBadge("Mail_Friend", b => Console.WriteLine($"[Notify] Mail_Friend => {b.Kind}({b.Count})"), fireImmediately: true);

Console.WriteLine("=== Initial subtree ===");
Console.WriteLine(tree.DumpSubtree("Root"));

Console.WriteLine("=== Demo: node-specific hop limits on Mail_Friend (Dot=1, New=0, Super=2) ===");

// 同一帧内写入多个类型的状态（batch）
tree.SetStateDeferred("Mail_Friend", BadgeType.Dot, 5);
tree.SetStateDeferred("Mail_Friend", BadgeType.New, 1);
tree.SetStateDeferred("Mail_Friend", BadgeType.Super, 7);

var changed = tree.ApplyPendingStatesAndFlush();
Console.WriteLine("Changed: " + string.Join(",", changed));

PrintBadge(tree, "Mail_Friend"); // 自己：Super 优先
PrintBadge(tree, "Mail");        // Dot 可到 Mail，Super 可到 Mail
PrintBadge(tree, "Root");        // Dot 不到 Root（Dot=1），Super 可到 Root（Super=2），New 不到父（New=0）

Console.WriteLine("=== Subtree after flush ===");
Console.WriteLine(tree.DumpSubtree("Root"));

Console.WriteLine();
Console.WriteLine("NOTE: 粒度收敛到入口级。列表项/实例级（例如背包格子）的【新】通常由业务层维护 map 判定，");
Console.WriteLine("入口级只写入聚合结果（例如 Inventory.NewCount / Mail.NewCount），再由 RedDotTree 负责向上传播与通知。");

