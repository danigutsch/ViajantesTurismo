using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class PaymentsListTests : BunitContext
{
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
        Assert.Contains("No payments recorded yet", alert.TextContent);
        Assert.Contains("bi-info-circle", alert.InnerHtml);
    }

    [Fact]
    public void Renders_Table_When_Payments_Exist()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = new DateTime(2024, 1, 15),
                Amount = 100.50m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var table = cut.Find("table.table");
        Assert.NotNull(table);
    }

    [Fact]
    public void Displays_Payment_Date()
    {
        // Arrange
        var paymentDate = new DateTime(2024, 3, 15);
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = paymentDate,
                Amount = 50.00m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        Assert.Contains(paymentDate.ToShortDateString(), cut.Markup);
    }

    [Fact]
    public void Displays_Amount_As_Currency()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 123.45m,
                Method = PaymentMethodDto.CreditCard,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var amountCell = cut.Find("td strong");
        Assert.Contains("123.45", amountCell.TextContent);
    }

    [Fact]
    public void Displays_Credit_Card_Method_With_Primary_Badge()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.CreditCard,
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.BankTransfer,
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Check,
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.PayPal,
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.BankTransfer,
                ReferenceNumber = "REF123456",
                RecordedAt = DateTime.UtcNow
            }
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
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var rows = cut.FindAll("tr");
        var dataRow = rows[1]; // First data row (index 0 is header)
        var cells = dataRow.QuerySelectorAll("td");
        Assert.Contains("-", cells[3].TextContent);
    }

    [Fact]
    public void Displays_Notes_When_Present()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                Notes = "Test payment notes",
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        Assert.Contains("Test payment notes", cut.Markup);
    }

    [Fact]
    public void Truncates_Long_Notes()
    {
        // Arrange
        var longNotes = "This is a very long note that should be truncated to 30 characters";
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                Notes = longNotes,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var spans = cut.FindAll("span[title]");
        var notesSpan = spans.First(s => s.GetAttribute("title") == longNotes);
        Assert.Contains("...", notesSpan.TextContent);
        Assert.True(notesSpan.TextContent.Length <= 34); // 30 + "..."
    }

    [Fact]
    public void Shows_Dash_When_Notes_Missing()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var rows = cut.FindAll("tr");
        var dataRow = rows[1];
        var cells = dataRow.QuerySelectorAll("td");
        Assert.Contains("-", cells[4].TextContent);
    }

    [Fact]
    public void Calculates_Total_Paid_Correctly()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            },
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 250.50m,
                Method = PaymentMethodDto.CreditCard,
                RecordedAt = DateTime.UtcNow
            },
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 49.50m,
                Method = PaymentMethodDto.BankTransfer,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var tfoot = cut.Find("tfoot");
        Assert.Contains("400", tfoot.TextContent); // 100 + 250.50 + 49.50 = 400
    }

    [Fact]
    public void Orders_Payments_By_Date_Descending()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = new DateTime(2024, 1, 1),
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            },
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = new DateTime(2024, 3, 1),
                Amount = 200m,
                Method = PaymentMethodDto.CreditCard,
                RecordedAt = DateTime.UtcNow
            },
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = new DateTime(2024, 2, 1),
                Amount = 150m,
                Method = PaymentMethodDto.BankTransfer,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var rows = cut.FindAll("tbody tr");
        var firstRowAmount = rows[0].QuerySelector("td strong")!.TextContent;
        var lastRowAmount = rows[2].QuerySelector("td strong")!.TextContent;

        Assert.Contains("200", firstRowAmount); // March payment (most recent)
        Assert.Contains("100", lastRowAmount); // January payment (oldest)
    }

    [Fact]
    public void Has_Responsive_Table_Wrapper()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var wrapper = cut.Find(".table-responsive");
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Renders_All_Table_Headers()
    {
        // Arrange
        var payments = new[]
        {
            new GetPaymentDto
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                PaymentDate = DateTime.Today,
                Amount = 100m,
                Method = PaymentMethodDto.Cash,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = Render<PaymentsList>(parameters => parameters
            .Add(p => p.Payments, payments));

        // Assert
        var headers = cut.FindAll("thead th");
        Assert.Equal(6, headers.Count);
        Assert.Equal("Payment Date", headers[0].TextContent);
        Assert.Equal("Amount", headers[1].TextContent);
        Assert.Equal("Method", headers[2].TextContent);
        Assert.Equal("Reference", headers[3].TextContent);
        Assert.Equal("Notes", headers[4].TextContent);
        Assert.Equal("Recorded At", headers[5].TextContent);
    }
}
