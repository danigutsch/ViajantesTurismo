namespace ViajantesTurismo.Management.Web.Components.Shared;

internal enum UploadOperationStatus
{
    None,
    Queued,
    Retrying,
    Uploading,
    Processing,
    Complete,
    Canceled,
    Failed,
}
