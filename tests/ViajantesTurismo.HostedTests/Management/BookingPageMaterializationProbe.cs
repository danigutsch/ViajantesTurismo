using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Counts booking entities materialized by the HTTP list reader.</summary>
internal sealed class BookingPageMaterializationProbe : IMaterializationInterceptor
{
    private int bookings;

    /// <summary>Gets the number of booking rows materialized so far.</summary>
    internal int Bookings => Volatile.Read(ref bookings);

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is Booking)
        {
            Interlocked.Increment(ref bookings);
        }

        return entity;
    }
}
