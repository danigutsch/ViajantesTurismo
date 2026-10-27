namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>An upstream response that records whether it was disposed.</summary>
internal sealed class ReleaseTrackingResponse : HttpResponseMessage
{
    /// <summary>True once the response has been disposed.</summary>
    public bool IsReleased
    {
        get; private set;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsReleased = true;
        }

        base.Dispose(disposing);
    }
}
