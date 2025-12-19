namespace ConsoleApp1;

public sealed class Node
{
    // 统一支持三种红点状态：Dot/New/Super
    // - Self: 业务写入的“本节点事实值”
    // - Value: 本节点最终展示用的聚合值（来自 Self + 可传播到本节点的子节点）
    // - Remaining: 本节点的 Value 对上级还能继续向上影响的剩余层数（0 表示到此为止）
    // - MaxHops: 本节点 Self 产生的影响最多向上传递多少层（默认无限）
    public const int TypeCount = 3;
    public const int InfiniteHops = int.MaxValue;

    public Node(string id, string? parentId, AggregatorKind dotAggregator)
    {
        Id = id;
        ParentId = parentId;
        DotAggregator = dotAggregator;
    }
    
    public string Id { get; }
    public string? ParentId { get; }

    public List<string> Children { get; } = new();

    // index: (int)BadgeType
    public int[] Self { get; } = new int[TypeCount];
    public int[] Value { get; } = new int[TypeCount];
    public int[] RemainingHops { get; } = new int[TypeCount];

    // 本节点 Self 的传播上限（默认无限）
    public int[] MaxHops { get; } = { InfiniteHops, InfiniteHops, InfiniteHops };

    public AggregatorKind DotAggregator { get; }
}