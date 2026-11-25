using System.Globalization;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>
/// Loads duplicate-resolution state for customer import conflicts using the current import file and existing customer data.
/// </summary>
internal static class ImportCustomersConflictLoader
{
    private const string LoadFailureMessage = "Existing customer data could not be loaded.";

    /// <summary>Builds one conflict state per conflict, pairing incoming file values with existing customer values loaded from the API.</summary>
    internal static async Task<IReadOnlyList<ImportCustomerConflictState>> LoadConflictStates(
        ICustomersApiClient customersApi,
        IReadOnlyList<ImportConflictDto> conflicts,
        ReadOnlyMemory<byte> mappedFileBytes,
        CancellationToken ct)
    {
        var incomingConflictValuesByEmail = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(mappedFileBytes);
        var existingConflictValuesByEmail = await LoadExistingConflictValuesByEmail(customersApi, conflicts, ct);

        return conflicts
            .Select(conflict => new ImportCustomerConflictState(
                conflict.Email,
                incomingConflictValuesByEmail.GetValueOrDefault(conflict.Email),
                existingConflictValuesByEmail.GetValueOrDefault(conflict.Email)))
            .ToList()
            .AsReadOnly();
    }

    private static async Task<Dictionary<string, Dictionary<string, string>>> LoadExistingConflictValuesByEmail(
        ICustomersApiClient customersApi,
        IReadOnlyList<ImportConflictDto> conflicts,
        CancellationToken ct)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var emails = conflicts.Select(conflict => conflict.Email).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in emails.Chunk(ContractConstants.CustomerImportConflictLookupMaxKeys))
        {
            var idsOutcome = await customersApi.FindCustomerImportConflictIds(batch, ct);
            if (idsOutcome is not { Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded, Value: { } ids } || ids.Length != batch.Length)
            {
                throw new InvalidOperationException(LoadFailureMessage);
            }

            for (var index = 0; index < batch.Length; index++)
            {
                if (ids[index] is not { } id)
                {
                    continue;
                }

                var detailsOutcome = await customersApi.GetCustomerById(id, ct);
                if (detailsOutcome.Kind == SharedKernel.HttpClients.HttpOutcomeKind.NotFound)
                {
                    continue;
                }

                if (detailsOutcome is not { Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded, Value: { } details })
                {
                    throw new InvalidOperationException(LoadFailureMessage);
                }

                result[batch[index]] = MapCustomerDetailsToFieldValues(details);
            }
        }

        return result;
    }

    private static Dictionary<string, string> MapCustomerDetailsToFieldValues(CustomerDetailsDto details)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerImportFieldNames.FirstName] = details.PersonalInfo.FirstName,
            [CustomerImportFieldNames.LastName] = details.PersonalInfo.LastName,
            [CustomerImportFieldNames.Gender] = details.PersonalInfo.Gender,
            [CustomerImportFieldNames.BirthDate] = details.PersonalInfo.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            [CustomerImportFieldNames.Nationality] = details.PersonalInfo.Nationality,
            [CustomerImportFieldNames.Occupation] = details.PersonalInfo.Occupation,
            [CustomerImportFieldNames.NationalId] = details.IdentificationInfo.NationalId,
            [CustomerImportFieldNames.IdNationality] = details.IdentificationInfo.IdNationality,
            [CustomerImportFieldNames.Email] = details.ContactInfo.Email,
            [CustomerImportFieldNames.Mobile] = details.ContactInfo.Mobile,
            [CustomerImportFieldNames.Instagram] = details.ContactInfo.Instagram ?? string.Empty,
            [CustomerImportFieldNames.Facebook] = details.ContactInfo.Facebook ?? string.Empty,
            [CustomerImportFieldNames.Street] = details.Address.Street,
            [CustomerImportFieldNames.Complement] = details.Address.Complement ?? string.Empty,
            [CustomerImportFieldNames.Neighborhood] = details.Address.Neighborhood ?? string.Empty,
            [CustomerImportFieldNames.PostalCode] = details.Address.PostalCode,
            [CustomerImportFieldNames.City] = details.Address.City,
            [CustomerImportFieldNames.State] = details.Address.State,
            [CustomerImportFieldNames.Country] = details.Address.Country,
            [CustomerImportFieldNames.WeightKg] = details.PhysicalInfo.WeightKg.ToString(CultureInfo.InvariantCulture),
            [CustomerImportFieldNames.HeightCentimeters] = details.PhysicalInfo.HeightCentimeters.ToString(CultureInfo.InvariantCulture),
            [CustomerImportFieldNames.BikeType] = details.PhysicalInfo.BikeType.ToString(),
            [CustomerImportFieldNames.RoomType] = details.AccommodationPreferences.RoomType.ToString(),
            [CustomerImportFieldNames.BedType] = details.AccommodationPreferences.BedType.ToString(),
            [CustomerImportFieldNames.CompanionId] = details.AccommodationPreferences.CompanionId?.ToString() ?? string.Empty,
            [CustomerImportFieldNames.EmergencyContactName] = details.EmergencyContact.Name,
            [CustomerImportFieldNames.EmergencyContactMobile] = details.EmergencyContact.Mobile,
            [CustomerImportFieldNames.Allergies] = details.MedicalInfo.Allergies ?? string.Empty,
            [CustomerImportFieldNames.AdditionalInfo] = details.MedicalInfo.AdditionalInfo ?? string.Empty,
        };
    }
}
