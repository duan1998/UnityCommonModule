using System.Text.Json.Serialization;

namespace ConsoleApp1;

public sealed class RedDotPropagationConfig
{
    // 传播上限（层数），不填表示无限
    [JsonPropertyName("dot")]
    public int? Dot { get; set; }

    [JsonPropertyName("new")]
    public int? New { get; set; }

    [JsonPropertyName("super")]
    public int? Super { get; set; }
}

public sealed class RedDotNodeConfig
{
    public string Id { get; set; } = "";

    public string? ParentId { get; set; }

    // Dot 的聚合器（Dot 走树聚合；New/Super 默认走 Max 聚合，且带 hop 限制）
    public string Aggregator { get; set; } = "AnyBool";

    // 每个节点可以对每种红点类型自定义向上传播层数（不填默认无限）
    // 例：{ "Dot": 1, "New": 0, "Super": 2 }
    public RedDotPropagationConfig? Propagate { get; set; }

    // 入口级粒度：列表项/实例级红点不建议建节点；用业务 map 判定 item 是否新，
    // 并把聚合结果（例如 NewCount）写入入口节点即可。
}

public sealed class RedDotConfig
{
    public List<RedDotNodeConfig> Nodes { get; set; } = new();

    // 全局默认传播策略（不填表示默认无限）
    // 节点上的 propagate 会覆盖这里
    [JsonPropertyName("defaultPropagate")]
    public RedDotPropagationConfig? DefaultPropagate { get; set; }
}