using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Maps CSV row data to validated domain objects.
/// </summary>
public static class CsvRowToCustomerMapper
{
    /// <summary>
    /// Maps a CSV row to a fully validated <see cref="Customer"/>.
    /// </summary>
    /// <param name="document">CSV document that provides header-to-column mapping.</param>
    /// <param name="row">CSV data row to map.</param>
    /// <param name="timeProvider">Time provider used by domain birth-date validation.</param>
    /// <returns>A result containing the mapped <see cref="Customer"/> or validation errors.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/>, <paramref name="row"/>, or <paramref name="timeProvider"/> is null.
    /// </exception>
    public static Result<Customer> MapCustomer(CsvDocument document, CsvRow row, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var personalInfoResult = MapPersonalInfo(document, row, timeProvider);
        var identificationInfoResult = MapIdentificationInfo(document, row);
        var contactInfoResult = MapContactInfo(document, row);
        var addressResult = MapAddress(document, row);
        var physicalInfoResult = MapPhysicalInfo(document, row);
        var accommodationPreferencesResult = MapAccommodationPreferences(document, row);
        var emergencyContactResult = MapEmergencyContact(document, row);
        var medicalInfoResult = MapMedicalInfo(document, row);

        var errors = new ValidationErrors();
        if (personalInfoResult.IsFailure)
        {
            errors.Add(personalInfoResult);
        }

        if (identificationInfoResult.IsFailure)
        {
            errors.Add(identificationInfoResult);
        }

        if (contactInfoResult.IsFailure)
        {
            errors.Add(contactInfoResult);
        }

        if (addressResult.IsFailure)
        {
            errors.Add(addressResult);
        }

        if (physicalInfoResult.IsFailure)
        {
            errors.Add(physicalInfoResult);
        }

        if (accommodationPreferencesResult.IsFailure)
        {
            errors.Add(accommodationPreferencesResult);
        }

        if (emergencyContactResult.IsFailure)
        {
            errors.Add(emergencyContactResult);
        }

        if (medicalInfoResult.IsFailure)
        {
            errors.Add(medicalInfoResult);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Customer>();
        }

        return new Customer(
            personalInfoResult.Value,
            identificationInfoResult.Value,
            contactInfoResult.Value,
            addressResult.Value,
            physicalInfoResult.Value,
            accommodationPreferencesResult.Value,
            emergencyContactResult.Value,
            medicalInfoResult.Value);
    }

    private static Result<PersonalInfo> MapPersonalInfo(CsvDocument document, CsvRow row, TimeProvider timeProvider)
    {
        var errors = new ValidationErrors();

        var firstName = GetRequired(document, row, "FirstName", errors);
        var lastName = GetRequired(document, row, "LastName", errors);
        var gender = GetRequired(document, row, "Gender", errors);
        var birthDateText = GetRequired(document, row, "BirthDate", errors);
        var nationality = GetRequired(document, row, "Nationality", errors);
        var occupation = GetRequired(document, row, "Occupation", errors);

        DateTime birthDate = default;
        if (!string.IsNullOrWhiteSpace(birthDateText)
            && !DateTime.TryParse(birthDateText, out birthDate))
        {
            errors.Add(Result.Invalid(
                detail: "BirthDate has invalid format.",
                field: "BirthDate",
                message: "BirthDate has invalid format."));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PersonalInfo>();
        }

        return PersonalInfo.Create(
            firstName!,
            lastName!,
            gender!,
            birthDate,
            nationality!,
            occupation!,
            timeProvider);
    }

    private static Result<IdentificationInfo> MapIdentificationInfo(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();
        var nationalId = GetRequired(document, row, "NationalId", errors);
        var idNationality = GetRequired(document, row, "IdNationality", errors);

        if (errors.HasErrors)
        {
            return errors.ToResult<IdentificationInfo>();
        }

        return IdentificationInfo.Create(nationalId!, idNationality!);
    }

    private static Result<ContactInfo> MapContactInfo(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();
        var email = GetRequired(document, row, "Email", errors);
        var mobile = GetRequired(document, row, "Mobile", errors);

        row.TryGetByHeader(document.Headers, "Instagram", out var instagram);
        row.TryGetByHeader(document.Headers, "Facebook", out var facebook);

        if (errors.HasErrors)
        {
            return errors.ToResult<ContactInfo>();
        }

        return ContactInfo.Create(email!, mobile!, instagram, facebook);
    }

    private static Result<Address> MapAddress(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();

        var street = GetRequired(document, row, "Street", errors);
        row.TryGetByHeader(document.Headers, "Complement", out var complement);
        var neighborhood = GetRequired(document, row, "Neighborhood", errors);
        var postalCode = GetRequired(document, row, "PostalCode", errors);
        var city = GetRequired(document, row, "City", errors);
        var state = GetRequired(document, row, "State", errors);
        var country = GetRequired(document, row, "Country", errors);

        if (errors.HasErrors)
        {
            return errors.ToResult<Address>();
        }

        return Address.Create(street!, complement, neighborhood!, postalCode!, city!, state!, country!);
    }

    private static Result<PhysicalInfo> MapPhysicalInfo(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();

        var weightKgText = GetRequired(document, row, "WeightKg", errors);
        var heightCentimetersText = GetRequired(document, row, "HeightCentimeters", errors);
        var bikeTypeText = GetRequired(document, row, "BikeType", errors);

        decimal weightKg = 0;
        if (!string.IsNullOrWhiteSpace(weightKgText)
            && !decimal.TryParse(weightKgText, out weightKg))
        {
            errors.Add(Result.Invalid(
                detail: "WeightKg has invalid format.",
                field: "WeightKg",
                message: "WeightKg has invalid format."));
        }

        var heightCentimeters = 0;
        if (!string.IsNullOrWhiteSpace(heightCentimetersText)
            && !int.TryParse(heightCentimetersText, out heightCentimeters))
        {
            errors.Add(Result.Invalid(
                detail: "HeightCentimeters has invalid format.",
                field: "HeightCentimeters",
                message: "HeightCentimeters has invalid format."));
        }

        BikeType bikeType = default;
        if (!string.IsNullOrWhiteSpace(bikeTypeText)
            && !Enum.TryParse(bikeTypeText, true, out bikeType))
        {
            errors.Add(Result.Invalid(
                detail: "BikeType has invalid format.",
                field: "BikeType",
                message: "BikeType has invalid format."));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PhysicalInfo>();
        }

        return PhysicalInfo.Create(weightKg, heightCentimeters, bikeType);
    }

    private static Result<AccommodationPreferences> MapAccommodationPreferences(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();

        var roomTypeText = GetRequired(document, row, "RoomType", errors);
        var bedTypeText = GetRequired(document, row, "BedType", errors);
        row.TryGetByHeader(document.Headers, "CompanionId", out var companionIdText);

        RoomType roomType = default;
        if (!string.IsNullOrWhiteSpace(roomTypeText)
            && !Enum.TryParse(roomTypeText, true, out roomType))
        {
            errors.Add(Result.Invalid(
                detail: "RoomType has invalid format.",
                field: "RoomType",
                message: "RoomType has invalid format."));
        }

        BedType bedType = default;
        if (!string.IsNullOrWhiteSpace(bedTypeText)
            && !Enum.TryParse(bedTypeText, true, out bedType))
        {
            errors.Add(Result.Invalid(
                detail: "BedType has invalid format.",
                field: "BedType",
                message: "BedType has invalid format."));
        }

        Guid? companionId = null;
        if (!string.IsNullOrWhiteSpace(companionIdText))
        {
            if (Guid.TryParse(companionIdText, out var parsedCompanionId))
            {
                companionId = parsedCompanionId;
            }
            else
            {
                errors.Add(Result.Invalid(
                    detail: "CompanionId has invalid format.",
                    field: "CompanionId",
                    message: "CompanionId has invalid format."));
            }
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<AccommodationPreferences>();
        }

        return AccommodationPreferences.Create(roomType, bedType, companionId);
    }

    private static Result<EmergencyContact> MapEmergencyContact(CsvDocument document, CsvRow row)
    {
        var errors = new ValidationErrors();

        var emergencyContactName = GetRequired(document, row, "EmergencyContactName", errors);
        var emergencyContactMobile = GetRequired(document, row, "EmergencyContactMobile", errors);

        if (errors.HasErrors)
        {
            return errors.ToResult<EmergencyContact>();
        }

        return EmergencyContact.Create(emergencyContactName!, emergencyContactMobile!);
    }

    private static Result<MedicalInfo> MapMedicalInfo(CsvDocument document, CsvRow row)
    {
        row.TryGetByHeader(document.Headers, "Allergies", out var allergies);
        row.TryGetByHeader(document.Headers, "AdditionalInfo", out var additionalInfo);
        return MedicalInfo.Create(allergies, additionalInfo);
    }

    private static string? GetRequired(CsvDocument document, CsvRow row, string headerName, ValidationErrors errors)
    {
        if (row.TryGetByHeader(document.Headers, headerName, out var value))
        {
            return value;
        }

        errors.Add(Result.Invalid(
            detail: $"Required header '{headerName}' is missing.",
            field: "headers",
            message: $"Required header '{headerName}' is missing."));

        return null;
    }
}
