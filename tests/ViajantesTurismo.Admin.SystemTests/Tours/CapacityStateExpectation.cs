namespace ViajantesTurismo.Admin.SystemTests.Tours;

public sealed record CapacityStateExpectation(
    string ListBadgeSelector,
    string ListBadgeText,
    string ListCapacityText,
    string DetailsBadgeSelector,
    string DetailsBadgeText);
