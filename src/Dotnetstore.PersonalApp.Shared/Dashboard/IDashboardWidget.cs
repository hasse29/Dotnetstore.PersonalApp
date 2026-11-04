namespace Dotnetstore.PersonalApp.Shared.Dashboard;

/// <summary>Who is looking at the dashboard and what day it is for them.</summary>
public sealed record DashboardContext(Guid UserId, DateOnly Today);

/// <summary>One figure on a panel. <paramref name="Warn"/> draws attention to it, for example stock under its minimum.</summary>
public sealed record DashboardMetric(string Label, string Value, bool Warn = false);

/// <summary>One line in a panel's list: a title, a smaller line below it and a value on the right.</summary>
public sealed record DashboardRow(string Title, string? Subtitle = null, string? Value = null);

/// <summary>What a module shows on the dashboard: figures, then a short list. The host draws it, so modules need no views for it.</summary>
public sealed record DashboardPanel(string Title, IReadOnlyList<DashboardMetric> Metrics, string ListTitle, IReadOnlyList<DashboardRow> Rows, string EmptyText)
{
    public bool IsEmpty => Rows.Count == 0;
}

/// <summary>Implemented by a module that has something to say on the dashboard; the host shows the panel of every enabled module.</summary>
public interface IDashboardWidget
{
    /// <summary>The module that owns the widget; it is hidden while the module is disabled.</summary>
    string ModuleKey { get; }

    /// <summary>Lower numbers come first.</summary>
    int Order { get; }

    Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken);
}
