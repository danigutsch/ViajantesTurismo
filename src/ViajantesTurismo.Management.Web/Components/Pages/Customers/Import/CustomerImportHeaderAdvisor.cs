using System.Net.Mail;
using System.Text;
using SharedKernel.Tabular;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>Suggests customer CSV mappings from known header labels and local sample shapes.</summary>
internal static class CustomerImportHeaderAdvisor
{
    private const string ExactHeaderReason = "Matches the expected header.";
    private const string RecognizedHeaderReason = "Recognized header name.";
    private const string EmailSampleReason = "Sample values look like email addresses.";

    private static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [CustomerImportFieldNames.FirstName] = ["Given name", "Nome", "Nome próprio"],
        [CustomerImportFieldNames.LastName] = ["Surname", "Family name", "Sobrenome"],
        [CustomerImportFieldNames.Gender] = ["Gênero", "Sexo"],
        [CustomerImportFieldNames.BirthDate] = ["Date of birth", "DOB", "Data de nascimento"],
        [CustomerImportFieldNames.Nationality] = ["Nacionalidade"],
        [CustomerImportFieldNames.Occupation] = ["Profissão", "Ocupação"],
        [CustomerImportFieldNames.NationalId] = ["CPF", "Documento de identidade"],
        [CustomerImportFieldNames.IdNationality] = ["Nacionalidade do documento"],
        [CustomerImportFieldNames.Email] = ["Email address", "Contact email", "Endereço de e-mail"],
        [CustomerImportFieldNames.Mobile] = ["Cell phone", "Mobile phone", "Celular", "Telefone celular"],
        [CustomerImportFieldNames.Street] = ["Street address", "Rua", "Logradouro"],
        [CustomerImportFieldNames.Complement] = ["Complemento"],
        [CustomerImportFieldNames.Neighborhood] = ["Bairro"],
        [CustomerImportFieldNames.PostalCode] = ["ZIP code", "CEP", "Código postal"],
        [CustomerImportFieldNames.City] = ["Cidade", "Município"],
        [CustomerImportFieldNames.State] = ["Estado", "UF"],
        [CustomerImportFieldNames.Country] = ["País"],
        [CustomerImportFieldNames.WeightKg] = ["Peso (kg)"],
        [CustomerImportFieldNames.HeightCentimeters] = ["Altura (cm)"],
        [CustomerImportFieldNames.BikeType] = ["Tipo de bicicleta"],
        [CustomerImportFieldNames.RoomType] = ["Tipo de quarto"],
        [CustomerImportFieldNames.BedType] = ["Tipo de cama"],
        [CustomerImportFieldNames.CompanionId] = ["ID do acompanhante"],
        [CustomerImportFieldNames.EmergencyContactName] = ["Nome do contato de emergência"],
        [CustomerImportFieldNames.EmergencyContactMobile] = ["Telefone de emergência"],
        [CustomerImportFieldNames.Allergies] = ["Alergias"],
        [CustomerImportFieldNames.AdditionalInfo] = ["Informações adicionais"],
    };

    private static readonly Dictionary<string, string> KnownLabels = CreateKnownLabels();

    /// <summary>Returns only the fields that have a non-exact suggestion or more than one plausible source.</summary>
    internal static IReadOnlyDictionary<string, List<CustomerImportColumnSuggestion>> Suggest(
        CsvDocument document,
        int maxSampleRows)
    {
        var candidates = new Dictionary<string, List<CustomerImportColumnSuggestion>>(StringComparer.Ordinal);
        var unknownColumns = new List<int>();
        for (var index = 0; index < document.Headers.Count; index++)
        {
            var header = document.Headers[index];
            if (!KnownLabels.TryGetValue(NormalizeLabel(header), out var field))
            {
                unknownColumns.Add(index);
                continue;
            }

            var reason = header.Equals(field, StringComparison.OrdinalIgnoreCase) ? ExactHeaderReason : RecognizedHeaderReason;
            if (!candidates.TryGetValue(field, out var fieldCandidates))
            {
                fieldCandidates = [];
                candidates.Add(field, fieldCandidates);
            }

            fieldCandidates.Add(new CustomerImportColumnSuggestion(header, reason));
        }

        if (!candidates.ContainsKey(CustomerImportFieldNames.Email))
        {
            var emailHeaders = unknownColumns
                .Where(column => SampleLooksLikeEmail(document.Rows, column, maxSampleRows))
                .Select(column => new CustomerImportColumnSuggestion(document.Headers[column], EmailSampleReason))
                .ToList();
            if (emailHeaders.Count > 0)
            {
                candidates.Add(CustomerImportFieldNames.Email, emailHeaders);
            }
        }

        return candidates
            .Where(candidate => candidate.Value.Count != 1 || candidate.Value[0].Reason != ExactHeaderReason)
            .ToDictionary(candidate => candidate.Key, candidate => candidate.Value, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> CreateKnownLabels()
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in CustomerImportHeaderMatcher.Fields)
        {
            labels.TryAdd(NormalizeLabel(field.Name), field.Name);
            labels.TryAdd(NormalizeLabel(field.DisplayName), field.Name);
            foreach (var alias in Aliases.GetValueOrDefault(field.Name) ?? [])
            {
                labels.TryAdd(NormalizeLabel(alias), field.Name);
            }
        }

        return labels;
    }

    private static string NormalizeLabel(string label) =>
        string.Concat(label.Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static bool SampleLooksLikeEmail(IReadOnlyList<CsvRow> rows, int column, int maxSampleRows)
    {
        var hasEmail = false;
        foreach (var row in rows.Take(maxSampleRows))
        {
            var value = row[column].Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (!MailAddress.TryCreate(value, out var address)
                || !string.Equals(value, address.Address, StringComparison.Ordinal)
                || !address.Host.Contains('.', StringComparison.Ordinal))
            {
                return false;
            }

            hasEmail = true;
        }

        return hasEmail;
    }
}
