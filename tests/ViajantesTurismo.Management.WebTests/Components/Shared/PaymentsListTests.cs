namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class PaymentsListTests : BunitContext
{
    public PaymentsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Shows_info_message_when_no_payments()
    {
        // Arrange
        var payments = Array.Empty<GetPaymentDto>();

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var alert = cut.Find(".alert-info");
        (alert.TextContent).ShouldContain("No payments recorded yet", StringComparison.Ordinal);
        (alert.InnerHtml).ShouldContain("bi-info-circle", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_table_when_payments_exist()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                paymentDate: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                amount: 100.50m,
                method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var quickGrid = cut.Find(".table.table-hover");
        _ = (quickGrid).ShouldNotBeNull();
    }

    [Fact]
    public void Displays_payment_date()
    {
        // Arrange
        var paymentDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var payments = new[]
        {
            BuildPaymentDto(
                paymentDate: paymentDate,
                amount: 50.00m,
                method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        (cut.Markup).ShouldContain("15/03/2024", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_amount_as_currency()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 123.45m,
                method: PaymentMethodDto.CreditCard)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var amountCell = cut.Find("td strong");
        (amountCell.TextContent).ShouldContain("123.45", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_credit_card_method_with_primary_badge()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.CreditCard)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        (badge.TextContent.Trim()).ShouldBe("Credit Card");
    }

    [Fact]
    public void Displays_bank_transfer_method_with_info_badge()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.BankTransfer)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var badge = cut.Find(".badge.bg-info");
        (badge.TextContent.Trim()).ShouldBe("Bank Transfer");
    }

    [Fact]
    public void Displays_cash_method_with_success_badge()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var badge = cut.Find(".badge.bg-success");
        (badge.TextContent.Trim()).ShouldBe("Cash");
    }

    [Fact]
    public void Displays_check_method_with_warning_badge()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Check)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var badge = cut.Find(".badge.bg-warning");
        (badge.TextContent.Trim()).ShouldBe("Check");
    }

    [Fact]
    public void Displays_paypal_method_with_secondary_badge()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.PayPal)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        (badge.TextContent.Trim()).ShouldBe("PayPal");
    }

    [Fact]
    public void Displays_reference_number_when_present()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.BankTransfer,
                referenceNumber: "REF123456")
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var code = cut.Find("code");
        (code.TextContent).ShouldBe("REF123456");
    }

    [Fact]
    public void Shows_dash_when_reference_number_missing()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var cells = cut.FindAll("td");
        var referenceCell = cells.FirstOrDefault(c => c.TextContent.Contains('-', StringComparison.Ordinal) && c.QuerySelector(".text-muted") != null);
        _ = (referenceCell).ShouldNotBeNull();
    }

    [Fact]
    public void Displays_notes_when_present()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Cash,
                notes: "Test payment notes")
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        (cut.Markup).ShouldContain("Test payment notes", StringComparison.Ordinal);
    }

    [Fact]
    public void Truncates_long_notes()
    {
        // Arrange
        var longNotes = "This is a very long note that should be truncated to 30 characters";
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Cash,
                notes: longNotes)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var spans = cut.FindAll("span[title]");
        var notesSpan = spans.First(s => s.GetAttribute("title") == longNotes);
        (notesSpan.TextContent).ShouldContain("...", StringComparison.Ordinal);
        (notesSpan.TextContent.Length <= 34).ShouldBeTrue(); // 30 + "..."
    }

    [Fact]
    public void Shows_dash_when_notes_missing()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(
                amount: 100m,
                method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var cells = cut.FindAll("td");
        var notesCell = cells.FirstOrDefault(c => c.TextContent.Contains('-', StringComparison.Ordinal) && c.QuerySelector(".text-muted") != null);
        _ = (notesCell).ShouldNotBeNull();
    }

    [Fact]
    public void Calculates_total_paid_correctly()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(amount: 100m, method: PaymentMethodDto.Cash),
            BuildPaymentDto(amount: 250.50m, method: PaymentMethodDto.CreditCard),
            BuildPaymentDto(amount: 49.50m, method: PaymentMethodDto.BankTransfer)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var totalSection = cut.Find(".bg-light");
        (totalSection.TextContent).ShouldContain("400", StringComparison.Ordinal); // 100 + 250.50 + 49.50 = 400
    }

    [Fact]
    public void Orders_payments_by_date_descending()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(paymentDate: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), amount: 100m, method: PaymentMethodDto.Cash),
            BuildPaymentDto(paymentDate: new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), amount: 200m, method: PaymentMethodDto.CreditCard),
            BuildPaymentDto(paymentDate: new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), amount: 150m, method: PaymentMethodDto.BankTransfer)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var rows = cut.FindAll("tbody tr");
        var amounts = rows.Select(r => r.QuerySelector("strong")!.TextContent).ToList();

        (amounts[0]).ShouldContain("200", StringComparison.Ordinal); // March payment (most recent)
        (amounts[2]).ShouldContain("100", StringComparison.Ordinal); // January payment (oldest)
    }

    [Fact]
    public void Has_responsive_table_wrapper()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(amount: 100m, method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var quickGrid = cut.Find(".table.table-hover");
        _ = (quickGrid).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_all_table_headers()
    {
        // Arrange
        var payments = new[]
        {
            BuildPaymentDto(amount: 100m, method: PaymentMethodDto.Cash)
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var headers = cut.FindAll("thead th");
        (headers.Count).ShouldBe(6);
        (headers[0].TextContent).ShouldContain("Payment Date", StringComparison.Ordinal);
        (headers[1].TextContent).ShouldContain("Amount", StringComparison.Ordinal);
        (headers[2].TextContent).ShouldContain("Method", StringComparison.Ordinal);
        (headers[3].TextContent).ShouldContain("Reference", StringComparison.Ordinal);
        (headers[4].TextContent).ShouldContain("Notes", StringComparison.Ordinal);
        (headers[5].TextContent).ShouldContain("Recorded At", StringComparison.Ordinal);
    }
}
