using SharedKernel.Testing;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using ViajantesTurismo.Management.Web.Components.Pages.Bookings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SharedKernel.HttpClients;
using System.Net;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

[TestScope(TestScope.Component)]
public class CustomerBookingCreateFormTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly FakeToursApiClient _fakeToursApi = new();
    private readonly RecordingUiFailureReporter _failures = new();

    public CustomerBookingCreateFormTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
        Services.AddSingleton<SharedKernel.Blazor.IUiFailureReporter>(_failures);
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Late_tour_details_do_not_replace_a_newer_choice()
    {
        // Arrange
        var older = BuildTourDto(name: "Older Tour", maxCustomers: 12, currentCustomerCount: 10);
        var newer = BuildTourDto(name: "Newer Tour", maxCustomers: 12, currentCustomerCount: 5);
        _fakeToursApi.AddTours([older, newer]);
        var olderStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var olderResult = new TaskCompletionSource<HttpOutcome<GetTourDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fakeToursApi.SetGetTourByIdHandler((id, ct) =>
        {
            if (id == older.Id)
            {
                olderStarted.SetResult(ct);
                return olderResult.Task;
            }

            return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, newer));
        });
        var model = new BookingFormModel();
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters.Add(form => form.Model, model));
        cut.WaitForAssertion(() => cut.FindAll("#tourId option").Count.ShouldBe(3));
        var testCancellation = Xunit.TestContext.Current.CancellationToken;

        // Act
        var previous = cut.Find("#tourId").ChangeAsync(new ChangeEventArgs { Value = older.Id.ToString() });
        var oldToken = await olderStarted.Task.WaitAsync(testCancellation);
        cut.WaitForAssertion(() => cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue());
        await cut.Find("#tourId").ChangeAsync(new ChangeEventArgs { Value = newer.Id.ToString() });
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("7 spots available", StringComparison.Ordinal));
        var newerRender = cut.RenderCount;
        await cut.InvokeAsync(() => olderResult.SetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, older)));
        await previous;
        cut.WaitForState(() => cut.RenderCount > newerRender);

        // Assert
        oldToken.IsCancellationRequested.ShouldBeTrue();
        model.TourId.ShouldBe(newer.Id);
        cut.Markup.ShouldContain("7 spots available", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("2 spots available", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unavailable_selected_tour_blocks_submission_until_details_are_loaded()
    {
        // Arrange
        var tour = BuildTourDto();
        _fakeToursApi.AddTour(tour);
        var attempts = 0;
        _fakeToursApi.SetGetTourByIdHandler((_, _) => Task.FromResult(++attempts == 1
            ? HttpOutcomeMapper.Status<GetTourDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, tour)));
        var customerId = Guid.NewGuid();
        var model = new BookingFormModel { CustomerId = customerId, PrincipalBikeType = BikeTypeDto.Regular };
        var submits = 0;
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.CurrentCustomerId, customerId)
            .Add(form => form.OnValidSubmit, EventCallback.Factory.Create<BookingFormModel>(this, _ => submits++)));
        cut.WaitForAssertion(() => cut.FindAll("#tourId option").Count.ShouldBe(2));

        // Act
        await cut.Find("#tourId").ChangeAsync(new ChangeEventArgs { Value = tour.Id.ToString() });
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("The selected tour is no longer available.", StringComparison.Ordinal));
        var disabledBeforeRetry = cut.Find("button[type='submit']").HasAttribute("disabled");
        await cut.Find("form").SubmitAsync();
        var submitsBeforeRetry = submits;
        await cut.Find("button:contains('Try again')").ClickAsync();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Price Breakdown", StringComparison.Ordinal));
        await cut.Find("form").SubmitAsync();

        // Assert
        disabledBeforeRetry.ShouldBeTrue();
        submitsBeforeRetry.ShouldBe(0);
        submits.ShouldBe(1);
    }

    [Fact]
    public async Task Tour_search_can_recover_after_an_API_failure()
    {
        // Arrange
        var tour = BuildTourDto(name: "Serra Waters");
        var choice = new GetTourOptionDto(tour.Id, tour.Name, tour.Identifier, tour.StartDate);
        var failure = new InvalidOperationException("Fixture transport failure");
        _fakeToursApi.SetSearchTourOptionsHandler((request, _) => string.IsNullOrEmpty(request.Query)
            ? Task.FromException<HttpOutcome<GetTourOptionsDto>>(failure)
            : Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetTourOptionsDto([choice], 1))));
        var cut = Render<CustomerBookingCreateForm>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Tours could not be loaded for booking selection.", StringComparison.Ordinal));
        var initialMarkup = cut.Markup;

        // Act
        await cut.Find("#tourSearch").InputAsync(new ChangeEventArgs { Value = "Serra" });

        // Assert
        initialMarkup.ShouldNotContain(failure.Message, StringComparison.Ordinal);
        _failures.Reports.ShouldHaveSingleItem().Operation.ShouldBe("bookings.create.load-tours");
        cut.Markup.ShouldNotContain("Tours could not be loaded for booking selection.", StringComparison.Ordinal);
        cut.Find("#tourId").TextContent.ShouldContain(tour.Name, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_keeps_the_selected_tour_visible_when_the_new_options_do_not_match()
    {
        // Arrange
        var tour = BuildTourDto(name: "Retained tour");
        _fakeToursApi.AddTour(tour);
        var model = new BookingFormModel { TourId = tour.Id };
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters.Add(form => form.Model, model));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Price Breakdown", StringComparison.Ordinal));

        // Act
        await cut.Find("#tourSearch").InputAsync(new ChangeEventArgs { Value = "No matching choices" });

        // Assert
        model.TourId.ShouldBe(tour.Id);
        cut.Markup.ShouldContain("No tours match.", StringComparison.Ordinal);
        var retained = cut.FindAll("#tourId option")
            .ShouldHaveSingleItem(option => option.GetAttribute("value") == tour.Id.ToString());
        retained.TextContent.ShouldContain(tour.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_companion_dropdown()
    {
        // Arrange
        var currentCustomerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: currentCustomerId, firstName: "Current", lastName: "Customer"),
            BuildCustomerDto(firstName: "Companion", lastName: "Person")
        };
        var model = new BookingFormModel();

        _fakeCustomersApi.AddCustomers(customers);

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelect = cut.FindAll("select.form-select")[1]; // Second select
        var options = companionSelect.QuerySelectorAll("option");
        options.Length.ShouldBe(2); // "No Companion" + 1 other customer
        options[0].TextContent.ShouldBe("-- No Companion --");
        options.ShouldNotContain(o => o.TextContent.Contains("Current Customer", StringComparison.Ordinal));
        options.ShouldContain(o => o.TextContent.Contains("Companion Person", StringComparison.Ordinal));
    }

    [Fact]
    public void Companion_dropdown_excludes_current_customer()
    {
        // Arrange
        var currentCustomerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: currentCustomerId, firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith"),
            BuildCustomerDto(firstName: "Charlie", lastName: "Jones")
        };
        var model = new BookingFormModel();

        _fakeCustomersApi.AddCustomers(customers);

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelect = cut.FindAll("select.form-select")[1];
        var options = companionSelect.QuerySelectorAll("option");
        options.Length.ShouldBe(3); // Placeholder + 2 other customers (Alice excluded)
        options.ShouldNotContain(o => o.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        options.ShouldContain(o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        options.ShouldContain(o => o.TextContent.Contains("Charlie Jones", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_notes_textarea()
    {
        // Arrange
        var model = new BookingFormModel { Notes = "Special requirements" };

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        notesTextArea.GetAttribute("value").ShouldBe("Special requirements");
        notesTextArea.GetAttribute("rows").ShouldBe("3");
    }

    [Fact]
    public void Renders_discount_card()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountCard = cut.FindAll(".card").First(c => c.TextContent.Contains("Discount", StringComparison.Ordinal));
        discountCard.QuerySelector(".card-title")!.TextContent.ShouldContain("Discount (Optional)", StringComparison.Ordinal);
        discountCard.InnerHtml.ShouldContain("bi-percent", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_discounttype_dropdown()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountTypeSelect = cut.Find("select#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        options.Length.ShouldBe(3);
        options[0].TextContent.ShouldContain("No Discount", StringComparison.Ordinal);
        options[1].TextContent.ShouldContain("Percentage (0-100%)", StringComparison.Ordinal);
        options[2].TextContent.ShouldContain("Absolute Amount (in tour currency)", StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_discount_fields_when_discounttype_is_none()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.None };

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var labels = cut.FindAll("label");
        labels.ShouldNotContain(l => l.TextContent.Contains("Discount Percentage", StringComparison.Ordinal));
        labels.ShouldNotContain(l => l.TextContent.Contains("Discount Amount", StringComparison.Ordinal));
        labels.ShouldNotContain(l => l.TextContent.Contains("Discount Reason", StringComparison.Ordinal));
    }

    [Fact]
    public void Shows_discount_percentage_fields_when_discounttype_is_percentage()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 10m,
            DiscountReason = "Loyalty discount"
        };

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var percentageLabel = cut.Find("label:contains('Discount Percentage')");
        _ = percentageLabel.ShouldNotBeNull();

        var discountAmountInput = cut.Find("input#discountAmount");
        discountAmountInput.GetAttribute("value").ShouldBe("10");

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        _ = helpText.ShouldNotBeNull();

        var reasonTextArea = cut.Find("textarea#discountReason");
        reasonTextArea.GetAttribute("value").ShouldBe("Loyalty discount");
    }

    [Fact]
    public void Shows_discount_amount_fields_when_discounttype_is_absolute()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 50m,
            DiscountReason = "Special promotion"
        };

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var amountLabel = cut.Find("label:contains('Discount Amount')");
        _ = amountLabel.ShouldNotBeNull();
        amountLabel.TextContent.ShouldNotContain("Percentage", StringComparison.Ordinal);

        var discountAmountInput = cut.Find("input#discountAmount");
        discountAmountInput.GetAttribute("value").ShouldBe("50");

        var helpText = cut.Find(".form-text:contains('Enter the discount amount in the tour currency')");
        _ = helpText.ShouldNotBeNull();

        var reasonTextArea = cut.Find("textarea#discountReason");
        reasonTextArea.GetAttribute("value").ShouldBe("Special promotion");
    }

    [Fact]
    public void Renders_discount_reason_placeholder_and_help_text()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Percentage };

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var reasonTextArea = cut.Find("textarea#discountReason");
        reasonTextArea.GetAttribute("placeholder").ShouldBe("e.g., Early bird discount, Loyalty customer, Group discount");
        reasonTextArea.GetAttribute("rows").ShouldBe("2");

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        helpText.TextContent.ShouldContain("10-500 characters", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_create_button()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        createButton.TextContent.ShouldContain("Create Booking", StringComparison.Ordinal);
        createButton.ClassName.ShouldContain("btn-primary", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_cancel_button()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        cancelButton.ClassName.ShouldContain("btn-secondary", StringComparison.Ordinal);
    }

    [Fact]
    public void Create_button_shows_spinner_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        var spinner = createButton.QuerySelector(".spinner-border");
        _ = spinner.ShouldNotBeNull();
        spinner.ClassName.ShouldContain("spinner-border-sm", StringComparison.Ordinal);
    }

    [Fact]
    public void Buttons_are_disabled_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        createButton.HasAttribute("disabled").ShouldBeTrue();
        cancelButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task OnCancel_is_called_when_cancel_button_is_clicked()
    {
        // Arrange
        var model = new BookingFormModel();
        var cancelCalled = false;

        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        await cancelButton.ClickAsync(new MouseEventArgs());

        // Assert
        cancelCalled.ShouldBeTrue();
    }

    [Fact]
    public void Renders_DataAnnotationsValidator()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        _ = validator.ShouldNotBeNull();
    }

    [Fact]
    public void Tour_and_companion_are_in_two_column_layout()
    {
        // Arrange
        var model = new BookingFormModel();

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var row = cut.Find(".row");
        var columns = row.QuerySelectorAll(".col-md-6");
        columns.Length.ShouldBe(2);

        var tourColumn = columns[0];
        tourColumn.TextContent.ShouldContain("Tour", StringComparison.Ordinal);

        var companionColumn = columns[1];
        companionColumn.TextContent.ShouldContain("Companion", StringComparison.Ordinal);
    }
}
