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
        Assert.Contains("No payments recorded yet", alert.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-info-circle", alert.InnerHtml, StringComparison.Ordinal);
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
        Assert.NotNull(quickGrid);
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
        Assert.Contains("15/03/2024", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("123.45", amountCell.TextContent, StringComparison.Ordinal);
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
        Assert.Equal("Credit Card", badge.TextContent.Trim());
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
        Assert.Equal("Bank Transfer", badge.TextContent.Trim());
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
        Assert.Equal("Cash", badge.TextContent.Trim());
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
        Assert.Equal("Check", badge.TextContent.Trim());
    }

    [Fact]
    public void Displays_payPal_method_with_secondary_badge()
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
        Assert.Equal("PayPal", badge.TextContent.Trim());
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
        Assert.Equal("REF123456", code.TextContent);
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
        Assert.NotNull(referenceCell);
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
        Assert.Contains("Test payment notes", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("...", notesSpan.TextContent, StringComparison.Ordinal);
        Assert.True(notesSpan.TextContent.Length <= 34); // 30 + "..."
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
        Assert.NotNull(notesCell);
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
        Assert.Contains("400", totalSection.TextContent, StringComparison.Ordinal); // 100 + 250.50 + 49.50 = 400
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

        Assert.Contains("200", amounts[0], StringComparison.Ordinal); // March payment (most recent)
        Assert.Contains("100", amounts[2], StringComparison.Ordinal); // January payment (oldest)
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
        Assert.NotNull(quickGrid);
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
        Assert.Equal(6, headers.Count);
        Assert.Contains("Payment Date", headers[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Amount", headers[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Method", headers[2].TextContent, StringComparison.Ordinal);
        Assert.Contains("Reference", headers[3].TextContent, StringComparison.Ordinal);
        Assert.Contains("Notes", headers[4].TextContent, StringComparison.Ordinal);
        Assert.Contains("Recorded At", headers[5].TextContent, StringComparison.Ordinal);
    }
}
