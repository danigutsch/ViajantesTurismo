namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines the sortable columns in the customer overview.</summary>
public enum CustomerPageSort
{
    /// <summary>Orders by first name followed by last name.</summary>
    Name,

    /// <summary>Orders by email address.</summary>
    Email,

    /// <summary>Orders by nationality.</summary>
    Nationality
}
