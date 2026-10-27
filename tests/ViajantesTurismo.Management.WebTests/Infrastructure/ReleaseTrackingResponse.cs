namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>An upstream response that records whether it was disposed.</summary>
internal sealed class ReleaseTrackingResponse : HttpResponseMessage
{
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
