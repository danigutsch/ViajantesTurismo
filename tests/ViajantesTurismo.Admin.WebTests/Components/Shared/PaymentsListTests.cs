namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class PaymentsListTests : BunitContext
{
    public PaymentsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Shows_Info_Message_When_No_Payments()
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
    public void Renders_Table_When_Payments_Exist()
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
    public void Displays_Payment_Date()
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
    public void Displays_Amount_As_Currency()
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
    public void Displays_Credit_Card_Method_With_Primary_Badge()
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
    public void Displays_Bank_Transfer_Method_With_Info_Badge()
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
    public void Displays_Cash_Method_With_Success_Badge()
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
    public void Displays_Check_Method_With_Warning_Badge()
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
    public void Displays_PayPal_Method_With_Secondary_Badge()
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
    public void Displays_Reference_Number_When_Present()
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
    public void Shows_Dash_When_Reference_Number_Missing()
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
    public void Displays_Notes_When_Present()
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
    public void Truncates_Long_Notes()
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
    public void Shows_Dash_When_Notes_Missing()
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
    public void Calculates_Total_Paid_Correctly()
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
    public void Orders_Payments_By_Date_Descending()
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
    public void Has_Responsive_Table_Wrapper()
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
    public void Renders_All_Table_Headers()
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
