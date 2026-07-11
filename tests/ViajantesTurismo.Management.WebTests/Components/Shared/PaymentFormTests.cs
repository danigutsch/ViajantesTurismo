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

        _ = TestAssert.NotNull(amountInput);
        _ = TestAssert.NotNull(paymentDateInput);
        _ = TestAssert.NotNull(methodSelect);
        _ = TestAssert.NotNull(referenceNumberInput);
        _ = TestAssert.NotNull(notesInput);
        _ = TestAssert.NotNull(submitButton);
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
        TestAssert.Equal(expectedSymbol, currencySymbol.TextContent);
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
        TestAssert.Equal(7, options.Count); // Placeholder + 6 payment methods

        TestAssert.Contains(options, o => string.IsNullOrEmpty(o.GetAttribute("value")));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.CreditCard));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        TestAssert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
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
        TestAssert.Equal("Credit Card", creditCardOption.TextContent);

        var bankTransferOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        TestAssert.Equal("Bank Transfer", bankTransferOption.TextContent);

        var cashOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        TestAssert.Equal("Cash", cashOption.TextContent);

        var checkOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        TestAssert.Equal("Check", checkOption.TextContent);

        var paypalOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        TestAssert.Equal("PayPal", paypalOption.TextContent);

        var otherOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
        TestAssert.Equal("Other", otherOption.TextContent);
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
        TestAssert.Contains("Record Payment", submitButton.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-cash-stack", submitButton.InnerHtml, StringComparison.Ordinal);
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
        TestAssert.Contains("Recording...", submitButton.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("spinner-border", submitButton.InnerHtml, StringComparison.Ordinal);
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
        TestAssert.True(submitButton.HasAttribute("disabled"));
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
        TestAssert.Empty(cancelButtons);
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
        _ = TestAssert.NotNull(cancelButton);
        TestAssert.Contains("Cancel", cancelButton.TextContent, StringComparison.Ordinal);
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
        TestAssert.True(cancelButton.HasAttribute("disabled"));
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
        TestAssert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment amount is required", StringComparison.Ordinal));
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
        TestAssert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment amount must be greater than zero", StringComparison.Ordinal));
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
        TestAssert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment date is required", StringComparison.Ordinal));
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
        TestAssert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment method is required", StringComparison.Ordinal));
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
        TestAssert.True(submitCalled);
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
        TestAssert.False(submitCalled);
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
        TestAssert.Equal("e.g., Transaction ID, Check Number", referenceNumberInput.GetAttribute("placeholder"));
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
        TestAssert.Equal("Optional notes about this payment", notesInput.GetAttribute("placeholder"));
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
        TestAssert.Equal(3, requiredIndicators.Count); // Amount, PaymentDate, Method
        TestAssert.All(requiredIndicators, indicator => TestAssert.Equal("*", indicator.TextContent));
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
        TestAssert.Equal("0.01", amountInput.GetAttribute("step"));
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
        TestAssert.Equal("3", notesInput.GetAttribute("rows"));
    }
}
