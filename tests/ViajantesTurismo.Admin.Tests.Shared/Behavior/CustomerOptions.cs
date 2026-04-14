using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for creating a customer in tests.
/// </summary>
/// <param name="FirstName">The first name override.</param>
/// <param name="LastName">The last name override.</param>
/// <param name="Gender">The gender override.</param>
/// <param name="BirthDate">The birth date override.</param>
/// <param name="Nationality">The nationality override.</param>
/// <param name="Occupation">The occupation override.</param>
/// <param name="PassportNumber">The passport number override.</param>
/// <param name="PassportCountry">The passport country override.</param>
/// <param name="Email">The email override.</param>
/// <param name="Mobile">The mobile number override.</param>
/// <param name="Instagram">The Instagram handle override.</param>
/// <param name="Facebook">The Facebook profile override.</param>
/// <param name="Street">The street override.</param>
/// <param name="Complement">The address complement override.</param>
/// <param name="Neighborhood">The neighborhood override.</param>
/// <param name="PostalCode">The postal code override.</param>
/// <param name="City">The city override.</param>
/// <param name="State">The state override.</param>
/// <param name="Country">The country override.</param>
/// <param name="WeightKg">The weight override.</param>
/// <param name="HeightCentimeters">The height override.</param>
/// <param name="PreferredBike">The preferred bike override.</param>
/// <param name="PreferredRoom">The preferred room override.</param>
/// <param name="PreferredBed">The preferred bed override.</param>
/// <param name="CompanionId">The companion identifier override.</param>
/// <param name="EmergencyContactName">The emergency contact name override.</param>
/// <param name="EmergencyContactMobile">The emergency contact mobile override.</param>
/// <param name="Allergies">The allergies override.</param>
/// <param name="MedicalAdditionalInfo">The medical info override.</param>
public sealed record CustomerOptions(
    string? FirstName = null,
    string? LastName = null,
    string? Gender = null,
    DateTime? BirthDate = null,
    string? Nationality = null,
    string? Occupation = null,
    string? PassportNumber = null,
    string? PassportCountry = null,
    string? Email = null,
    string? Mobile = null,
    string? Instagram = null,
    string? Facebook = null,
    string? Street = null,
    string? Complement = null,
    string? Neighborhood = null,
    string? PostalCode = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    int? WeightKg = null,
    int? HeightCentimeters = null,
    BikeType? PreferredBike = null,
    RoomType? PreferredRoom = null,
    BedType? PreferredBed = null,
    Guid? CompanionId = null,
    string? EmergencyContactName = null,
    string? EmergencyContactMobile = null,
    string? Allergies = null,
    string? MedicalAdditionalInfo = null);
