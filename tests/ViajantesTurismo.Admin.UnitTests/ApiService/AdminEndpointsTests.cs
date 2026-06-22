using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>
/// Verifies centralized Admin API endpoint definitions.
/// </summary>
public sealed class AdminEndpointsTests
{
    [Fact]
    public void Catalog_Contains_All_Expected_Admin_Endpoint_Names()
    {
        // Arrange
        string[] expectedNames =
        [
            "CancelBooking",
            "CommitImportWithResolutions",
            "CompleteBooking",
            "ConfirmBooking",
            "CreateBooking",
            "CreateCustomer",
            "CreateTour",
            "DeleteBooking",
            "GetBookingById",
            "GetBookings",
            "GetBookingsByCustomerId",
            "GetBookingsByTourId",
            "GetCustomerById",
            "GetCustomers",
            "GetErrorDocumentation",
            "GetErrorDocumentationByIdentifier",
            "GetTourById",
            "GetTours",
            "ImportCustomers",
            "RecordPayment",
            "UpdateBookingDetails",
            "UpdateBookingDiscount",
            "UpdateBookingNotes",
            "UpdateCustomer",
            "UpdateTour"
        ];

        var definitions = AdminEndpointDefinitionCatalog.GetDefinitions();

        // Act
        var actualNames = definitions
            .Select(definition => definition.Metadata.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(expectedNames, actualNames);
    }

    [Fact]
    public void Catalog_Definitions_Have_Required_Route_And_Metadata()
    {
        // Arrange
        var definitions = AdminEndpointDefinitionCatalog.GetDefinitions();

        // Act
        var invalidDefinitions = definitions
            .Where(definition =>
                string.IsNullOrWhiteSpace(definition.Pattern)
                || string.IsNullOrWhiteSpace(definition.Metadata.Name)
                || string.IsNullOrWhiteSpace(definition.Metadata.Summary)
                || string.IsNullOrWhiteSpace(definition.Metadata.Description))
            .ToArray();

        // Assert
        Assert.Empty(invalidDefinitions);
    }

    [Fact]
    public void Catalog_Uses_Unique_Endpoint_Names()
    {
        // Arrange
        var definitions = AdminEndpointDefinitionCatalog.GetDefinitions();

        // Act
        var duplicatedNames = definitions
            .GroupBy(definition => definition.Metadata.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        // Assert
        Assert.Empty(duplicatedNames);
    }

    [Fact]
    public void Catalog_Exposes_Record_Payment_Definition()
    {
        // Arrange
        var definition = AdminEndpoints.Bookings.RecordPayment;

        // Act
        var pattern = definition.Pattern;
        var metadata = definition.Metadata;

        // Assert
        Assert.Equal("/{id:guid}/payments", pattern);
        Assert.Equal("RecordPayment", metadata.Name);
        Assert.Equal("Records a payment.", metadata.Summary);
        Assert.Equal("Records a payment for a booking.", metadata.Description);
    }
}
