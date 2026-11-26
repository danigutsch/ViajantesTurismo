using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded response with Admin outbox publication metadata.</summary>
public sealed record GetOutboxDeliveryPageDto
{
    /// <summary>Gets the rows in descending timestamp and storage identifier order.</summary>
    public required IReadOnlyList<OutboxDeliveryRowDto> Rows
    {
        get; init;
    }

    /// <summary>Gets the timestamp to continue from with the same filters, when another row exists.</summary>
    public required DateTimeOffset? NextRecordedAt
    {
        get; init;
    }

    /// <summary>Gets the storage identifier to send with the continuation timestamp.</summary>
    public required Guid? NextStorageId
    {
        get; init;
    }

    /// <summary>Gets whether the response includes retained published metadata in an exact event search.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludesPublished
    {
        get; init;
    }
}
