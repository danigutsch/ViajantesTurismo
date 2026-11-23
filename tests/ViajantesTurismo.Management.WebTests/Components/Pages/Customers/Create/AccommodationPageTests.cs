using PandaBear.Testing;
using Microsoft.AspNetCore.Components;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

[TestScope(TestScope.Component)]
public sealed class AccommodationPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly CustomerCreationState _state = new();
    private readonly RecordingUiFailureReporter _failures = new();

    public AccommodationPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(_failures);
        Services.AddSingleton(_state);
    }

    [Fact]
    public async Task Renders_a_bounded_companion_search_in_accommodation_wizard_step()
    {
        // Arrange
        foreach (var index in Enumerable.Range(0, 800))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(
                firstName: $"Customer{index:D3}", lastName: "Example", email: $"customer-{index:D3}@example.test"));
        }

        // Act
        var cut = Render<Accommodation>();
        cut.WaitForAssertion(() => cut.Find("[role='status']").TextContent.ShouldContain("20 of 800 customers", StringComparison.Ordinal));
        var initialOptions = cut.FindAll("#companionId option").Count;
        await cut.Find("#companionSearch").InputAsync(new ChangeEventArgs { Value = "customer799" });

        // Assert
        cut.FindAll("input#companionSearch[type='search']").ShouldHaveSingleItem();
        initialOptions.ShouldBe(21);
        cut.Find("#companionId").TextContent.ShouldContain("Customer799 Example", StringComparison.Ordinal);
        cut.FindAll("#companionId option").Count.ShouldBe(2);
        _fakeCustomersApi.LastGetCustomersMaxItems.ShouldBeNull();
    }

    [Fact]
    public async Task OnInitialized_when_state_already_has_accommodationpreferences_preloads_existing_values()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        foreach (var index in Enumerable.Range(0, AdminContractConstants.MaxCustomerOptions))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(
                firstName: $"Ada{index:D3}", lastName: "Example", email: $"other-{index:D3}@example.test"));
        }

        _fakeCustomersApi.AddCustomer(BuildCustomerDto(
            id: companionId,
            firstName: "Alice",
            lastName: "Brown",
            email: "alice@example.com"));
        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            BedType = BedTypeDto.DoubleBed,
            CompanionId = companionId,
        });

        // Act
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Find("#roomType").GetAttribute("value").ShouldBe(nameof(RoomTypeDto.SingleOccupancy));
        cut.Find("#bedType").GetAttribute("value").ShouldBe(nameof(BedTypeDto.DoubleBed));
        cut.Markup.ShouldContain("alice@example.com", StringComparison.Ordinal);
        cut.Find("#companionId").GetAttribute("value").ShouldBe(companionId.ToString());
        _state.CurrentStep.ShouldBe(6);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_emergency_contact()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(
            id: companionId,
            firstName: "Alice",
            lastName: "Brown",
            email: "alice@example.com"));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.InvokeAsync(() => cut.Find("#roomType").Change(nameof(RoomTypeDto.DoubleOccupancy)));
        await cut.InvokeAsync(() => cut.Find("#bedType").Change(nameof(BedTypeDto.SingleBed)));
        await cut.InvokeAsync(() => cut.Find("#companionId").Change(companionId.ToString()));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => navigationManager.Uri.ShouldEndWith("/customers/create/emergency-contact", StringComparison.Ordinal));
        _state.AccommodationPreferences.ShouldNotBeNull();
        _state.AccommodationPreferences.RoomType.ShouldBe(RoomTypeDto.DoubleOccupancy);
        _state.AccommodationPreferences.BedType.ShouldBe(BedTypeDto.SingleBed);
        _state.AccommodationPreferences.CompanionId.ShouldBe(companionId);
        _state.CurrentStep.ShouldBe(7);
    }

    [Fact]
    public async Task Back_button_navigates_to_physical_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => navigationManager.Uri.ShouldEndWith("/customers/create/physical", StringComparison.Ordinal));
        _state.CurrentStep.ShouldBe(5);
    }

    [Fact]
    public void Transport_failure_while_loading_companions_shows_a_safe_error_message()
    {
        // Arrange
        _fakeCustomersApi.SetSearchCustomerOptionsHandler((_, _) =>
            Task.FromResult(FakeApiClientOutcomes.TransportFailure<GetCustomerOptionsDto>()));

        // Act
        var cut = Render<Accommodation>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("Customers could not be loaded for companion selection."));
    }

    [Fact]
    public async Task Removing_the_optional_choice_clears_the_saved_companion()
    {
        // Arrange
        var companion = BuildCustomerDto(firstName: "Alice", lastName: "Brown");
        _fakeCustomersApi.AddCustomer(companion);
        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            BedType = BedTypeDto.DoubleBed,
            CompanionId = companion.Id,
        });
        var cut = Render<Accommodation>();
        cut.WaitForAssertion(() => cut.Find("#companionId").TextContent.ShouldContain("Alice Brown", StringComparison.Ordinal));

        // Act
        await cut.InvokeAsync(() => cut.Find("#companionId").Change(string.Empty));
        await cut.Find("form").SubmitAsync();

        // Assert
        var saved = _state.AccommodationPreferences.ShouldNotBeNull();
        saved.CompanionId.ShouldBeNull();
        _state.CurrentStep.ShouldBe(7);
    }

    [Fact]
    public void Search_exception_reports_the_operation_and_shows_a_safe_error()
    {
        // Arrange
        _fakeCustomersApi.SetSearchCustomerOptionsHandler((_, _) =>
            throw new InvalidOperationException("sensitive test input"));

        // Act
        var cut = Render<Accommodation>();
        var alert = cut.WaitForElement("[role='alert']");

        // Assert
        alert.TextContent.ShouldBe("Customers could not be loaded for companion selection.");
        alert.TextContent.ShouldNotContain("sensitive test input", StringComparison.Ordinal);
        _failures.Reports.ShouldBe([(nameof(InvalidOperationException), "customers.companion-options.search")]);
    }

    [Fact]
    public async Task Invalid_companion_value_does_not_advance_the_wizard()
    {
        // Arrange
        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            BedType = BedTypeDto.DoubleBed,
        });
        var cut = Render<Accommodation>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#companionId").Change("invalid-companion"));
        await cut.Find("form").SubmitAsync();

        // Assert
        var errors = cut.FindAll(".validation-message").Select(element => element.TextContent);
        errors.ShouldContain("Select a valid companion.");
        _state.CurrentStep.ShouldBe(6);
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Superseded_search_is_cancelled_and_cannot_replace_the_latest_choices()
    {
        // Arrange
        var staleSearch = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var staleResponse = new TaskCompletionSource<HttpOutcome<GetCustomerOptionsDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newest = new GetCustomerOptionDto(Guid.NewGuid(), "New", "Choice", "new@example.test", BikeTypeDto.Regular);
        _fakeCustomersApi.SetSearchCustomerOptionsHandler((request, ct) =>
        {
            if (request.Query == "old")
            {
                staleSearch.SetResult(ct);
                return staleResponse.Task;
            }

            var result = request.Query == "new"
                ? new GetCustomerOptionsDto([newest], 1)
                : new GetCustomerOptionsDto([], 0);
            return Task.FromResult(HttpOutcomeMapper.Succeeded(System.Net.HttpStatusCode.OK, result));
        });
        var cut = Render<Accommodation>();
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Act
        var previous = cut.Find("#companionSearch").InputAsync(new ChangeEventArgs { Value = "old" });
        var superseded = await staleSearch.Task.WaitAsync(ct);
        await cut.Find("#companionSearch").InputAsync(new ChangeEventArgs { Value = "new" });
        var old = new GetCustomerOptionDto(Guid.NewGuid(), "Old", "Choice", "old@example.test", BikeTypeDto.Regular);
        staleResponse.SetResult(HttpOutcomeMapper.Succeeded(System.Net.HttpStatusCode.OK, new GetCustomerOptionsDto([old], 1)));
        await previous;

        // Assert
        superseded.IsCancellationRequested.ShouldBeTrue();
        cut.Find("#companionId").TextContent.ShouldContain("New Choice", StringComparison.Ordinal);
        cut.Find("#companionId").TextContent.ShouldNotContain("Old Choice", StringComparison.Ordinal);
        cut.Find("[role='status'][aria-atomic='true']").TextContent.ShouldContain("1 of 1 customers", StringComparison.Ordinal);
    }
}
