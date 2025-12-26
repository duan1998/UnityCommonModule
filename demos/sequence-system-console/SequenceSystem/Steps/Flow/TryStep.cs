using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

/// <summary>
/// 错误处理 Step：try-catch-finally 语义
/// - tryStep: 尝试执行的步骤
/// - catchStep: 出错时执行的步骤（可选）
/// - finallyStep: 无论成功失败都执行的步骤（可选）
/// </summary>
public sealed class TryStep : ISequenceStep
{
    private readonly ISequenceStep _tryStep;
    private readonly ISequenceStep? _catchStep;
    private readonly ISequenceStep? _finallyStep;
    
    private ISequenceStep? _currentStep;
    private TryState _state;
    private bool _hasError;
    private Exception? _caughtException;

    public string Name { get; }
    public bool IsDone { get; private set; }
    
    /// <summary>
    /// 获取捕获的异常（如果有）
    /// </summary>
    public Exception? CaughtException => _caughtException;
    
    /// <summary>
    /// 是否发生了错误
    /// </summary>
    public bool HasError => _hasError;

    private enum TryState
    {
        Try,
        Catch,
        Finally,
        Done
    }

    public TryStep(string name, ISequenceStep tryStep, ISequenceStep? catchStep = null, ISequenceStep? finallyStep = null)
    {
        Name = name;
        _tryStep = tryStep;
        _catchStep = catchStep;
        _finallyStep = finallyStep;
    }

    public void Enter()
    {
        IsDone = false;
        _hasError = false;
        _caughtException = null;
        _state = TryState.Try;
        
        Console.WriteLine($"[TryStep] '{Name}' Enter - starting try block");
        
        try
        {
            _currentStep = _tryStep;
            _currentStep.Enter();
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        try
        {
            switch (_state)
            {
                case TryState.Try:
                    TickTry(dt);
                    break;
                case TryState.Catch:
                    TickCatch(dt);
                    break;
                case TryState.Finally:
                    TickFinally(dt);
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private void TickTry(float dt)
    {
        if (_currentStep == null) return;
        
        _currentStep.Tick(dt);
        
        if (_currentStep.IsDone)
        {
            Console.WriteLine($"[TryStep] '{Name}' try block completed successfully");
            TransitionToFinally();
        }
    }

    private void TickCatch(float dt)
    {
        if (_currentStep == null)
        {
            TransitionToFinally();
            return;
        }
        
        _currentStep.Tick(dt);
        
        if (_currentStep.IsDone)
        {
            Console.WriteLine($"[TryStep] '{Name}' catch block completed");
            TransitionToFinally();
        }
    }

    private void TickFinally(float dt)
    {
        if (_currentStep == null)
        {
            Complete();
            return;
        }
        
        _currentStep.Tick(dt);
        
        if (_currentStep.IsDone)
        {
            Console.WriteLine($"[TryStep] '{Name}' finally block completed");
            Complete();
        }
    }

    private void HandleException(Exception ex)
    {
        _hasError = true;
        _caughtException = ex;
        
        Console.WriteLine($"[TryStep] '{Name}' caught exception: {ex.Message}");
        
        // 取消当前正在执行的 step
        _currentStep?.Cancel();
        
        // 转到 catch 块
        if (_catchStep != null)
        {
            _state = TryState.Catch;
            _currentStep = _catchStep;
            Console.WriteLine($"[TryStep] '{Name}' entering catch block");
            _currentStep.Enter();
        }
        else
        {
            TransitionToFinally();
        }
    }

    private void TransitionToFinally()
    {
        if (_finallyStep != null)
        {
            _state = TryState.Finally;
            _currentStep = _finallyStep;
            Console.WriteLine($"[TryStep] '{Name}' entering finally block");
            _currentStep.Enter();
        }
        else
        {
            Complete();
        }
    }

    private void Complete()
    {
        _state = TryState.Done;
        IsDone = true;
        Console.WriteLine($"[TryStep] '{Name}' completed (hasError={_hasError})");
    }

    public void Cancel()
    {
        Console.WriteLine($"[TryStep] '{Name}' Cancel");
        _currentStep?.Cancel();
        
        // 即使取消，也要执行 finally
        if (_state != TryState.Finally && _finallyStep != null)
        {
            _state = TryState.Finally;
            _currentStep = _finallyStep;
            _currentStep.Enter();
            // 注意：这里不等待 finally 完成，直接标记为完成
            // 如果需要等待，可以改为异步处理
        }
        
        IsDone = true;
    }
}

/// <summary>
/// 会抛出异常的测试 Step
/// </summary>
public sealed class ThrowStep : ISequenceStep
{
    private readonly string _errorMessage;
    private readonly bool _throwOnEnter;
    private readonly bool _throwOnTick;
    private int _tickCount;
    private readonly int _throwAfterTicks;

    public string Name { get; }
    public bool IsDone { get; private set; }

    public ThrowStep(string name, string errorMessage, bool throwOnEnter = true, bool throwOnTick = false, int throwAfterTicks = 0)
    {
        Name = name;
        _errorMessage = errorMessage;
        _throwOnEnter = throwOnEnter;
        _throwOnTick = throwOnTick;
        _throwAfterTicks = throwAfterTicks;
    }

    public void Enter()
    {
        _tickCount = 0;
        IsDone = false;
        
        if (_throwOnEnter)
        {
            throw new Exception(_errorMessage);
        }
    }

    public void Tick(float dt)
    {
        _tickCount++;
        
        if (_throwOnTick && _tickCount >= _throwAfterTicks)
        {
            throw new Exception(_errorMessage);
        }
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
