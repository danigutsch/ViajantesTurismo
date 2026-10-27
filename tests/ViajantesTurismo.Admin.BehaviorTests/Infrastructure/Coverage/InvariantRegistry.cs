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
    public static string[] GetInvariantsForAggregate(Type aggregateType)
    {
        ArgumentNullException.ThrowIfNull(aggregateType);

        return aggregateType.Name switch
        {
            "Tour" => [.. typeof(Tour).GetFields().Select(ReadInvariantValue)],
            "Customer" => [.. typeof(Customer).GetFields().Select(ReadInvariantValue)],
            _ => []
        };
    }

    /// <summary>
    /// Get all Tour invariants.
    /// </summary>
    public static string[] GetTourInvariants() =>
        [.. typeof(Tour).GetFields().Select(ReadInvariantValue)];

    /// <summary>
    /// Get all Customer invariants.
    /// </summary>
    public static string[] GetCustomerInvariants() =>
        [.. typeof(Customer).GetFields().Select(ReadInvariantValue)];

    /// <summary>
    /// Get all invariants across all aggregates.
    /// </summary>
    public static string[] GetAllInvariants() =>
        [.. GetTourInvariants().Concat(GetCustomerInvariants())];

    private static string ReadInvariantValue(System.Reflection.FieldInfo field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return field.GetValue(null) as string
            ?? throw new InvalidOperationException($"Invariant field '{field.Name}' must expose a non-null string value.");
    }

    /// <summary>
    /// All Tour aggregate invariants from docs/bounded-contexts/Admin.md
    /// </summary>
    [UsedImplicitly]
    internal static class Tour
    {
        /// <summary>Invariant INV-TOUR-001: unique identifier.</summary>
        public const string UniqueIdentifier = "INV-TOUR-001";
        /// <summary>Invariant INV-TOUR-002: identifier required.</summary>
        public const string IdentifierRequired = "INV-TOUR-002";
        /// <summary>Invariant INV-TOUR-003: identifier max length.</summary>
        public const string IdentifierMaxLength = "INV-TOUR-003";
        /// <summary>Invariant INV-TOUR-004: name required.</summary>
        public const string NameRequired = "INV-TOUR-004";
        /// <summary>Invariant INV-TOUR-005: name max length.</summary>
        public const string NameMaxLength = "INV-TOUR-005";
        /// <summary>Invariant INV-TOUR-006: end date after start date.</summary>
        public const string EndDateAfterStartDate = "INV-TOUR-006";
        /// <summary>Invariant INV-TOUR-007: minimum duration.</summary>
        public const string MinimumDuration = "INV-TOUR-007";
        /// <summary>Invariant INV-TOUR-008: prices strictly positive.</summary>
        public const string PricesStrictlyPositive = "INV-TOUR-008";
        /// <summary>Invariant INV-TOUR-009: prices within maximum.</summary>
        public const string PricesWithinMaximum = "INV-TOUR-009";
        /// <summary>Invariant INV-TOUR-010: min customers range.</summary>
        public const string MinCustomersRange = "INV-TOUR-010";
        /// <summary>Invariant INV-TOUR-011: max customers range.</summary>
        public const string MaxCustomersRange = "INV-TOUR-011";
        /// <summary>Invariant INV-TOUR-012: min less than or equal max.</summary>
        public const string MinLessThanOrEqualMax = "INV-TOUR-012";
        /// <summary>Invariant INV-TOUR-013: cannot exceed max capacity.</summary>
        public const string CannotExceedMaxCapacity = "INV-TOUR-013";
        /// <summary>Invariant INV-TOUR-014: bookings only through aggregate.</summary>
        public const string BookingsOnlyThroughAggregate = "INV-TOUR-014";
        /// <summary>Invariant INV-TOUR-015: cannot delete with confirmed bookings.</summary>
        public const string CannotDeleteWithConfirmedBookings = "INV-TOUR-015";
        /// <summary>Invariant INV-TOUR-016: principal and companion different.</summary>
        public const string PrincipalAndCompanionDifferent = "INV-TOUR-016";
        /// <summary>Invariant INV-TOUR-017: bike type none not allowed.</summary>
        public const string BikeTypeNoneNotAllowed = "INV-TOUR-017";
        /// <summary>Invariant INV-TOUR-018: cannot modify cancelled or completed.</summary>
        public const string CannotModifyCancelledOrCompleted = "INV-TOUR-018";
        /// <summary>Invariant INV-TOUR-019: can only remove pending.</summary>
        public const string CanOnlyRemovePending = "INV-TOUR-019";
        /// <summary>Invariant INV-TOUR-020: payment cannot exceed balance.</summary>
        public const string PaymentCannotExceedBalance = "INV-TOUR-020";
        /// <summary>Invariant INV-TOUR-021: payment date not future.</summary>
        public const string PaymentDateNotFuture = "INV-TOUR-021";
        /// <summary>Invariant INV-TOUR-022: absolute discount not exceed subtotal.</summary>
        public const string AbsoluteDiscountNotExceedSubtotal = "INV-TOUR-022";
        /// <summary>Invariant INV-TOUR-023: final price positive.</summary>
        public const string FinalPricePositive = "INV-TOUR-023";
        /// <summary>Invariant INV-TOUR-024: percentage discount max100.</summary>
        public const string PercentageDiscountMax100 = "INV-TOUR-024";
    }

    /// <summary>
    /// All Customer aggregate invariants from docs/bounded-contexts/Admin.md
    /// </summary>
    [UsedImplicitly]
    internal static class Customer
    {
        /// <summary>Invariant INV-CUST-001: email unique.</summary>
        public const string EmailUnique = "INV-CUST-001";
        /// <summary>Invariant INV-CUST-002: email valid.</summary>
        public const string EmailValid = "INV-CUST-002";
        /// <summary>Invariant INV-CUST-003: email max length.</summary>
        public const string EmailMaxLength = "INV-CUST-003";
        /// <summary>Invariant INV-CUST-004: birth date not future.</summary>
        public const string BirthDateNotFuture = "INV-CUST-004";
        /// <summary>Invariant INV-CUST-005: first name required.</summary>
        public const string FirstNameRequired = "INV-CUST-005";
        /// <summary>Invariant INV-CUST-006: first name max length.</summary>
        public const string FirstNameMaxLength = "INV-CUST-006";
        /// <summary>Invariant INV-CUST-007: last name required.</summary>
        public const string LastNameRequired = "INV-CUST-007";
        /// <summary>Invariant INV-CUST-008: last name max length.</summary>
        public const string LastNameMaxLength = "INV-CUST-008";
        /// <summary>Invariant INV-CUST-009: gender required.</summary>
        public const string GenderRequired = "INV-CUST-009";
        /// <summary>Invariant INV-CUST-010: gender max length.</summary>
        public const string GenderMaxLength = "INV-CUST-010";
        /// <summary>Invariant INV-CUST-011: nationality required.</summary>
        public const string NationalityRequired = "INV-CUST-011";
        /// <summary>Invariant INV-CUST-012: nationality max length.</summary>
        public const string NationalityMaxLength = "INV-CUST-012";
        /// <summary>Invariant INV-CUST-013: occupation required.</summary>
        public const string OccupationRequired = "INV-CUST-013";
        /// <summary>Invariant INV-CUST-014: occupation max length.</summary>
        public const string OccupationMaxLength = "INV-CUST-014";
        /// <summary>Invariant INV-CUST-015: mobile required.</summary>
        public const string MobileRequired = "INV-CUST-015";
        /// <summary>Invariant INV-CUST-016: mobile max length.</summary>
        public const string MobileMaxLength = "INV-CUST-016";
        /// <summary>Invariant INV-CUST-017: weight range.</summary>
        public const string WeightRange = "INV-CUST-017";
        /// <summary>Invariant INV-CUST-018: height range.</summary>
        public const string HeightRange = "INV-CUST-018";
        /// <summary>Invariant INV-CUST-019: national id required.</summary>
        public const string NationalIdRequired = "INV-CUST-019";
        /// <summary>Invariant INV-CUST-020: national id max length.</summary>
        public const string NationalIdMaxLength = "INV-CUST-020";
        /// <summary>Invariant INV-CUST-021: id nationality required.</summary>
        public const string IdNationalityRequired = "INV-CUST-021";
        /// <summary>Invariant INV-CUST-022: address street required.</summary>
        public const string AddressStreetRequired = "INV-CUST-022";
        /// <summary>Invariant INV-CUST-023: address neighborhood required.</summary>
        public const string AddressNeighborhoodRequired = "INV-CUST-023";
        /// <summary>Invariant INV-CUST-024: address postal code required.</summary>
        public const string AddressPostalCodeRequired = "INV-CUST-024";
        /// <summary>Invariant INV-CUST-025: address city required.</summary>
        public const string AddressCityRequired = "INV-CUST-025";
        /// <summary>Invariant INV-CUST-026: address state required.</summary>
        public const string AddressStateRequired = "INV-CUST-026";
        /// <summary>Invariant INV-CUST-027: address country required.</summary>
        public const string AddressCountryRequired = "INV-CUST-027";
        /// <summary>Invariant INV-CUST-028: emergency contact name required.</summary>
        public const string EmergencyContactNameRequired = "INV-CUST-028";
        /// <summary>Invariant INV-CUST-029: emergency contact mobile required.</summary>
        public const string EmergencyContactMobileRequired = "INV-CUST-029";
        /// <summary>Invariant INV-CUST-030: medical info max length.</summary>
        public const string MedicalInfoMaxLength = "INV-CUST-030";
    }
}
