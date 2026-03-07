using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class PaymentFormTests : BunitContext
{
    [Fact]
    public void Renders_All_Form_Fields()
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

        Assert.NotNull(amountInput);
        Assert.NotNull(paymentDateInput);
        Assert.NotNull(methodSelect);
        Assert.NotNull(referenceNumberInput);
        Assert.NotNull(notesInput);
        Assert.NotNull(submitButton);
    }

    [Theory]
    [InlineData(CurrencyDto.UsDollar, "$")]
    [InlineData(CurrencyDto.Euro, "\u20ac")]
    [InlineData(CurrencyDto.Real, "R$")]
    public void Amount_Field_Has_Currency_Symbol(CurrencyDto currency, string expectedSymbol)
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
        Assert.Equal(expectedSymbol, currencySymbol.TextContent);
    }

    [Fact]
    public void Payment_Method_Dropdown_Contains_All_Options()
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
        Assert.Equal(7, options.Count); // Placeholder + 6 payment methods

        Assert.Contains(options, o => string.IsNullOrEmpty(o.GetAttribute("value")));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.CreditCard));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        Assert.Contains(options, o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
    }

    [Fact]
    public void Payment_Method_Options_Are_Formatted_Correctly()
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
        Assert.Equal("Credit Card", creditCardOption.TextContent);

        var bankTransferOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.BankTransfer));
        Assert.Equal("Bank Transfer", bankTransferOption.TextContent);

        var cashOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Cash));
        Assert.Equal("Cash", cashOption.TextContent);

        var checkOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Check));
        Assert.Equal("Check", checkOption.TextContent);

        var paypalOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.PayPal));
        Assert.Equal("PayPal", paypalOption.TextContent);

        var otherOption = options.First(o => o.GetAttribute("value") == nameof(PaymentMethodDto.Other));
        Assert.Equal("Other", otherOption.TextContent);
    }

    [Fact]
    public void Submit_Button_Shows_Record_Payment_Text_When_Not_Submitting()
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
        Assert.Contains("Record Payment", submitButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-cash-stack", submitButton.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_Button_Shows_Recording_Text_When_Submitting()
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
        Assert.Contains("Recording...", submitButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("spinner-border", submitButton.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_Button_Is_Disabled_When_Submitting()
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
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Cancel_Button_Is_Hidden_When_OnCancel_Not_Provided()
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
        Assert.Empty(cancelButtons);
    }

    [Fact]
    public void Cancel_Button_Is_Shown_When_OnCancel_Provided()
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
        Assert.NotNull(cancelButton);
        Assert.Contains("Cancel", cancelButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancel_Button_Is_Disabled_When_Submitting()
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
        Assert.True(cancelButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Validation_Error_Shown_For_Missing_Amount()
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
        Assert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment amount is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_Error_Shown_For_Zero_Amount()
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
        Assert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment amount must be greater than zero", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_Error_Shown_For_Missing_Payment_Date()
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
        Assert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment date is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_Error_Shown_For_Missing_Payment_Method()
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
        Assert.Contains(validationMessages, vm => vm.TextContent.Contains("Payment method is required", StringComparison.Ordinal));
    }

    [Fact]
    public void OnValidSubmit_Called_When_Form_Is_Valid()
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
        Assert.True(submitCalled);
    }

    [Fact]
    public void OnValidSubmit_Not_Called_When_Form_Is_Invalid()
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
        Assert.False(submitCalled);
    }

    [Fact]
    public void Reference_Number_Field_Has_Placeholder_Text()
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
        Assert.Equal("e.g., Transaction ID, Check Number", referenceNumberInput.GetAttribute("placeholder"));
    }

    [Fact]
    public void Notes_Field_Has_Placeholder_Text()
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
        Assert.Equal("Optional notes about this payment", notesInput.GetAttribute("placeholder"));
    }

    [Fact]
    public void Required_Fields_Have_Asterisk_Indicators()
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
        Assert.Equal(3, requiredIndicators.Count); // Amount, PaymentDate, Method
        Assert.All(requiredIndicators, indicator => Assert.Equal("*", indicator.TextContent));
    }

    [Fact]
    public void Amount_Input_Has_Step_Attribute_For_Decimals()
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
        Assert.Equal("0.01", amountInput.GetAttribute("step"));
    }

    [Fact]
    public void Notes_Textarea_Has_Three_Rows()
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
        Assert.Equal("3", notesInput.GetAttribute("rows"));
    }
}
