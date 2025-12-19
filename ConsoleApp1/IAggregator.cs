namespace ConsoleApp1;

public enum BadgeType
{
    Dot = 0,
    New = 1,
    Super = 2
}

public enum BadgeKind
{
    None = 0,
    New = 1,
    Dot = 2,
    SuperDot = 3
}

public readonly record struct BadgeState(BadgeKind Kind, int Count);

public enum AggregatorKind
{
    AnyBool = 0,
    Sum = 1,
    Max = 2,
}

public interface IAggregator
{
    int Aggregate(int selfValue, IReadOnlyList<int> childValues);
}

public sealed class AnyBoolAggregator : IAggregator
{
    public int Aggregate(int selfValue, IReadOnlyList<int> childValues)
    {
        if (selfValue > 0) return 1;
        for (int i = 0; i < childValues.Count; i++)
        {
            if (childValues[i] > 0) return 1;
        }

        return 0;
    }
}

public sealed class SumAggregator : IAggregator
{
    public int Aggregate(int selfValue, IReadOnlyList<int> childValues)
    {
        var sum = selfValue;
        for (int i = 0; i < childValues.Count; i++)
        {
            sum += childValues[i];
        }

        return sum;
    }
}

public sealed class MaxAggregator : IAggregator
{
    public int Aggregate(int selfValue, IReadOnlyList<int> childValues)
    {
        var max = selfValue;
        for (int i = 0; i < childValues.Count; i++)
        {
            if (childValues[i] > max) max = childValues[i];
        }

        return max;
    }
}