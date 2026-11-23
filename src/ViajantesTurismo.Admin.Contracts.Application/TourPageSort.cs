namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Lists the ordering options for the Management tour overview.</summary>
public enum TourPageSort
{
    /// <summary>Shows the newest tours first by identifier assigned at creation.</summary>
    Default,

    /// <summary>Orders by operational identifier.</summary>
    Identifier,

    /// <summary>Orders by the tour name.</summary>
    Name,

    /// <summary>Orders by the first day of the tour.</summary>
    StartDate,

    /// <summary>Orders by the last day of the tour.</summary>
    EndDate,

    /// <summary>Orders by the tour's base price.</summary>
    Price,

    /// <summary>Orders by the current number of confirmed travelers.</summary>
    CurrentCustomerCount
}
