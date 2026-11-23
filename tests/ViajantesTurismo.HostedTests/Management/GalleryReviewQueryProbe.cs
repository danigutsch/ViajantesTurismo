using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Counts gallery queries and the image roots materialized by the real Catalog store.</summary>
internal sealed class GalleryReviewQueryProbe : DbCommandInterceptor, IMaterializationInterceptor
{
    private int imageReads;
    private int limitedImageReads;
    private int materializedImages;

    /// <summary>Gets the database reads involving image metadata.</summary>
    internal int ImageReads => Volatile.Read(ref imageReads);

    /// <summary>Gets the image reads that limit the selected image root in SQL.</summary>
    internal int LimitedImageReads => Volatile.Read(ref limitedImageReads);

    /// <summary>Gets the number of materialized image roots.</summary>
    internal int MaterializedImages => Volatile.Read(ref materializedImages);

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"PublicMediaImages\"", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref imageReads);
            if (command.CommandText.Contains("LIMIT 1", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref limitedImageReads);
            }
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is PublicMediaImage)
        {
            Interlocked.Increment(ref materializedImages);
        }

        return entity;
    }
}
