using System.Text;

namespace ConsoleApp1;

public sealed class RedDotTree
{
    private readonly Dictionary<string, Node> _nodes = new();
    private readonly Dictionary<string, List<string>> _pendingChildrenByParent = new();
    private readonly Dictionary<AggregatorKind, IAggregator> _aggregators = new()
    {
        { AggregatorKind.AnyBool, new AnyBoolAggregator() },
        { AggregatorKind.Sum, new SumAggregator() },
        { AggregatorKind.Max, new MaxAggregator() },
    };

    private readonly HashSet<string> _dirty = new();
    private readonly Dictionary<string, int> _depth = new();

    // 兼容旧接口：订阅 Dot 的 int value
    private readonly Dictionary<string, List<Action<int>>> _dotSubs = new();
    private readonly Dictionary<string, int> _lastDotValue = new();

    // 新接口：订阅最终 BadgeState（SuperDot > Dot > New）
    private readonly Dictionary<string, List<Action<BadgeState>>> _badgeSubs = new();
    private readonly Dictionary<string, BadgeState> _lastBadgeState = new();

    private readonly IDispatcher _dispatcher;
    // Batch 写入：同一帧合并（按类型与节点去重，后写覆盖前写）
    private readonly Dictionary<(string id, BadgeType type), int> _pendingWrites = new();

    public RedDotTree(IDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? new ImmediateDispatcher();
    }
    
    public void RegisterNode(string id, string? parentId, AggregatorKind dotAggregator)
    {
        if (_nodes.ContainsKey(id))
            throw new InvalidOperationException($"Duplicate node id: {id}");
        
        var node = new Node(id, parentId, dotAggregator);
        _nodes.Add(id, node);
        
        _depth[id] = (!string.IsNullOrWhiteSpace(parentId) && _depth.TryGetValue(parentId, out var pd)) ? pd + 1 : 0;

        if (!string.IsNullOrEmpty(parentId))
        {
            if (_nodes.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(id);
            }
            else
            {
                if (!_pendingChildrenByParent.TryGetValue(parentId, out var pending))
                {
                    pending = new List<string>();
                    _pendingChildrenByParent.Add(parentId, pending);
                }
                pending.Add(id);
            }
        }

        if (_pendingChildrenByParent.TryGetValue(id, out var waitingChildren))
        {
            node.Children.AddRange(waitingChildren);
            _pendingChildrenByParent.Remove(id);
        }
    }

    // 兼容旧用法：把 dot 当作唯一值
    public void SetLeafValue(string id, int newDotValue)
    {
        if (!_nodes.TryGetValue(id, out var node))
            throw new KeyNotFoundException($"Node with id {id} not found");
        
        node.Self[(int)BadgeType.Dot] = newDotValue;
        MarkDirtyLimited(id, BadgeType.Dot);
    }

    // 只标脏有限祖先链（节点级、类型级传播上限）
    public void MarkDirtyLimited(string id, BadgeType type)
    {
        if (!_nodes.TryGetValue(id, out var start))
            throw new KeyNotFoundException($"Node with id {id} not found");

        var remaining = start.MaxHops[(int)type];
        var current = id;

        while (true)
        {
            if (!_nodes.TryGetValue(current, out var node))
                throw new KeyNotFoundException($"Node with id {current} not found");
            
            _dirty.Add(current);

            if (string.IsNullOrWhiteSpace(node.ParentId))
                break; 

            if (remaining == 0)
                break;

            remaining = DecRemaining(remaining);
            
            current = node.ParentId;
        }
    }

    public void Flush()
    {
        FlushAndGetChanged();
    }

    public IReadOnlyList<string> FlushAndGetChanged()
    {
        if (_dirty.Count == 0) return Array.Empty<string>();
        
        var list = _dirty.ToList();
        list.Sort((a,b) => _depth.GetValueOrDefault(b).CompareTo(_depth.GetValueOrDefault(a)));

        for (var i = 0; i < list.Count; i++)
            RecomputeSingle(list[i]);
        
        var changed = new List<string>();

        for (var i = 0; i < list.Count; i++)
        {
            var id = list[i];
            var node = _nodes[id];

            // 1) Dot int 通知（兼容）
            var dot = node.Value[(int)BadgeType.Dot];
            if (!_lastDotValue.TryGetValue(id, out var lastDot) || lastDot != dot)
            {
                _lastDotValue[id] = dot;

                if (_dotSubs.TryGetValue(id, out var dotCallbacks))
                {
                    var snapshot = dotCallbacks.ToArray();
                    for (var j = 0; j < snapshot.Length; j++)
                    {
                        var cb = snapshot[j];
                        _dispatcher.Post(() => cb(dot));
                    }
                }
            }

            // 2) BadgeState 通知 + changedIds（以 BadgeState 变化为准）
            var badge = GetBadgeState(id);
            if (!_lastBadgeState.TryGetValue(id, out var lastBadge) || lastBadge != badge)
            {
                _lastBadgeState[id] = badge;
                changed.Add(id);
                
                if (_badgeSubs.TryGetValue(id, out var badgeCallbacks))
                {
                    var snapshot = badgeCallbacks.ToArray();
                    for (var j = 0; j < snapshot.Length; j++)
                    {
                        var cb = snapshot[j];
                        _dispatcher.Post(() => cb(badge));
                    }
                }
            }
        }
        
        _dirty.Clear();
        return changed;
    } 

    private void RecomputeSingle(string id)
    {
        var node = _nodes[id];

        // Dot：按节点配置的聚合器
        RecomputeType(node, BadgeType.Dot, _aggregators[node.DotAggregator]);

        // New / Super：默认用 Max 聚合（可表达“有就亮/取最大数”）
        var maxAgg = _aggregators[AggregatorKind.Max];
        RecomputeType(node, BadgeType.New, maxAgg);
        RecomputeType(node, BadgeType.Super, maxAgg);
    }
    
    // 兼容旧接口：默认返回 Dot value
    public int GetValue(string id) => _nodes[id].Value[(int)BadgeType.Dot];

    public int GetValue(string id, BadgeType type) => _nodes[id].Value[(int)type];

    public BadgeState GetBadgeState(string id)
    {
        var node = _nodes[id];
        var super = node.Value[(int)BadgeType.Super];
        if (super > 0) return new BadgeState(BadgeKind.SuperDot, super);

        var dot = node.Value[(int)BadgeType.Dot];
        if (dot > 0) return new BadgeState(BadgeKind.Dot, dot);

        var nw = node.Value[(int)BadgeType.New];
        if (nw > 0) return new BadgeState(BadgeKind.New, nw);

        return new BadgeState(BadgeKind.None, 0);
    }

    public void Build(bool flushAll = true)
    {
        if (_pendingChildrenByParent.Count > 0)
        {
            var missing = string.Join(",", _pendingChildrenByParent.Keys);
            throw new InvalidOperationException($"Missing parent nodes: {missing}");
        }

        RebuildPaths();

        if (flushAll)
        {
            foreach (var id in _nodes.Keys)
                _dirty.Add(id);
            
            Flush();
            
            _lastDotValue.Clear();
            _lastBadgeState.Clear();
            foreach (var (id, node) in _nodes)
            {
                _lastDotValue[id] = node.Value[(int)BadgeType.Dot];
                _lastBadgeState[id] = GetBadgeState(id);
            }
        }
    }

    private void RebuildPaths()
    {
        _depth.Clear();
        
        var queue = new Queue<string>();
        foreach (var (id, node) in _nodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentId))
            {
                _depth[id] = 0;
                queue.Enqueue(id);
            }
        }
        
        // BFS
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var node = _nodes[id];
            var d = _depth[id];

            for (var i = 0; i < node.Children.Count; i++)
            {
                var childId = node.Children[i];
                _depth[childId] = d + 1;
                queue.Enqueue(childId);
            }
            
        }
        
        if (_depth.Count != _nodes.Count)
            throw new InvalidOperationException("Depth rebuild failed: graph is not a valid rooted tree (disconnected or cyclic).");
    }

    // 兼容：订阅 Dot int
    public IDisposable Subscribe(string id, Action<int> onChanged, bool fireImmediately = true)
    {
        if (!_nodes.ContainsKey(id))
            throw new KeyNotFoundException($"Node with id {id} not found");

        if (!_dotSubs.TryGetValue(id, out var list))
        {
            list = new List<Action<int>>();
            _dotSubs.Add(id, list);
        }
        
        list.Add(onChanged);

        if (fireImmediately)
            onChanged(GetValue(id));

        return new Subscription(() =>
        {
            if (_dotSubs.TryGetValue(id, out var l))
                l.Remove(onChanged);
        });
    }

    public IDisposable Subscribe(string id, SubscriptionOwner owner, Action<int> onChanged, bool fireImmediately = true)
    {
        var token = Subscribe(id, onChanged, fireImmediately);
        owner.Add(token);
        return token;
    }

    public IDisposable SubscribeBadge(string id, Action<BadgeState> onChanged, bool fireImmediately = true)
    {
        if (!_nodes.ContainsKey(id))
            throw new KeyNotFoundException($"Node with id {id} not found");

        if (!_badgeSubs.TryGetValue(id, out var list))
        {
            list = new List<Action<BadgeState>>();
            _badgeSubs.Add(id, list);
        }

        list.Add(onChanged);

        if (fireImmediately)
            onChanged(GetBadgeState(id));

        return new Subscription(() =>
        {
            if (_badgeSubs.TryGetValue(id, out var l))
                l.Remove(onChanged);
        });
    }
    
    private sealed class Subscription : IDisposable
    {
        private Action? _dispose;

        public Subscription(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null; 
        }
    }

    public IReadOnlyList<string> GetAncestors(string id)
    {
        if (!_nodes.ContainsKey(id))
            throw new KeyNotFoundException($"Node with id {id} not found");
        
        var result = new List<string>();
        var current = id;

        while (true)
        {
            result.Add(current);
            var node = _nodes[current];
            if (String.IsNullOrWhiteSpace(node.ParentId))
                break;
            
            current = node.ParentId;
        }
        
        return result;
    }

    public string DumpSubtree(string id, int maxDepth = 5)
    {
        if (!_nodes.ContainsKey(id))
            throw new KeyNotFoundException($"Node with id {id} not found");
        
        var sb = new StringBuilder();
        DumpSubtreeInternal(id, 0, maxDepth, sb);
        return sb.ToString();
    }

    private void DumpSubtreeInternal(string id, int depth, int maxDepth, StringBuilder sb)
    {
        var node = _nodes[id];
        sb.Append(' ', depth * 2);
        sb.Append(id);
        sb.Append(" dot=");
        sb.Append(node.Value[(int)BadgeType.Dot]);
        sb.Append(" new=");
        sb.Append(node.Value[(int)BadgeType.New]);
        sb.Append(" super=");
        sb.Append(node.Value[(int)BadgeType.Super]);
        sb.Append(" rem(dot/new/super)=");
        sb.Append(node.RemainingHops[(int)BadgeType.Dot]);
        sb.Append("/");
        sb.Append(node.RemainingHops[(int)BadgeType.New]);
        sb.Append("/");
        sb.Append(node.RemainingHops[(int)BadgeType.Super]);
        sb.Append(" aggDot=");
        sb.Append(node.DotAggregator);
        sb.AppendLine();

        if (depth >= maxDepth)
            return;
        
        for  (var i = 0; i < node.Children.Count; i++)
            DumpSubtreeInternal(node.Children[i], depth + 1, maxDepth, sb);
    }

    // 兼容：Dot batch 写入
    public void SetLeafValueDeferred(string id, int newDotValue)
    {
        SetStateDeferred(id, BadgeType.Dot, newDotValue);
    }

    // 新：按类型 batch 写入（同一帧覆盖）
    public void SetStateDeferred(string id, BadgeType type, int value)
    {
        _pendingWrites[(id, type)] = value;
    }

    public IReadOnlyList<string> ApplyPendingLeafWritesAndFlush() => ApplyPendingStatesAndFlush();

    public IReadOnlyList<string> ApplyPendingStatesAndFlush()
    {
        foreach (var (key, v) in _pendingWrites)
        {
            var (id, type) = key;
            var node = _nodes[id];
            var idx = (int)type;

            if (node.Self[idx] == v)
                continue;

            node.Self[idx] = v;
            MarkDirtyLimited(id, type);
        }

        _pendingWrites.Clear();
        return FlushAndGetChanged();
    }

    public void RegisterFromConfig(RedDotConfig config)
    {
        ValidateConfigOrThrow(config);

        var idSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in config.Nodes)
        {
            if (!idSet.Add(n.Id))
                throw new Exception($"Duplicate node id in config: {n.Id}");
        }
        
        foreach (var n in config.Nodes)
        {
            if (!string.IsNullOrWhiteSpace(n.ParentId) && !idSet.Contains(n.ParentId))
                throw new InvalidOperationException($"Missing parent '{n.ParentId}' for node '{n.Id}'");
        }
        
        foreach (var n in config.Nodes)
        {
            if (!Enum.TryParse<AggregatorKind>(n.Aggregator, ignoreCase: true, out var dotAgg))
                throw new InvalidOperationException($"Invalid aggregator '{n.Aggregator}' at node '{n.Id}'");

            RegisterNode(n.Id, n.ParentId, dotAgg);

            // 传播策略（默认无限）
            var node = _nodes[n.Id];
            // 先应用全局默认，再应用节点覆写
            ApplyPropagation(node, config.DefaultPropagate);
            ApplyPropagation(node, n.Propagate);
        }

        ValidateTreeOrThrow();

        Build(flushAll:true);
    }

    /// <summary>
    /// 配置级校验：尽量在建树之前给出更明确的错误信息。
    /// </summary>
    public static void ValidateConfigOrThrow(RedDotConfig config)
    {
        if (config.Nodes.Count == 0)
            throw new InvalidOperationException("Config has no nodes.");

        static void ValidateHops(string scope, RedDotPropagationConfig? p)
        {
            if (p == null) return;
            // 约定：-1 表示无限；0 表示只影响自己；>0 表示向上 N 层
            if (p.Dot is < -1) throw new InvalidOperationException($"{scope}.propagate.dot must be >= -1");
            if (p.New is < -1) throw new InvalidOperationException($"{scope}.propagate.new must be >= -1");
            if (p.Super is < -1) throw new InvalidOperationException($"{scope}.propagate.super must be >= -1");
        }

        ValidateHops("defaultPropagate", config.DefaultPropagate);
        for (int i = 0; i < config.Nodes.Count; i++)
        {
            var n = config.Nodes[i];
            if (string.IsNullOrWhiteSpace(n.Id))
                throw new InvalidOperationException($"nodes[{i}].id is empty");
            ValidateHops($"node '{n.Id}'", n.Propagate);
        }
    }

    /// <summary>
    /// 树级校验：循环/断链/多根等，给出可定位的报错信息。
    /// </summary>
    public void ValidateTreeOrThrow()
    {
        if (_pendingChildrenByParent.Count > 0)
        {
            var missing = string.Join(", ", _pendingChildrenByParent.Keys);
            throw new InvalidOperationException($"Missing parent nodes: {missing}");
        }

        // roots
        var roots = new List<string>();
        foreach (var (id, node) in _nodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentId))
                roots.Add(id);
        }
        if (roots.Count == 0)
            throw new InvalidOperationException("No root node found (all nodes have parentId).");
        // 多根不一定是错，但容易导致“入口级”语义不清，先提示成异常（你也可以改成 warning）
        if (roots.Count > 1)
            throw new InvalidOperationException($"Multiple roots found: {string.Join(", ", roots)}");

        // cycle detection (DFS color)
        var color = new Dictionary<string, int>(_nodes.Count, StringComparer.Ordinal); // 0=unvisited,1=visiting,2=done
        var stack = new List<string>();

        bool Dfs(string id)
        {
            color[id] = 1;
            stack.Add(id);
            var node = _nodes[id];
            for (int i = 0; i < node.Children.Count; i++)
            {
                var childId = node.Children[i];
                if (!_nodes.ContainsKey(childId))
                    throw new InvalidOperationException($"Broken child reference: parent '{id}' -> '{childId}' not found");

                if (!color.TryGetValue(childId, out var c)) c = 0;
                if (c == 0)
                {
                    if (Dfs(childId)) return true;
                }
                else if (c == 1)
                {
                    // build cycle path
                    var idx = stack.IndexOf(childId);
                    var cycle = idx >= 0 ? stack.GetRange(idx, stack.Count - idx) : new List<string> { childId };
                    cycle.Add(childId);
                    throw new InvalidOperationException($"Cycle detected: {string.Join(" -> ", cycle)}");
                }
            }
            stack.RemoveAt(stack.Count - 1);
            color[id] = 2;
            return false;
        }

        Dfs(roots[0]);

        // disconnected / orphaned
        if (color.Count != _nodes.Count)
        {
            var unreachable = new List<string>();
            foreach (var id in _nodes.Keys)
            {
                if (!color.TryGetValue(id, out var c) || c == 0)
                    unreachable.Add(id);
            }
            throw new InvalidOperationException($"Disconnected nodes (unreachable from root '{roots[0]}'): {string.Join(", ", unreachable)}");
        }
    }

    /// <summary>
    /// 诊断：解释某个入口当前为什么亮（优先级 Super > Dot > New），以及来自哪里。
    /// </summary>
    public string Explain(string id)
    {
        if (!_nodes.TryGetValue(id, out var node))
            throw new KeyNotFoundException($"Node with id {id} not found");

        var sb = new StringBuilder();
        sb.AppendLine($"Explain '{id}': badge={GetBadgeState(id).Kind}({GetBadgeState(id).Count})");
        sb.AppendLine($"  Dot={node.Value[(int)BadgeType.Dot]}  New={node.Value[(int)BadgeType.New]}  Super={node.Value[(int)BadgeType.Super]}");
        sb.AppendLine($"  RemHops Dot/New/Super = {node.RemainingHops[(int)BadgeType.Dot]}/{node.RemainingHops[(int)BadgeType.New]}/{node.RemainingHops[(int)BadgeType.Super]}");
        sb.AppendLine($"  MaxHops Dot/New/Super = {FmtHops(node.MaxHops[(int)BadgeType.Dot])}/{FmtHops(node.MaxHops[(int)BadgeType.New])}/{FmtHops(node.MaxHops[(int)BadgeType.Super])}");

        sb.AppendLine("  Contributors:");
        AppendContributor(sb, id, BadgeType.Super);
        AppendContributor(sb, id, BadgeType.Dot);
        AppendContributor(sb, id, BadgeType.New);
        return sb.ToString();
    }

    private void AppendContributor(StringBuilder sb, string id, BadgeType type)
    {
        var node = _nodes[id];
        var idx = (int)type;
        if (node.Value[idx] <= 0)
        {
            sb.AppendLine($"    {type}: none");
            return;
        }

        // For Max-like semantics (New/Super): find max contributor; for Dot (sum/anybool/max) still useful to show top child and self
        var self = node.Self[idx];
        var bestChildId = "";
        var bestChildVal = 0;
        var bestChildRem = 0;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = _nodes[node.Children[i]];
            if (child.RemainingHops[idx] <= 0) continue;
            if (child.Value[idx] > bestChildVal)
            {
                bestChildVal = child.Value[idx];
                bestChildId = child.Id;
                bestChildRem = child.RemainingHops[idx];
            }
        }

        sb.AppendLine($"    {type}: value={node.Value[idx]} (self={self}, maxChild={bestChildId}:{bestChildVal}, childRem={bestChildRem})");
    }

    private static string FmtHops(int hops) => hops == Node.InfiniteHops ? "INF" : hops.ToString();

    private static void ApplyPropagation(Node node, RedDotPropagationConfig? p)
    {
        if (p == null) return;

        if (p.Dot.HasValue) node.MaxHops[(int)BadgeType.Dot] = NormalizeHops(p.Dot.Value);
        if (p.New.HasValue) node.MaxHops[(int)BadgeType.New] = NormalizeHops(p.New.Value);
        if (p.Super.HasValue) node.MaxHops[(int)BadgeType.Super] = NormalizeHops(p.Super.Value);
    }

    private static int NormalizeHops(int hops)
    {
        if (hops < 0) return Node.InfiniteHops;
        return hops;
    }

    private static int DecRemaining(int remaining)
    {
        if (remaining == Node.InfiniteHops) return Node.InfiniteHops;
        if (remaining <= 0) return 0;
        return remaining - 1;
    }

    private void RecomputeType(Node node, BadgeType type, IAggregator aggregator)
    {
        var idx = (int)type;

        var childValues = new List<int>(node.Children.Count);
        var bestRemaining = 0;

        // self 贡献的传播上限
        if (node.Self[idx] > 0)
            bestRemaining = Math.Max(bestRemaining, node.MaxHops[idx]);

        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = _nodes[node.Children[i]];
            var childRem = child.RemainingHops[idx];
            if (childRem <= 0) continue; // 子节点到不了当前节点

            childValues.Add(child.Value[idx]);
            bestRemaining = Math.Max(bestRemaining, DecRemaining(childRem));
        }

        var v = aggregator.Aggregate(node.Self[idx], childValues);
        node.Value[idx] = v;
        node.RemainingHops[idx] = v > 0 ? bestRemaining : 0;
    }
}

