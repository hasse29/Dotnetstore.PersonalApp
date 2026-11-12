namespace Dotnetstore.PersonalApp.Shared.Results;

/// <summary>What telemetry needs to know about any <see cref="Result{T}"/> without knowing its value type.</summary>
public interface IResult
{
    bool IsSuccess { get; }

    /// <summary>The <see cref="Error.Code"/> of a failed result; null when it succeeded.</summary>
    string? ErrorCode { get; }

    /// <summary>The value of a successful result; null when it failed. Only used to find the id of the entity an operation produced.</summary>
    object? BoxedValue { get; }
}
