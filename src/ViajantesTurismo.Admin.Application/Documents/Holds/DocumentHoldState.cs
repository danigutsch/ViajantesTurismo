namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Names the review states a hold can be filtered by in the cross-document overview.</summary>
public enum DocumentHoldState
{
    /// <summary>The hold is not released, whether or not its review is due.</summary>
    Active,

    /// <summary>The hold is not released and its review date is strictly before now.</summary>
    OverdueReview,

    /// <summary>The hold was released.</summary>
    Released,
}
