using PandaBear.Blazor;
using System.Globalization;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersDuplicateResolutionTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersDuplicateResolutionTests()
    {
        Services.AddUiFailureReporting();
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
            cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_each_conflict_shows_keep_and_overwrite_buttons()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        var keepButtons = cut.FindAll("button[data-action='keep']");
        var overwriteButtons = cut.FindAll("button[data-action='overwrite']");

        keepButtons.Count.ShouldBe(2);
        overwriteButtons.Count.ShouldBe(2);
    }

    [Fact]
    public void DuplicateResolution_confirmisdisabled_untilallconflictsresolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        // No decisions made yet — confirm should be disabled
        cut.Find("button[data-action='confirm-import']").HasAttribute("disabled").ShouldBeTrue();

        // Resolve only one of two — still disabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "a@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        cut.Find("button[data-action='confirm-import']").HasAttribute("disabled").ShouldBeTrue();

        // Resolve the second — now enabled
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "b@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        cut.Find("button[data-action='confirm-import']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void DuplicateResolution_confirmimport_showsresultafterallconflictsresolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='keep']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_commit_conflict_returns_to_resolution_instead_of_showing_success()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(
            this,
            CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
        cut.Find("button[data-action='keep']").Click();

        // Act
        cut.Find("button[data-action='confirm-import']").Click();

        // Assert
        cut.WaitForAssertion(() => _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull());
        cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Import complete.", StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateResolution_commit_conflict_does_not_show_a_completed_upload_for_the_file()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(
            this,
            CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
        cut.Find("button[data-action='keep']").Click();

        // Act
        cut.Find("button[data-action='confirm-import']").Click();

        // Assert
        cut.WaitForAssertion(() => _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull());
        cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Complete: ", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain(" complete.", StringComparison.Ordinal);
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
                BirthDate = DateOnly.FromDateTime(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
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
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();

        cut.FindAll("button[data-action='field-existing']").ShouldNotBeEmpty();
        cut.FindAll("button[data-action='field-incoming']").ShouldNotBeEmpty();
    }

    [Fact]
    public void DuplicateResolution_mixed_decision_is_sent_on_commit()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));

        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        cut.Find("button[data-action='mixed']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));

        _fakeCustomersApi.LastCommitConflictResolutions.ShouldNotBeNull();
        _fakeCustomersApi.LastCommitConflictResolutions["a@example.com"].ShouldBe("mixed");
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
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        // Act
        cut.Find("button[data-action='mixed']").Click();
        cut.WaitForAssertion(() => cut.FindAll("button[data-action='field-incoming']").ShouldNotBeEmpty());

        var firstNameRow = ImportCustomersTestDomHelper.FindRowContainingText(cut, "table.table.table-sm.mb-0 tbody tr", "First Name");
        firstNameRow.QuerySelector("button[data-action='field-incoming']")!.Click();

        cut.Find("button[data-action='confirm-import']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));

        // Assert
        _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull();
        _fakeCustomersApi.LastCommitFileName.ShouldBe("customers.csv");

        var committedValues = ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(
            [.. _fakeCustomersApi.LastCommitFileContent]);
        committedValues["FirstName"].ShouldBe("IncomingFirst");
        committedValues["LastName"].ShouldBe("ExistingLast");
        committedValues["Email"].ShouldBe("a@example.com");
    }

    [Fact]
    public async Task DuplicateResolution_commit_reports_progress_and_cancel_preserves_the_effective_mixed_file()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        ImportCustomersDuplicateResolutionTestHelper.SeedExistingCustomer(
            _fakeCustomersApi,
            "a@example.com",
            "ExistingFirst",
            "ExistingLast");
        var requestStarted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCompletion = new TaskCompletionSource<ImportResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        byte[]? committedContent = null;
        IProgress<long>? reportedProgress = null;
        _fakeCustomersApi.SetCommitImportHandler((content, _, _, progress, _) =>
        {
            committedContent = content;
            reportedProgress = progress;
            requestStarted.TrySetResult(content.LongLength);
            return requestCompletion.Task;
        });
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(
            this,
            CustomerImportCsvTestData.BuildCsvWithOverrides(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "IncomingFirst",
                ["LastName"] = "IncomingLast",
                ["Email"] = "a@example.com",
            }));
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
        await cut.Find("button[data-action='mixed']").ClickAsync();
        var firstNameRow = ImportCustomersTestDomHelper.FindRowContainingText(
            cut,
            "table.table.table-sm.mb-0 tbody tr",
            "First Name");
        var incomingButton = firstNameRow.QuerySelector("button[data-action='field-incoming']").ShouldNotBeNull();
        await incomingButton.ClickAsync();

        // Act
        var commitTask = cut.Find("button[data-action='confirm-import']").ClickAsync();
        var totalBytes = await requestStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        var progress = reportedProgress.ShouldNotBeNull();
        var transferredBytes = totalBytes / 2;
        progress.Report(transferredBytes);
        await cut.WaitForAssertionAsync(() =>
            cut.Find("progress[value]").GetAttribute("value").ShouldBe(transferredBytes.ToString(CultureInfo.InvariantCulture)));
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        requestCompletion.TrySetResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        await commitTask;

        // Assert
        _fakeCustomersApi.LastCommitCancellationToken.IsCancellationRequested.ShouldBeTrue();
        cut.Find("[data-upload-status]").TextContent.ShouldContain("Canceled", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Import complete", StringComparison.Ordinal);
        var effectiveContent = committedContent.ShouldNotBeNull();
        var committedValues = ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(effectiveContent);
        committedValues["FirstName"].ShouldBe("IncomingFirst");
        committedValues["LastName"].ShouldBe("ExistingLast");
    }

    [Fact]
    public async Task Canceled_conflict_lookup_cannot_replace_newer_retry_conflicts()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(
            this,
            CustomerImportCsvTestData.BuildCsvWithEmail("a@example.com"));
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
        await cut.Find("button[data-action='keep']").ClickAsync();

        var commitCount = 0;
        _fakeCustomersApi.SetCommitImportHandler((_, _, _, _, _) =>
        {
            var conflictEmail = Interlocked.Increment(ref commitCount) == 1
                ? "old@example.com"
                : "new@example.com";
            return Task.FromResult(new ImportResultDto(0, 0, [new ImportConflictDto(conflictEmail)]));
        });
        var oldLookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldLookupCompletion = new TaskCompletionSource<IReadOnlyList<GetCustomerDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupCount = 0;
        _fakeCustomersApi.SetGetCustomersHandler((_, _) =>
        {
            if (Interlocked.Increment(ref lookupCount) == 1)
            {
                oldLookupStarted.TrySetResult();
                return oldLookupCompletion.Task;
            }

            return Task.FromResult<IReadOnlyList<GetCustomerDto>>([]);
        });

        // Act
        var oldCommit = cut.Find("button[data-action='confirm-import']").ClickAsync();
        await oldLookupStarted.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await cut.Find("button[aria-label='Cancel upload of customers.csv']").ClickAsync();
        await cut.Find("button[data-action='confirm-import']").ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("new@example.com", StringComparison.Ordinal));
        oldLookupCompletion.TrySetResult([]);
        await oldCommit;

        // Assert
        cut.Markup.ShouldContain("new@example.com", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("old@example.com", StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateResolution_retry_uses_the_effective_mixed_file_displayed_after_a_conflict()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        ImportCustomersDuplicateResolutionTestHelper.SeedExistingCustomer(
            _fakeCustomersApi,
            "a@example.com",
            "ExistingFirst",
            "ExistingLast");
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(
            this,
            CustomerImportCsvTestData.BuildCsvWithOverrides(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "IncomingFirst",
                ["LastName"] = "IncomingLast",
                ["Email"] = "a@example.com",
            }));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve Duplicates", StringComparison.Ordinal));
        cut.Find("button[data-action='mixed']").Click();
        var firstNameRow = ImportCustomersTestDomHelper.FindRowContainingText(
            cut,
            "table.table.table-sm.mb-0 tbody tr",
            "First Name");
        var incomingButton = firstNameRow.QuerySelector("button[data-action='field-incoming']").ShouldNotBeNull();
        incomingButton.Click();
        cut.Find("button[data-action='confirm-import']").Click();
        cut.WaitForAssertion(() => _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull());
        var firstCommitFileContent = _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull();
        var firstCommitValues = ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(
            [.. firstCommitFileContent]);
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));

        // Act
        cut.Find("button[data-action='overwrite']").Click();
        cut.Find("button[data-action='confirm-import']").Click();
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));
        var retryFileContent = _fakeCustomersApi.LastCommitFileContent.ShouldNotBeNull();
        var retryValues = ImportCustomersDuplicateResolutionTestHelper.ParseSingleDataRow(
            [.. retryFileContent]);

        // Assert
        firstCommitValues["FirstName"].ShouldBe("IncomingFirst");
        firstCommitValues["LastName"].ShouldBe("ExistingLast");
        retryValues["FirstName"].ShouldBe("IncomingFirst");
        retryValues["LastName"].ShouldBe("ExistingLast");
    }
}
