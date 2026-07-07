using System.Text.RegularExpressions;
using PluginRuntime.Capabilities.Abstractions;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Database;

/// <summary>
/// Provides controlled database access for plugins with SQL injection protection,
/// schema isolation, and transparent connection pooling.
/// </summary>
public partial class DatabaseCapability : IDatabaseCapability
{
    private readonly Guid _pluginId;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public string Name => "database";
    public string Version => "1.0";

    public DatabaseCapability(Guid pluginId, IDatabaseConnectionFactory connectionFactory)
    {
        _pluginId = pluginId;
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        var scopedSql = ScopeToPluginSchema(sql);
        return await _connectionFactory.QueryAsync<T>(_pluginId, scopedSql, parameters, cancellationToken);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        var scopedSql = ScopeToPluginSchema(sql);
        return await _connectionFactory.ExecuteAsync(_pluginId, scopedSql, parameters, cancellationToken);
    }

    public async Task<T?> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        var scopedSql = ScopeToPluginSchema(sql);
        return await _connectionFactory.QuerySingleAsync<T>(_pluginId, scopedSql, parameters, cancellationToken);
    }

    /// <summary>
    /// Validates that the SQL does not contain string interpolation markers.
    /// All queries must use parameterized syntax (@parameter).
    /// </summary>
    private static void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query must not be null or empty.", nameof(sql));

        // Detect string interpolation markers (non-parameterized SQL)
        if (InterpolationPattern().IsMatch(sql))
            throw new InvalidOperationException(
                "Non-parameterized SQL detected. All queries must use parameterized queries. " +
                "Use @parameter syntax instead of string interpolation.");
    }

    /// <summary>
    /// Scopes the SQL to the plugin's isolated schema by setting the search_path.
    /// </summary>
    private string ScopeToPluginSchema(string sql)
    {
        var schemaPrefix = $"plugin_{_pluginId:N}";
        return $"SET search_path TO {schemaPrefix}; {sql}";
    }

    [GeneratedRegex(@"\$""|{[^}]+}", RegexOptions.Compiled)]
    private static partial Regex InterpolationPattern();
}
