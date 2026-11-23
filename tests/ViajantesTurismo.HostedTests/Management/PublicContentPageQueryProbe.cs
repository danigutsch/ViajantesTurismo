using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Measures public content SQL and entities read over the routed Management request.</summary>
internal sealed class PublicContentPageQueryProbe : DbCommandInterceptor, IMaterializationInterceptor
{
    private int entries;
    private int variants;
    private int reads;

    /// <summary>Gets the number of editable content entities materialized.</summary>
    internal int Entries => Volatile.Read(ref entries);

    /// <summary>Gets the number of localized variant entities materialized.</summary>
    internal int Variants => Volatile.Read(ref variants);

    /// <summary>Gets the number of public content SELECT commands.</summary>
    internal int Reads => Volatile.Read(ref reads);

    /// <summary>Gets the most recent limited public content SQL statement.</summary>
    internal string? LastLimitedRead
    {
        get; private set;
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is EditablePublicContent)
        {
            Interlocked.Increment(ref entries);
        }
        else if (entity is PublicContentVariant)
        {
            Interlocked.Increment(ref variants);
        }

        return entity;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"PublicContent\"", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref reads);
            if (command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
            {
                LastLimitedRead = command.CommandText;
            }
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
