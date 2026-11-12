using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Startup;

/// <summary>
/// Migrates the Contact database, then moves in the data of the former Addresses, EmailAddresses and PhoneNumbers
/// databases (one file each) and deletes those files, so nothing the user entered is lost.
/// A legacy file that cannot be imported is left where it is, untouched, and tried again at the next start; it never
/// stops the app from starting or the other files from being imported.
/// </summary>
internal sealed class MigrateContactDatabaseTask(
    IDbContextFactory<ContactDbContext> dbFactory,
    ILocalizationService localization,
    IReadOnlyList<string> legacyDatabasePaths) : IStartupTask
{
    /// <summary>A type table, the table whose rows point at it, and that table's foreign key column.</summary>
    private sealed record TypedPair(string TypeTable, string Table, string TypeColumn);

    private static readonly TypedPair[] Pairs =
    [
        new("AddressTypes", "Addresses", "AddressTypeId"),
        new("EmailAddressTypes", "EmailAddresses", "EmailAddressTypeId"),
        new("PhoneNumberTypes", "PhoneNumbers", "PhoneNumberTypeId"),
    ];

    private static readonly string[] Suffixes = ["", "-wal", "-shm", "-journal"];

    private readonly List<string> _skipped = [];

    /// <summary>The legacy databases the last run could not import (they are kept for the next start).</summary>
    public IReadOnlyList<string> SkippedLegacyDatabases => _skipped;

    public string Description => localization.Get("Startup_ContactDatabase");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _skipped.Clear();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var path in legacyDatabasePaths.Where(File.Exists))
        {
            try
            {
                await ImportAsync(db, path, cancellationToken).ConfigureAwait(false);
                DeleteDatabase(path);
            }
            catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
            {
                _skipped.Add(path);
            }
        }
    }

    private static async Task ImportAsync(ContactDbContext db, string legacyPath, CancellationToken cancellationToken)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        // ATTACH takes an expression, so the path is bound as a parameter.
        await Execute(connection, "ATTACH DATABASE $path AS legacy", cancellationToken, ("$path", legacyPath)).ConfigureAwait(false);
        try
        {
            await using (var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
            {
                await Execute(connection, "CREATE TEMP TABLE IF NOT EXISTS TypeMap (OldId TEXT PRIMARY KEY, NewId TEXT NOT NULL)", cancellationToken).ConfigureAwait(false);

                foreach (var pair in Pairs)
                {
                    if (await TableExists(connection, pair.TypeTable, cancellationToken).ConfigureAwait(false))
                    {
                        await ImportTypesAsync(connection, pair, cancellationToken).ConfigureAwait(false);
                    }

                    if (await TableExists(connection, pair.Table, cancellationToken).ConfigureAwait(false))
                    {
                        await ImportRowsAsync(connection, pair, cancellationToken).ConfigureAwait(false);
                    }
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await Execute(connection, "DROP TABLE IF EXISTS temp.TypeMap", cancellationToken).ConfigureAwait(false);
            await Execute(connection, "DETACH DATABASE legacy", cancellationToken).ConfigureAwait(false);

            // Release the legacy file so it can be deleted (or left alone for a retry) without a lingering handle.
            SqliteConnection.ClearAllPools();
        }
    }

    /// <summary>
    /// Types have a unique name. A legacy type whose id is new but whose name already exists (the user deleted a seeded type
    /// and made their own with the same name) is not inserted; its rows are pointed at the existing type instead.
    /// </summary>
    private static async Task ImportTypesAsync(SqliteConnection connection, TypedPair pair, CancellationToken cancellationToken)
    {
        await Execute(connection, "DELETE FROM temp.TypeMap", cancellationToken).ConfigureAwait(false);
        await Execute(
            connection,
            $"""
            INSERT OR IGNORE INTO temp.TypeMap (OldId, NewId)
            SELECT l."Id", m."Id" FROM legacy."{pair.TypeTable}" l
            JOIN main."{pair.TypeTable}" m ON m."Name" = l."Name" COLLATE NOCASE
            WHERE l."Id" <> m."Id" AND NOT EXISTS (SELECT 1 FROM main."{pair.TypeTable}" x WHERE x."Id" = l."Id")
            """,
            cancellationToken).ConfigureAwait(false);

        // Seeded types have fixed ids and already exist; OR IGNORE keeps them as they are.
        var columns = await SharedColumns(connection, pair.TypeTable, cancellationToken).ConfigureAwait(false);
        await Execute(
            connection,
            $"INSERT OR IGNORE INTO main.\"{pair.TypeTable}\" ({columns}) SELECT {columns} FROM legacy.\"{pair.TypeTable}\" WHERE \"Id\" NOT IN (SELECT OldId FROM temp.TypeMap)",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task ImportRowsAsync(SqliteConnection connection, TypedPair pair, CancellationToken cancellationToken)
    {
        // Only the columns both sides have are copied, so an older legacy schema cannot break the import.
        var shared = await SharedColumnNames(connection, pair.Table, cancellationToken).ConfigureAwait(false);
        var target = string.Join(", ", shared.Select(c => $"\"{c}\""));
        var source = string.Join(
            ", ",
            shared.Select(c => c == pair.TypeColumn
                ? $"COALESCE((SELECT NewId FROM temp.TypeMap WHERE OldId = l.\"{c}\"), l.\"{c}\")"
                : $"l.\"{c}\""));

        // Rows that already exist, or that break a uniqueness rule such as one email address once per object, are skipped.
        await Execute(
            connection,
            $"INSERT OR IGNORE INTO main.\"{pair.Table}\" ({target}) SELECT {source} FROM legacy.\"{pair.Table}\" l",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> TableExists(SqliteConnection connection, string table, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM legacy.sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<string> SharedColumns(SqliteConnection connection, string table, CancellationToken cancellationToken) =>
        string.Join(", ", (await SharedColumnNames(connection, table, cancellationToken).ConfigureAwait(false)).Select(c => $"\"{c}\""));

    private static async Task<List<string>> SharedColumnNames(SqliteConnection connection, string table, CancellationToken cancellationToken)
    {
        var legacy = await Columns(connection, "legacy", table, cancellationToken).ConfigureAwait(false);
        var current = await Columns(connection, "main", table, cancellationToken).ConfigureAwait(false);
        return [.. current.Where(legacy.Contains)];
    }

    private static async Task<List<string>> Columns(SqliteConnection connection, string schema, string table, CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info($table, $schema)";
        command.Parameters.AddWithValue("$table", table);
        command.Parameters.AddWithValue("$schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task Execute(SqliteConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Only constant SQL and table or column names from the constant list above are interpolated, never user input.
        command.CommandText = sql;
#pragma warning restore CA2100
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void DeleteDatabase(string path)
    {
        foreach (var suffix in Suffixes)
        {
            File.Delete(path + suffix);
        }
    }
}
