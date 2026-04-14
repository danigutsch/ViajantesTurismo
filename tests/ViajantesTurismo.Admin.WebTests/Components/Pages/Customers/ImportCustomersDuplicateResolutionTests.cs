using System.Text;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersDuplicateResolutionTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private static readonly string AllCanonicalValues =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(_ => "v"));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersDuplicateResolutionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    private static string BuildCsvWithEmail(string email)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(f => f.Name.Equals("Email", StringComparison.OrdinalIgnoreCase) ? email : "v");
        return AllCanonicalHeaders + "\n" + string.Join(",", values);
    }

    private static string BuildCsvWithOverrides(IReadOnlyDictionary<string, string> valuesByField)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => valuesByField.GetValueOrDefault(field.Name, "v"));

        return AllCanonicalHeaders + "\n" + string.Join(",", values);
    }

    private void SeedExistingCustomer(string email, string firstName, string lastName)
    {
        var customerId = Guid.NewGuid();

        _fakeCustomersApi.AddCustomer(new GetCustomerDto
        {
            Id = customerId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });

        _fakeCustomersApi.AddCustomerDetails(new CustomerDetailsDto
        {
            Id = customerId,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = firstName,
                LastName = lastName,
                Gender = "Female",
                BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                Nationality = "Brazilian",
                Occupation = "Engineer",
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "A123",
                IdNationality = "Brazilian",
            },
            ContactInfo = new ContactInfoDto
            {
                Email = email,
                Mobile = "+551111111111",
                Instagram = null,
                Facebook = null,
            },
            Address = new AddressDto
            {
                Street = "Rua A",
                Complement = null,
                Neighborhood = "Centro",
                PostalCode = "01000-000",
                City = "São Paulo",
                State = "SP",
                Country = "Brazil",
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 70,
                HeightCentimeters = 170,
                BikeType = BikeTypeDto.Regular,
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null,
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+55222222222",
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null,
            },
        });
    }

    private IRenderedComponent<ImportCustomers> GoToPreview(string csvContent, string fileName = "customers.csv")
    {
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(csvContent, fileName);
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        return cut;
    }

    [Fact]
    public void ConfirmImport_WithConflicts_AdvancesToDuplicateResolutionStep()
    {
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("existing@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_Each_Conflict_Shows_Keep_And_Overwrite_Buttons()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        var keepButtons = cut.FindAll("button[data-action='keep']");
        var overwriteButtons = cut.FindAll("button[data-action='overwrite']");

        Assert.Equal(2, keepButtons.Count);
        Assert.Equal(2, overwriteButtons.Count);
    }

    [Fact]
    public void DuplicateResolution_ConfirmIsDisabled_UntilAllConflictsResolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        // No decisions made yet — confirm should be disabled
        Assert.True(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));

        // Resolve only one of two — still disabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "a@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        Assert.True(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));

        // Resolve the second — now enabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "b@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        Assert.False(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));
    }

    [Fact]
    public void DuplicateResolution_ConfirmImport_ShowsResultAfterAllConflictsResolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        cut.Find("button[data-action='keep']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_Shows_Mixed_Decision_And_Field_Source_Controls()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var customerId = Guid.NewGuid();
        _fakeCustomersApi.AddCustomer(new GetCustomerDto
        {
            Id = customerId,
            FirstName = "Existing",
            LastName = "Customer",
            Email = "a@example.com",
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular
        });

        _fakeCustomersApi.AddCustomerDetails(new CustomerDetailsDto
        {
            Id = customerId,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Existing",
                LastName = "Customer",
                Gender = "Female",
                BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                Nationality = "Brazilian",
                Occupation = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "A123",
                IdNationality = "Brazilian"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "a@example.com",
                Mobile = "+551111111111",
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "Rua A",
                Complement = null,
                Neighborhood = "Centro",
                PostalCode = "01000-000",
                City = "São Paulo",
                State = "SP",
                Country = "Brazil"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 70,
                HeightCentimeters = 170,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+55222222222"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        });

        var cut = GoToPreview(BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();

        Assert.NotEmpty(cut.FindAll("button[data-action='field-existing']"));
        Assert.NotEmpty(cut.FindAll("button[data-action='field-incoming']"));
    }

    [Fact]
    public void DuplicateResolution_Mixed_Decision_Is_Sent_On_Commit()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));

        var cut = GoToPreview(BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));

        Assert.NotNull(_fakeCustomersApi.LastCommitConflictResolutions);
        Assert.Equal("mixed", _fakeCustomersApi.LastCommitConflictResolutions["a@example.com"]);
    }

    [Fact]
    public void DuplicateResolution_Mixed_Decision_Applies_Selected_Field_Sources_To_Committed_File()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        SeedExistingCustomer("a@example.com", "ExistingFirst", "ExistingLast");

        var cut = GoToPreview(BuildCsvWithOverrides(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "IncomingFirst",
            ["LastName"] = "IncomingLast",
            ["Email"] = "a@example.com",
        }));

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        // Act
        cut.Find("button[data-action='mixed']").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button[data-action='field-incoming']")));

        var firstNameRow = ImportCustomersTestDomHelper.FindRowContainingText(cut, "table.table.table-sm.mb-0 tbody tr", "First Name");
        firstNameRow.QuerySelector("button[data-action='field-incoming']")!.Click();

        cut.Find("button[data-action='confirm-import']").Click();
        cut.WaitForAssertion(() => Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.NotNull(_fakeCustomersApi.LastCommitFileContent);
        Assert.Equal("customers.csv", _fakeCustomersApi.LastCommitFileName);

        var committedCsv = Encoding.UTF8.GetString(_fakeCustomersApi.LastCommitFileContent.ToArray());
        var committedLines = committedCsv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, committedLines.Length);

        var committedHeaders = committedLines[0].Split(',');
        var committedValues = committedLines[1].Split(',');
        var headerIndexes = committedHeaders
            .Select((header, index) => new { header, index })
            .ToDictionary(item => item.header, item => item.index, StringComparer.Ordinal);

        Assert.Equal("IncomingFirst", committedValues[headerIndexes["FirstName"]]);
        Assert.Equal("ExistingLast", committedValues[headerIndexes["LastName"]]);
        Assert.Equal("a@example.com", committedValues[headerIndexes["Email"]]);
    }
}
