namespace WalletFramework.Core.Functional;

public enum TaskCompletionResult
{
    Successful,
    Error,
    Exceptional
}

public static class TaskFun
{
    public static async Task OnException(this Task task, Action<Exception> fallback)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            fallback(e);
        }
    }
    
    public static async Task<T> OnException<T>(this Task<T> task, Func<Exception, Task<T>> fallback)
    {
        try
        {
            return await task;
        }
        catch (Exception e)
        {
            return await fallback(e);
        }
    }
    
    public static async Task<T> OnException<T>(this Task<T> task, Func<Exception, T> fallback)
    {
        try
        {
            return await task;
        }
        catch (Exception e)
        {
            return fallback(e);
        }
    }
    
    public static async Task<T2> OnException<T1, T2>(
        this Task<T1> task,
        Func<T1, T2> onSuccess,
        Func<Exception, T2> fallback)
    {
        try
        {
            var t1 = await task;
            return onSuccess(t1);
        }
        catch (Exception e)
        {
            return fallback(e);
        }
    }
    
    public static async Task<TaskCompletionResult> OnException(this Task task, Func<Exception, Task> fallback)
    {
        try
        {
            await task;
            return TaskCompletionResult.Successful;
        }
        catch (Exception e)
        {
            await fallback(e);
            return TaskCompletionResult.Exceptional;
        }
    }
    
    public static async Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks) =>
        await Task.WhenAll(tasks);
}
