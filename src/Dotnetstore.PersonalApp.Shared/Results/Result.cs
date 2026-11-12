using System.Diagnostics.CodeAnalysis;

namespace Dotnetstore.PersonalApp.Shared.Results;

// Static factories and implicit conversions are the point of a Result type; handlers return `value` or `Error` directly.
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Result pattern ergonomics.")]
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Success/Failure are the named alternates.")]
public sealed class Result<T> : IResult
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
    }

    private Result(Error error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    string? IResult.ErrorCode => _error?.Code;

    object? IResult.BoxedValue => IsSuccess ? _value : null;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
