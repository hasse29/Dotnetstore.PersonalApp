using System.Collections.Concurrent;
using System.Reflection;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Audit;

/// <summary>
/// Entry point for the audit log. The Host connects the sink and the signed-in user once at startup; until then (and in handler
/// tests) recording does nothing. Every handler is recorded through <c>AppTelemetry.TraceAsync</c>, other actions call
/// <see cref="Record"/> themselves.
/// </summary>
public static class AuditTrail
{
    private static readonly ConcurrentDictionary<Type, RequestShape> Shapes = new();

    public static IAuditSink? Sink { get; set; }

    public static Func<AuthenticatedUser?> CurrentUser { get; set; } = static () => null;

    public static TimeProvider Clock { get; set; } = TimeProvider.System;

    public static void Record(
        string operation,
        AuditKind kind,
        bool succeeded,
        string? errorCode = null,
        Guid? entityId = null,
        IReadOnlyList<string>? fields = null,
        double elapsedMs = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var sink = Sink;
        if (sink is null)
        {
            return;
        }

        var user = CurrentUser();
        sink.Record(new AuditEntry(Clock.GetUtcNow(), user?.Id, user?.Username, operation, kind, entityId, succeeded, errorCode, fields ?? [], elapsedMs));
    }

    /// <summary>Records a handler call from its request and result, without ever looking at values other than ids.</summary>
    internal static void RecordHandler(string operation, object? request, object? result, bool succeeded, string? errorCode, double elapsedMs)
    {
        if (Sink is null)
        {
            return;
        }

        var shape = request is null ? RequestShape.Empty : Shapes.GetOrAdd(request.GetType(), RequestShape.Of);
        var entityId = shape.IdOf(request) ?? IdOf(result is IResult { IsSuccess: true } ok ? ok.BoxedValue : result);
        var fields = shape.Kind == AuditKind.Write ? shape.SuppliedFields(request) : [];
        Record(operation, shape.Kind, succeeded, errorCode, entityId, fields, elapsedMs);
    }

    private static Guid? IdOf(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var property = value.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return property?.PropertyType == typeof(Guid) ? (Guid)property.GetValue(value)! : null;
    }

    /// <summary>A Query reads, anything else (a Command) writes. Fields are the request properties that were filled in.</summary>
    private sealed class RequestShape
    {
        public static RequestShape Empty { get; } = new(AuditKind.Read, null, []);

        private readonly PropertyInfo? _id;
        private readonly PropertyInfo[] _fields;

        private RequestShape(AuditKind kind, PropertyInfo? id, PropertyInfo[] fields)
        {
            Kind = kind;
            _id = id;
            _fields = fields;
        }

        public AuditKind Kind { get; }

        public static RequestShape Of(Type type)
        {
            var kind = type.Name.EndsWith("Query", StringComparison.Ordinal) ? AuditKind.Read : AuditKind.Write;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var id = properties.FirstOrDefault(p => p.Name == "Id" && p.PropertyType == typeof(Guid));
            var fields = properties
                .Where(p => p.GetIndexParameters().Length == 0 && p.Name != "Id" && !p.Name.EndsWith("Id", StringComparison.Ordinal) && p.Name != "EqualityContract")
                .ToArray();
            return new RequestShape(kind, id, fields);
        }

        public Guid? IdOf(object? request) => request is null ? null : _id?.GetValue(request) as Guid?;

        public List<string> SuppliedFields(object? request) =>
            request is null
                ? []
                : [.. _fields.Where(p => p.GetValue(request) is { } value && value is not "").Select(p => p.Name)];
    }
}
