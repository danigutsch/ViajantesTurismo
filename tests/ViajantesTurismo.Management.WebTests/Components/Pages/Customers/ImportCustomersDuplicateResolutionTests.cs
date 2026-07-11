using System.Text;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersDuplicateResolutionTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersDuplicateResolutionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void ConfirmImport_withconflicts_advancestoduplicateresolutionstep()
    {
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("existing@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        cut.WaitForAssertion(() =>
            (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_each_conflict_shows_keep_and_overwrite_buttons()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        var keepButtons = cut.FindAll("button[data-action='keep']");
        var overwriteButtons = cut.FindAll("button[data-action='overwrite']");

        (keepButtons.Count).ShouldBe(2);
        (overwriteButtons.Count).ShouldBe(2);
    }

    [Fact]
    public void DuplicateResolution_confirmisdisabled_untilallconflictsresolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        // No decisions made yet — confirm should be disabled
        (cut.Find("button[data-action='confirm-import']").HasAttribute("disabled")).ShouldBeTrue();

        // Resolve only one of two — still disabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "a@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        (cut.Find("button[data-action='confirm-import']").HasAttribute("disabled")).ShouldBeTrue();

        // Resolve the second — now enabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "b@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        (cut.Find("button[data-action='confirm-import']").HasAttribute("disabled")).ShouldBeFalse();
    }

    [Fact]
    public void DuplicateResolution_confirmimport_showsresultafterallconflictsresolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='keep']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            (cut.Markup).ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_shows_mixed_decision_and_field_source_controls()
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

        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();

        (cut.FindAll("button[data-action='field-existing']")).ShouldNotBeEmpty();
        (cut.FindAll("button[data-action='field-incoming']")).ShouldNotBeEmpty();
    }

    [Fact]
    public void DuplicateResolution_mixed_decision_is_sent_on_commit()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));

        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            (cut.Markup).ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));

        (_fakeCustomersApi.LastCommitConflictResolutions).ShouldNotBeNull();
        (_fakeCustomersApi.LastCommitConflictResolutions["a@example.com"]).ShouldBe("mixed");
    }

    [Fact]
    public void DuplicateResolution_mixed_decision_applies_selected_field_sources_to_committed_file()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        ImportCustomersDuplicateResolutionTestHelper.SeedExistingCustomer(_fakeCustomersApi, "a@example.com", "ExistingFirst", "ExistingLast");

        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.BuildCsvWithOverrides(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "IncomingFirst",
            ["LastName"] = "IncomingLast",
            ["Email"] = "a@example.com",
        }));

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        // Act
        cut.Find("button[data-action='mixed']").Click();
        cut.WaitForAssertion(() => (cut.FindAll("button[data-action='field-incoming']")).ShouldNotBeEmpty());

        var firstNameRow = ImportCustomersTestDomHelper.FindRowContainingText(cut, "table.table.table-sm.mb-0 tbody tr", "First Name");
        firstNameRow.QuerySelector("button[data-action='field-incoming']")!.Click();

        cut.Find("button[data-action='confirm-import']").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));

        // Assert
        (_fakeCustomersApi.LastCommitFileContent).ShouldNotBeNull();
        (_fakeCustomersApi.LastCommitFileName).ShouldBe("customers.csv");

        var committedCsv = Encoding.UTF8.GetString(_fakeCustomersApi.LastCommitFileContent.ToArray());
        var committedLines = committedCsv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        (committedLines.Length).ShouldBe(2);

        var committedHeaders = committedLines[0].Split(',');
        var committedValues = committedLines[1].Split(',');
        var headerIndexes = committedHeaders
            .Select((header, index) => new { header, index })
            .ToDictionary(item => item.header, item => item.index, StringComparer.Ordinal);

        (committedValues[headerIndexes["FirstName"]]).ShouldBe("IncomingFirst");
        (committedValues[headerIndexes["LastName"]]).ShouldBe("ExistingLast");
        (committedValues[headerIndexes["Email"]]).ShouldBe("a@example.com");
    }
}
