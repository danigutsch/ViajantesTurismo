using JetBrains.Annotations;

namespace ViajantesTurismo.Admin.BehaviorTests.Infrastructure.Coverage;

/// <summary>
/// Registry of all domain invariants that must have behavior test coverage.
/// Each invariant maps to specific scenarios that validate the business rule.
/// </summary>
public static class InvariantRegistry
{
    /// <summary>
    /// Get all invariant IDs for an aggregate.
    /// </summary>
    public static string[] GetInvariantsForAggregate(Type aggregateType) =>
        aggregateType.Name switch
        {
            "Tour" => [.. typeof(Tour).GetFields().Select(f => (string)f.GetValue(null)!)],
            "Customer" => [.. typeof(Customer).GetFields().Select(f => (string)f.GetValue(null)!)],
            _ => []
        };

    /// <summary>
    /// Get all Tour invariants.
    /// </summary>
    public static string[] GetTourInvariants() =>
        [.. typeof(Tour).GetFields().Select(f => (string)f.GetValue(null)!)];

    /// <summary>
    /// Get all Customer invariants.
    /// </summary>
    public static string[] GetCustomerInvariants() =>
        [.. typeof(Customer).GetFields().Select(f => (string)f.GetValue(null)!)];

    /// <summary>
    /// Get all invariants across all aggregates.
    /// </summary>
    public static string[] GetAllInvariants() =>
        [.. GetTourInvariants().Concat(GetCustomerInvariants())];

    /// <summary>
    /// All Tour aggregate invariants from docs/bounded-contexts/Admin.md
    /// </summary>
    [UsedImplicitly]
    internal static class Tour
    {
        public const string UniqueIdentifier = "INV-TOUR-001";
        public const string IdentifierRequired = "INV-TOUR-002";
        public const string IdentifierMaxLength = "INV-TOUR-003";
        public const string NameRequired = "INV-TOUR-004";
        public const string NameMaxLength = "INV-TOUR-005";
        public const string EndDateAfterStartDate = "INV-TOUR-006";
        public const string MinimumDuration = "INV-TOUR-007";
        public const string PricesStrictlyPositive = "INV-TOUR-008";
        public const string PricesWithinMaximum = "INV-TOUR-009";
        public const string MinCustomersRange = "INV-TOUR-010";
        public const string MaxCustomersRange = "INV-TOUR-011";
        public const string MinLessThanOrEqualMax = "INV-TOUR-012";
        public const string CannotExceedMaxCapacity = "INV-TOUR-013";
        public const string BookingsOnlyThroughAggregate = "INV-TOUR-014";
        public const string CannotDeleteWithConfirmedBookings = "INV-TOUR-015";
        public const string PrincipalAndCompanionDifferent = "INV-TOUR-016";
        public const string BikeTypeNoneNotAllowed = "INV-TOUR-017";
        public const string CannotModifyCancelledOrCompleted = "INV-TOUR-018";
        public const string CanOnlyRemovePending = "INV-TOUR-019";
        public const string PaymentCannotExceedBalance = "INV-TOUR-020";
        public const string PaymentDateNotFuture = "INV-TOUR-021";
        public const string AbsoluteDiscountNotExceedSubtotal = "INV-TOUR-022";
        public const string FinalPricePositive = "INV-TOUR-023";
        public const string PercentageDiscountMax100 = "INV-TOUR-024";
    }

    /// <summary>
    /// All Customer aggregate invariants from docs/bounded-contexts/Admin.md
    /// </summary>
    [UsedImplicitly]
    internal static class Customer
    {
        public const string EmailUnique = "INV-CUST-001";
        public const string EmailValid = "INV-CUST-002";
        public const string EmailMaxLength = "INV-CUST-003";
        public const string BirthDateNotFuture = "INV-CUST-004";
        public const string FirstNameRequired = "INV-CUST-005";
        public const string FirstNameMaxLength = "INV-CUST-006";
        public const string LastNameRequired = "INV-CUST-007";
        public const string LastNameMaxLength = "INV-CUST-008";
        public const string GenderRequired = "INV-CUST-009";
        public const string GenderMaxLength = "INV-CUST-010";
        public const string NationalityRequired = "INV-CUST-011";
        public const string NationalityMaxLength = "INV-CUST-012";
        public const string OccupationRequired = "INV-CUST-013";
        public const string OccupationMaxLength = "INV-CUST-014";
        public const string MobileRequired = "INV-CUST-015";
        public const string MobileMaxLength = "INV-CUST-016";
        public const string WeightRange = "INV-CUST-017";
        public const string HeightRange = "INV-CUST-018";
        public const string NationalIdRequired = "INV-CUST-019";
        public const string NationalIdMaxLength = "INV-CUST-020";
        public const string IdNationalityRequired = "INV-CUST-021";
        public const string AddressStreetRequired = "INV-CUST-022";
        public const string AddressNeighborhoodRequired = "INV-CUST-023";
        public const string AddressPostalCodeRequired = "INV-CUST-024";
        public const string AddressCityRequired = "INV-CUST-025";
        public const string AddressStateRequired = "INV-CUST-026";
        public const string AddressCountryRequired = "INV-CUST-027";
        public const string EmergencyContactNameRequired = "INV-CUST-028";
        public const string EmergencyContactMobileRequired = "INV-CUST-029";
        public const string MedicalInfoMaxLength = "INV-CUST-030";
    }
}
