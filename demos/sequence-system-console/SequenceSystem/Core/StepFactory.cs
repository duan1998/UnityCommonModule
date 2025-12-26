using System.Text.Json;
using System.Text.Json.Serialization;
using SequenceSystem.Steps.Common;
using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Core;

/// <summary>
/// Step 配置（JSON 反序列化用）
/// </summary>
public class StepConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("seconds")]
    public float? Seconds { get; set; }
    
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }
    
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
    
    [JsonPropertyName("steps")]
    public List<StepConfig>? Steps { get; set; }
    
    [JsonPropertyName("ifTrue")]
    public List<StepConfig>? IfTrue { get; set; }
    
    [JsonPropertyName("ifFalse")]
    public List<StepConfig>? IfFalse { get; set; }
    
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
}

/// <summary>
/// 序列配置（JSON 反序列化用）
/// </summary>
public class SequenceConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Unnamed";
    
    [JsonPropertyName("steps")]
    public List<StepConfig> Steps { get; set; } = new();
}

/// <summary>
/// Step 工厂：从配置创建 Step 实例
/// 支持数据驱动的流程定义
/// </summary>
public class StepFactory
{
    private readonly Dictionary<string, Func<bool>> _conditionRegistry = new();
    private readonly Dictionary<string, Func<StepConfig, ISequenceStep>> _customStepCreators = new();

    /// <summary>
    /// 注册条件（用于 ConditionStep）
    /// </summary>
    public void RegisterCondition(string key, Func<bool> condition)
    {
        _conditionRegistry[key] = condition;
    }

    /// <summary>
    /// 注册自定义 Step 创建器
    /// </summary>
    public void RegisterStepCreator(string type, Func<StepConfig, ISequenceStep> creator)
    {
        _customStepCreators[type] = creator;
    }

    /// <summary>
    /// 从 JSON 字符串解析序列配置
    /// </summary>
    public SequenceConfig ParseConfig(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<SequenceConfig>(json, options) 
               ?? throw new InvalidOperationException("Failed to parse sequence config");
    }

    /// <summary>
    /// 从配置创建 Step 列表
    /// </summary>
    public List<ISequenceStep> CreateSteps(SequenceConfig config)
    {
        return config.Steps.Select(CreateStep).ToList();
    }

    /// <summary>
    /// 从 JSON 字符串直接创建 Step 列表
    /// </summary>
    public List<ISequenceStep> CreateStepsFromJson(string json)
    {
        var config = ParseConfig(json);
        return CreateSteps(config);
    }

    /// <summary>
    /// 从单个配置创建 Step
    /// </summary>
    public ISequenceStep CreateStep(StepConfig config)
    {
        var name = config.Name ?? config.Type;
        
        // 先检查自定义创建器
        if (_customStepCreators.TryGetValue(config.Type, out var creator))
        {
            return creator(config);
        }

        return config.Type.ToLower() switch
        {
            "log" => new LogStep(name, config.Message ?? ""),
            
            "delay" => new DelayStep(name, config.Seconds ?? 1f),
            
            "wait" => new WaitSecondsStep(name, config.Seconds ?? 1f),
            
            "load" => new AsyncLoadStep(name, config.Resource ?? "unknown", config.Seconds ?? 1f),
            
            "sequence" => CreateSequenceStep(name, config),
            
            "parallel" => CreateParallelStep(name, config),
            
            "condition" => CreateConditionStep(name, config),
            
            "action" => new ActionStep(name, () => Console.WriteLine($"[Action] {config.Message}")),
            
            _ => throw new NotSupportedException($"Unknown step type: {config.Type}")
        };
    }

    private ISequenceStep CreateSequenceStep(string name, StepConfig config)
    {
        if (config.Steps == null || config.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Sequence step '{name}' must have steps");
        }
        
        var childSteps = config.Steps.Select(CreateStep).ToArray();
        return new SequenceStep(name, childSteps);
    }

    private ISequenceStep CreateParallelStep(string name, StepConfig config)
    {
        if (config.Steps == null || config.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Parallel step '{name}' must have steps");
        }
        
        var childSteps = config.Steps.Select(CreateStep).ToArray();
        var mode = config.Mode?.ToLower() == "any" 
            ? ParallelWaitMode.WaitAny 
            : ParallelWaitMode.WaitAll;
        
        return new ParallelStep(name, mode, childSteps);
    }

    private ISequenceStep CreateConditionStep(string name, StepConfig config)
    {
        if (string.IsNullOrEmpty(config.Condition))
        {
            throw new InvalidOperationException($"Condition step '{name}' must have a condition key");
        }

        Func<bool> condition;
        if (_conditionRegistry.TryGetValue(config.Condition, out var registered))
        {
            condition = registered;
        }
        else
        {
            // 默认返回 true
            Console.WriteLine($"[Warning] Condition '{config.Condition}' not registered, defaulting to true");
            condition = () => true;
        }

        ISequenceStep? ifFalse = null;

        // ifTrue 是必需的，如果没有配置则使用空的 ActionStep
        ISequenceStep ifTrue;
        if (config.IfTrue != null && config.IfTrue.Count > 0)
        {
            var trueSteps = config.IfTrue.Select(CreateStep).ToArray();
            ifTrue = trueSteps.Length == 1 ? trueSteps[0] : new SequenceStep($"{name}_true", trueSteps);
        }
        else
        {
            ifTrue = new ActionStep($"{name}_true_empty", () => { });
        }

        if (config.IfFalse != null && config.IfFalse.Count > 0)
        {
            var falseSteps = config.IfFalse.Select(CreateStep).ToArray();
            ifFalse = falseSteps.Length == 1 ? falseSteps[0] : new SequenceStep($"{name}_false", falseSteps);
        }

        return new ConditionStep(name, condition, ifTrue, ifFalse);
    }
}

/// <summary>
/// Step 工厂扩展方法
/// </summary>
public static class StepFactoryExtensions
{
    /// <summary>
    /// 从 JSON 文件加载序列
    /// </summary>
    public static List<ISequenceStep> LoadFromFile(this StepFactory factory, string filePath)
    {
        var json = File.ReadAllText(filePath);
        return factory.CreateStepsFromJson(json);
    }
}
