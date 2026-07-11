using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class PaymentFormTests : BunitContext
{
    [Fact]
    public void Renders_all_form_fields()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var amountInput = cut.Find("input#amount");
        var paymentDateInput = cut.Find("input#paymentDate");
        var methodSelect = cut.Find("select#method");
        var referenceNumberInput = cut.Find("input#referenceNumber");
        var notesInput = cut.Find("textarea#notes");
        var submitButton = cut.Find("button[type='submit']");

        _ = (amountInput).ShouldNotBeNull();
        _ = (paymentDateInput).ShouldNotBeNull();
        _ = (methodSelect).ShouldNotBeNull();
        _ = (referenceNumberInput).ShouldNotBeNull();
        _ = (notesInput).ShouldNotBeNull();
        _ = (submitButton).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(CurrencyDto.UsDollar, "$")]
    [InlineData(CurrencyDto.Euro, "\u20ac")]
    [InlineData(CurrencyDto.Real, "R$")]
    public void Amount_field_has_currency_symbol(CurrencyDto currency, string expectedSymbol)
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Currency, currency)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var currencySymbol = cut.Find(".input-group-text");
        (currencySymbol.TextContent).ShouldBe(expectedSymbol);
    }

    [Fact]
    public void Payment_method_dropdown_contains_all_options()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var options = cut.FindAll("select#method option");
        (options.Count).ShouldBe(7); // Placeholder + 6 payment methods

        (options).ShouldContain(o => string.IsNullOrEmpty(o.GetAttribute("value")));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.CreditCard));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        (options).ShouldContain(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
    }

    [Fact]
    public void Payment_method_options_are_formatted_correctly()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var options = cut.FindAll("select#method option");

        var creditCardOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.CreditCard));
        (creditCardOption.TextContent).ShouldBe("Credit Card");

        var bankTransferOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        (bankTransferOption.TextContent).ShouldBe("Bank Transfer");

        var cashOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        (cashOption.TextContent).ShouldBe("Cash");

        var checkOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        (checkOption.TextContent).ShouldBe("Check");

        var paypalOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        (paypalOption.TextContent).ShouldBe("PayPal");

        var otherOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
        (otherOption.TextContent).ShouldBe("Other");
    }

    [Fact]
    public void Submit_button_shows_record_payment_text_when_not_submitting()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit)
            .Add(p => p.IsSubmitting, false));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.TextContent).ShouldContain("Record Payment", StringComparison.Ordinal);
        (submitButton.InnerHtml).ShouldContain("bi-cash-stack", StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_button_shows_recording_text_when_submitting()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit)
            .Add(p => p.IsSubmitting, true));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.TextContent).ShouldContain("Recording...", StringComparison.Ordinal);
        (submitButton.InnerHtml).ShouldContain("spinner-border", StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_button_is_disabled_when_submitting()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit)
            .Add(p => p.IsSubmitting, true));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.HasAttribute("disabled")).ShouldBeTrue();
    }

    [Fact]
    public void Cancel_button_is_hidden_when_oncancel_not_provided()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var cancelButtons = cut.FindAll("button[type='button']");
        (cancelButtons).ShouldBeEmpty();
    }

    [Fact]
    public void Cancel_button_is_shown_when_oncancel_provided()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });
        var onCancel = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit)
            .Add(p => p.OnCancel, onCancel));

        // Assert
        var cancelButton = cut.Find("button[type='button']");
        _ = (cancelButton).ShouldNotBeNull();
        (cancelButton.TextContent).ShouldContain("Cancel", StringComparison.Ordinal);
    }

    [Fact]
    public void Cancel_button_is_disabled_when_submitting()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });
        var onCancel = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit)
            .Add(p => p.OnCancel, onCancel)
            .Add(p => p.IsSubmitting, true));

        // Assert
        var cancelButton = cut.Find("button[type='button']");
        (cancelButton.HasAttribute("disabled")).ShouldBeTrue();
    }

    [Fact]
    public void Validation_error_shown_for_missing_amount()
    {
        // Arrange
        var model = new PaymentFormModel { Amount = null };
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-message");
        (validationMessages).ShouldContain(vm => vm.TextContent.Contains("Payment amount is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_error_shown_for_zero_amount()
    {
        // Arrange
        var model = new PaymentFormModel { Amount = 0 };
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-message");
        (validationMessages).ShouldContain(vm => vm.TextContent.Contains("Payment amount must be greater than zero", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_error_shown_for_missing_payment_date()
    {
        // Arrange
        var model = new PaymentFormModel { PaymentDate = null };
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-message");
        (validationMessages).ShouldContain(vm => vm.TextContent.Contains("Payment date is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_error_shown_for_missing_payment_method()
    {
        // Arrange
        var model = new PaymentFormModel { Method = null };
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        var validationMessages = cut.FindAll(".validation-message");
        (validationMessages).ShouldContain(vm => vm.TextContent.Contains("Payment method is required", StringComparison.Ordinal));
    }

    [Fact]
    public void OnValidSubmit_called_when_form_is_valid()
    {
        // Arrange
        var submitCalled = false;
        var model = new PaymentFormModel
        {
            Amount = 100.50m,
            PaymentDate = DateTime.Today,
            Method = PaymentMethodDto.CreditCard
        };
        var onValidSubmit = EventCallback.Factory.Create(this, () => submitCalled = true);

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        (submitCalled).ShouldBeTrue();
    }

    [Fact]
    public void OnValidSubmit_not_called_when_form_is_invalid()
    {
        // Arrange
        var submitCalled = false;
        var model = new PaymentFormModel { Amount = null }; // Invalid
        var onValidSubmit = EventCallback.Factory.Create(this, () => submitCalled = true);

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        var form = cut.Find("form");
        form.Submit();

        // Assert
        (submitCalled).ShouldBeFalse();
    }

    [Fact]
    public void Reference_number_field_has_placeholder_text()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var referenceNumberInput = cut.Find("input#referenceNumber");
        (referenceNumberInput.GetAttribute("placeholder")).ShouldBe("e.g., Transaction ID, Check Number");
    }

    [Fact]
    public void Notes_field_has_placeholder_text()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var notesInput = cut.Find("textarea#notes");
        (notesInput.GetAttribute("placeholder")).ShouldBe("Optional notes about this payment");
    }

    [Fact]
    public void Required_fields_have_asterisk_indicators()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var requiredIndicators = cut.FindAll("span.text-danger");
        (requiredIndicators.Count).ShouldBe(3); // Amount, PaymentDate, Method
        (requiredIndicators).ShouldAllSatisfy(indicator => (indicator.TextContent).ShouldBe("*"));
    }

    [Fact]
    public void Amount_input_has_step_attribute_for_decimals()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var amountInput = cut.Find("input#amount");
        (amountInput.GetAttribute("step")).ShouldBe("0.01");
    }

    [Fact]
    public void Notes_textarea_has_three_rows()
    {
        // Arrange
        var model = new PaymentFormModel();
        var onValidSubmit = EventCallback.Factory.Create(this, () => { });

        // Act
        var cut = Render<PaymentForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, onValidSubmit));

        // Assert
        var notesInput = cut.Find("textarea#notes");
        (notesInput.GetAttribute("rows")).ShouldBe("3");
    }
}
