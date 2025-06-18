using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace LinuxDedicatedServer.Api;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the status of the result.
    /// </summary>
    Result.State Status { get; }

    /// <summary>
    /// Gets the message associated with the result.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the exception associated with the result, if any.
    /// </summary>
    Exception Exception { get; }
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>
    /// Gets the value associated with the result.
    /// </summary>
    T Value { get; }
}

/// <summary>
/// Represents a non-generic result of an operation.
/// </summary>
public readonly struct Result : IResult
{
    /// <inheritdoc/>
    public bool IsSuccess { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public State Status { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public string Message { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public Exception Exception { [DebuggerStepperBoundary] get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct with the specified state.
    /// </summary>
    /// <param name="state">The result state.</param>
    [DebuggerStepperBoundary]
    private Result(Result.State state)
    {
        IsSuccess = state == State.Success;
        Status = state;
        Message = string.Empty;
        Exception = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct with a validation fault message.
    /// </summary>
    /// <param name="message">The validation fault message.</param>
    [DebuggerStepperBoundary]
    private Result(string message)
    {
        IsSuccess = false;
        Status = State.ValidationFault;
        Message = message;
        Exception = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    [DebuggerStepperBoundary]
    private Result(Exception exception)
    {
        IsSuccess = false;
        Status = State.Exception;
        Message = string.Empty;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct from another <see cref="IResult"/>.
    /// </summary>
    /// <param name="result">The result to copy.</param>
    [DebuggerStepperBoundary]
    private Result(IResult result)
    {
        IsSuccess = result.IsSuccess;
        Status = result.Status;
        Message = result.Message;
        Exception = result.Exception;
    }

    /// <summary>
    /// Creates a successful <see cref="Result"/>.
    /// </summary>
    /// <returns>A successful result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Ok() => new Result(State.Success);

    /// <summary>
    /// Creates a failed <see cref="Result"/> with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Fail(Exception exception) => new Result(exception);

    /// <summary>
    /// Creates a failed <see cref="Result"/> with a message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Fail(string message) => new Result(message);

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with a value.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with a message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed result with a message.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Fail<T>(string message) => Result<T>.Fail(message);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with an exception.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed result with an exception.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Fail<T>(Exception exception) => Result<T>.Fail(exception);

    /// <summary>
    /// Forwards a failed <see cref="IResult"/> as a <see cref="Result"/>.
    /// </summary>
    /// <param name="result">The result to forward.</param>
    /// <returns>A forwarded failed result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result ForwardFail(IResult result) => new Result(result);

    /// <summary>
    /// Forwards a failed <see cref="IResult"/> as a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to forward.</param>
    /// <returns>A forwarded failed result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ForwardFail<T>(IResult result) => Result<T>.ForwardFail(result);

    /// <summary>
    /// Executes an action and captures any exception as a failed result.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A successful or failed result.</returns>
    [DebuggerStepperBoundary]
    public static Result Capture(Action action)
    {
        try
        {
            action();
            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
    }

    /// <summary>
    /// Executes a function and captures any exception as a failed result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A successful or failed result with a value.</returns>
    [DebuggerStepperBoundary]
    public static Result<T> Capture<T>(Func<T> func)
    {
        try
        {
            return Result.Ok(func());
        }
        catch (Exception e)
        {
            return Result.Fail<T>(e);
        }
    }

    /// <summary>
    /// Executes an asynchronous action and captures any exception as a failed result.
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a successful or failed result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
    }

    /// <summary>
    /// Executes an asynchronous function and captures any exception as a failed result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a successful or failed result with a value.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<T>> CaptureAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return Result.Ok(await func());
        }
        catch (Exception e)
        {
            return Result.Fail<T>(e);
        }
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string describing the result.</returns>
    public override string ToString()
    {
        return Status switch
        {
            State.Success => $"Success",
            State.ValidationFault => $"Validation Failed: {Message}",
            State.Exception => $"Exception: {Exception.GetType().Name} - {Exception.Message}",
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Represents the state of a <see cref="Result"/>.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// The operation was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The operation failed due to an exception.
        /// </summary>
        Exception,

        /// <summary>
        /// The operation failed due to a validation fault.
        /// </summary>
        ValidationFault,
    }
}

/// <summary>
/// Represents a result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct Result<T> : IResult<T>
{
    /// <inheritdoc/>
    public bool IsSuccess { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public Result.State Status { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public string Message { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public Exception Exception { [DebuggerStepperBoundary] get; }

    /// <inheritdoc/>
    public T Value { [DebuggerStepperBoundary] get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    [DebuggerStepperBoundary]
    private Result(T value)
    {
        IsSuccess = true;
        Status = Result.State.Success;
        Message = string.Empty;
        Exception = default!;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct with a validation fault message.
    /// </summary>
    /// <param name="message">The validation fault message.</param>
    [DebuggerStepperBoundary]
    private Result(string message)
    {
        IsSuccess = false;
        Status = Result.State.ValidationFault;
        Message = message;
        Exception = default!;
        Value = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    [DebuggerStepperBoundary]
    private Result(Exception exception)
    {
        IsSuccess = false;
        Status = Result.State.Exception;
        Message = string.Empty;
        Exception = exception;
        Value = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct from another <see cref="IResult"/>.
    /// </summary>
    /// <param name="result">The result to copy.</param>
    [DebuggerStepperBoundary]
    private Result(IResult result)
    {
        IsSuccess = result.IsSuccess;
        Status = result.Status;
        Message = result.Message;
        Exception = result.Exception;
        Value = default!;
    }

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with a value.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Ok(T value) => new Result<T>(value);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with a message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed result with a message.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Fail(string message) => new Result<T>(message);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed result with an exception.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Fail(Exception exception) => new Result<T>(exception);

    /// <summary>
    /// Forwards a failed <see cref="IResult"/> as a <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="result">The result to forward.</param>
    /// <returns>A forwarded failed result.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ForwardFail(IResult result) => new Result<T>(result);

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a non-generic <see cref="Result"/>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Result(Result<T> result) => Result.ForwardFail(result);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string describing the result.</returns>
    public override string ToString()
    {
        return Status switch
        {
            Result.State.Success => $"Success: {Value!.GetType().Name}",
            Result.State.ValidationFault => $"Validation Failed: {Message}",
            Result.State.Exception => $"Exception: {Exception.GetType().Name} - {Exception.Message}",
            _ => throw new NotImplementedException(),
        };
    }
}

/// <summary>
/// Provides extension methods for working with <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public static class ResultExtension
{
    /// <summary>
    /// Executes a function if the result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A new result.</returns>
    [DebuggerStepperBoundary]
    public static Result<TResult> Then<TResult>(this Result result, Func<TResult> func)
    {
        if (result.IsSuccess)
        {
            return Result.Ok(func());
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes a function if the result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A new result.</returns>
    [DebuggerStepperBoundary]
    public static Result<TResult> Then<T, TResult>(this Result<T> result, Func<T, TResult> func)
    {
        if (result.IsSuccess)
        {
            return Result.Ok(func(result.Value));
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes an asynchronous function if the result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<TResult>(this Result result, Func<Task<TResult>> func)
    {
        if (result.IsSuccess)
        {
            return Result.Ok(await func());
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes an asynchronous function if the result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<T, TResult>(this Result<T> result, Func<T, Task<TResult>> func)
    {
        if (result.IsSuccess)
        {
            return Result.Ok(await func(result.Value));
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes a function if the awaited result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<TResult>(this Task<Result> resultTask, Func<TResult> func)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Ok(func());
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes a function if the awaited result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<T, TResult>(this Task<Result<T>> resultTask, Func<T, TResult> func)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Ok(func(result.Value));
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes an asynchronous function if the awaited result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<TResult>(this Task<Result> resultTask, Func<Task<TResult>> func)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Ok(await func());
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes an asynchronous function if the awaited result is successful, otherwise forwards the failure.
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>A task representing the asynchronous operation, with a new result.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<TResult>> ThenAsync<T, TResult>(this Task<Result<T>> resultTask, Func<T, Task<TResult>> func)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Ok(await func(result.Value));
        }

        return Result.ForwardFail<TResult>(result);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    [DebuggerStepperBoundary]
    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    [DebuggerStepperBoundary]
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action if the result is successful.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result> TapAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
        {
            await action();
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action if the result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an action if the awaited result is successful.
    /// </summary>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result> TapAsync(this Task<Result> resultTask, Action action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Executes an action if the awaited result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action if the awaited result is successful.
    /// </summary>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result> TapAsync(this Task<Result> resultTask, Func<Task> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            await action();
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action if the awaited result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>The original result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            await action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Maps the values of a successful result sequence to a new type.
    /// </summary>
    /// <typeparam name="T">The type of the input values.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    /// <param name="result">The result containing a sequence of values.</param>
    /// <param name="func">The mapping function.</param>
    /// <returns>A new result with mapped values.</returns>
    [DebuggerStepperBoundary]
    public static Result<IEnumerable<TResult>> Map<T, TResult>(this Result<IEnumerable<T>> result, Func<T, TResult> func)
    {
        if (result.IsSuccess)
        {
            return Result.Ok(result.Value.Select(func));
        }

        return Result.ForwardFail<IEnumerable<TResult>>(result);
    }

    /// <summary>
    /// Maps the values of a successful awaited result sequence to a new type.
    /// </summary>
    /// <typeparam name="T">The type of the input values.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    /// <param name="resultTask">The result task containing a sequence of values.</param>
    /// <param name="func">The mapping function.</param>
    /// <returns>A new result with mapped values as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<IEnumerable<TResult>>> MapAsync<T, TResult>(this Task<Result<IEnumerable<T>>> resultTask, Func<T, TResult> func)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Ok(result.Value.Select(func));
        }

        return Result.ForwardFail<IEnumerable<TResult>>(result);
    }

    /// <summary>
    /// Gets the value if the result is successful, otherwise returns the default value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="defaultValue">The default value to return if not successful.</param>
    /// <returns>The value or the default value.</returns>
    [DebuggerStepperBoundary]
    public static T GetOrDefault<T>(this IResult<T> result, T defaultValue = default!)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the value if the awaited result is successful, otherwise returns the default value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="defaultValue">The default value to return if not successful.</param>
    /// <returns>The value or the default value as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<T> GetOrDefaultAsync<T>(this Task<Result<T>> resultTask, T defaultValue = default!)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return result.Value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the value if the result is successful, otherwise throws an exception.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">Thrown if the result is not successful and not an exception.</exception>
    [DebuggerStepperBoundary]
    public static T GetOrThrow<T>(this IResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        if (result.Status == Result.State.Exception)
        {
            ExceptionDispatchInfo.Capture(result.Exception).Throw();
        }

        throw new ArgumentException(result.Message);
    }

    /// <summary>
    /// Gets the value if the awaited result is successful, otherwise throws an exception.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <returns>The value as a task.</returns>
    /// <exception cref="ArgumentException">Thrown if the result is not successful and not an exception.</exception>
    [DebuggerStepperBoundary]
    public static async Task<T> GetOrThrowAsync<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return result.Value;
        }

        if (result.Status == Result.State.Exception)
        {
            ExceptionDispatchInfo.Capture(result.Exception).Throw();
        }

        throw new ArgumentException(result.Message);
    }

    /// <summary>
    /// Determines whether the awaited result is successful.
    /// </summary>
    /// <param name="resultTask">The result task.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<bool> IsSuccessAsync(this Task<Result> resultTask)
    {
        return (await resultTask).IsSuccess;
    }

    /// <summary>
    /// Determines whether the awaited result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<bool> IsSuccessAsync<T>(this Task<Result<T>> resultTask)
    {
        return (await resultTask).IsSuccess;
    }

    /// <summary>
    /// Ensures that the value of a successful result is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>A successful result if the value is not null; otherwise, a failed result.</returns>
    [DebuggerStepperBoundary]
    public static Result<T> EnsureNotNull<T>(this Result<T?> result)
    {
        if (result.IsSuccess)
        {
            if (result.Value is not null)
            {
                return Result.Ok(result.Value);
            }

            return Result.Fail<T>($"Result<{typeof(T).Name}> is null");
        }

        return Result.ForwardFail<T>(result);
    }

    /// <summary>
    /// Ensures that the value of a successful awaited result is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <returns>A successful result if the value is not null; otherwise, a failed result as a task.</returns>
    [DebuggerStepperBoundary]
    public static async Task<Result<T>> EnsureNotNullAsync<T>(this Task<Result<T?>> resultTask)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            if (result.Value is not null)
            {
                return Result.Ok(result.Value);
            }

            return Result.Fail<T>($"Result<{typeof(T).Name}> is null");
        }

        return Result.ForwardFail<T>(result);
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a non-generic <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <returns>The non-generic result as a task.</returns>
    [DebuggerStepperBoundary, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> StripGenericAsync<T>(this Task<Result<T>> resultTask)
    {
        return (Result) (await resultTask);
    }
}