namespace ViajantesTurismo.Management.Web.Components.Shared;

internal sealed class UploadOperationState
{
    private const int AnnouncementPercentageStep = 10;
    private long _nextOperationId;
    private int _lastAnnouncedPercentage;
    private bool _uploadStartedAnnounced;

    internal long OperationId { get; private set; }

    internal UploadOperationStatus Status { get; private set; }

    internal string FileName { get; private set; } = string.Empty;

    internal long BytesTransferred { get; private set; }

    internal long TotalBytes { get; private set; }

    internal string Announcement { get; private set; } = string.Empty;

    internal string? FailureMessage { get; private set; }

    internal bool IsActive => Status is UploadOperationStatus.Queued
        or UploadOperationStatus.Retrying
        or UploadOperationStatus.Uploading
        or UploadOperationStatus.Processing;

    internal bool IsDeterminate => Status is UploadOperationStatus.Uploading;

    internal bool CanRetry => Status is UploadOperationStatus.Canceled or UploadOperationStatus.Failed;

    internal long Begin(string fileName, long totalBytes, bool isRetry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalBytes);

        if (IsActive)
        {
            throw new InvalidOperationException("An upload operation is already active.");
        }

        if (isRetry && !CanRetry)
        {
            throw new InvalidOperationException("Only a canceled or failed upload can be retried.");
        }

        OperationId = checked(++_nextOperationId);
        Status = isRetry ? UploadOperationStatus.Retrying : UploadOperationStatus.Queued;
        FileName = fileName;
        BytesTransferred = 0;
        TotalBytes = totalBytes;
        FailureMessage = null;
        _lastAnnouncedPercentage = 0;
        _uploadStartedAnnounced = false;
        Announcement = isRetry ? $"Retrying {fileName}." : $"{fileName} queued.";
        return OperationId;
    }

    internal bool ReportBytes(long operationId, long bytesTransferred)
    {
        if (!IsCurrentActive(operationId))
        {
            return false;
        }

        if (Status is UploadOperationStatus.Processing)
        {
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(bytesTransferred);
        if (bytesTransferred > TotalBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesTransferred), bytesTransferred, "Transferred bytes cannot exceed the declared file length.");
        }

        if (bytesTransferred < BytesTransferred)
        {
            throw new InvalidOperationException("Transferred bytes cannot move backwards.");
        }

        if (bytesTransferred == BytesTransferred && Status is UploadOperationStatus.Uploading)
        {
            return false;
        }

        BytesTransferred = bytesTransferred;
        if (bytesTransferred == TotalBytes)
        {
            Status = UploadOperationStatus.Processing;
            Announcement = $"Upload complete. Server processing {FileName}.";
            return true;
        }

        Status = UploadOperationStatus.Uploading;
        UpdateUploadAnnouncement();
        return true;
    }

    internal bool Complete(long operationId)
    {
        if (!IsCurrentActive(operationId))
        {
            return false;
        }

        if (Status is not UploadOperationStatus.Processing)
        {
            throw new InvalidOperationException("An upload cannot complete before all declared bytes reach the transport.");
        }

        Status = UploadOperationStatus.Complete;
        Announcement = $"{FileName} complete.";
        return true;
    }

    internal bool Cancel(long operationId)
    {
        if (!IsCurrentActive(operationId))
        {
            return false;
        }

        Status = UploadOperationStatus.Canceled;
        Announcement = $"Canceled {FileName}.";
        return true;
    }

    internal bool Fail(long operationId, string failureMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        if (!IsCurrentActive(operationId))
        {
            return false;
        }

        Status = UploadOperationStatus.Failed;
        FailureMessage = failureMessage;
        Announcement = $"{FileName} failed.";
        return true;
    }

    internal bool IsCurrentActive(long operationId) => OperationId == operationId && IsActive;

    internal void Reset()
    {
        Status = UploadOperationStatus.None;
        FileName = string.Empty;
        BytesTransferred = 0;
        TotalBytes = 0;
        Announcement = string.Empty;
        FailureMessage = null;
        _lastAnnouncedPercentage = 0;
        _uploadStartedAnnounced = false;
    }

    private void UpdateUploadAnnouncement()
    {
        var percentage = (int)(BytesTransferred * 100 / TotalBytes);
        var announcementPercentage = percentage / AnnouncementPercentageStep * AnnouncementPercentageStep;
        if (announcementPercentage >= AnnouncementPercentageStep && announcementPercentage > _lastAnnouncedPercentage)
        {
            _lastAnnouncedPercentage = announcementPercentage;
            _uploadStartedAnnounced = true;
            Announcement = $"{FileName} upload is {announcementPercentage}% complete.";
            return;
        }

        if (!_uploadStartedAnnounced)
        {
            _uploadStartedAnnounced = true;
            Announcement = $"Uploading {FileName}.";
        }
    }
}
