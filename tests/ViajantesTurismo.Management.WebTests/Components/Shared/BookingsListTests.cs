using Microsoft.AspNetCore.Components.QuickGrid;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class BookingsListTests : BunitContext
{
    public BookingsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_info_message_when_bookings_is_null()
    {
        // Arrange
        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, null));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No bookings found", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_info_message_when_bookings_is_empty()
    {
        // Arrange
        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, Array.Empty<GetBookingDto>()));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No bookings found", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_QuickGrid_when_bookings_exist()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var grid = cut.Find("table.table.table-hover");
        Assert.NotNull(grid);
    }

    [Fact]
    public void Shows_tour_info_column_when_showTourInfo_is_true()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                tourIdentifier: "TOUR-001",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        Assert.Contains("Tour 1", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("TOUR-001", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_tour_info_column_when_showTourInfo_is_false()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                tourIdentifier: "TOUR-001",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        Assert.DoesNotContain("Tour 1", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_customer_info_column_when_showCustomerInfo_is_true()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "John Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        Assert.Contains("John Doe", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_customer_info_column_when_showCustomerInfo_is_false()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "John Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowCustomerInfo, false));

        // Assert
        var headers = cut.FindAll("th");
        Assert.DoesNotContain(headers, h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        Assert.DoesNotContain("John Doe", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_companion_link_when_companion_exists()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                companionId: companionId,
                companionName: "Jane Doe",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var companionLink = cut.Find($"a[href='/customers/{companionId}']");
        Assert.Equal("Jane Doe", companionLink.TextContent);
    }

    [Fact]
    public void Displays_dash_when_no_companion()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        Assert.Contains("<span class=\"text-muted\">-</span>", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_percentage_discount_badge()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Percentage,
                discountAmount: 15.5m,
                discountReason: "Early Bird",
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.Contains("15.50%", badge.TextContent, StringComparison.Ordinal);
        Assert.Equal("Percentage Discount: Early Bird", badge.GetAttribute("title"));
    }

    [Fact]
    public void Displays_absolute_discount_badge()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Absolute,
                discountAmount: 100.00m,
                discountReason: "Group Discount",
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid,
                currency: CurrencyDto.Real)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-warning.text-dark");
        Assert.Contains("100", badge.TextContent, StringComparison.Ordinal); // Currency formatting will include the amount
        Assert.Equal("Absolute Discount: Group Discount", badge.GetAttribute("title"));
    }

    [Theory]
    [InlineData(CurrencyDto.Real, "R$")]
    [InlineData(CurrencyDto.Euro, "€")]
    [InlineData(CurrencyDto.UsDollar, "$")]
    public void Displays_discount_with_correct_currency_symbol(CurrencyDto currency, string expectedSymbol)
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.Absolute,
                discountAmount: 150.00m,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid,
                currency: currency)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var badge = cut.Find(".badge.bg-warning.text-dark");
        Assert.Contains(expectedSymbol, badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_dash_when_no_discount()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var discountCell = cut.FindAll("td")[5]; // Discount column
        Assert.Contains("-", discountCell.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_bookingStatusBadge()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Confirmed,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var statusBadge = cut.FindComponent<BookingStatusBadge>();
        Assert.NotNull(statusBadge);
    }

    [Fact]
    public void Displays_paymentStatusBadge()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.PartiallyPaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paymentBadge = cut.FindComponent<PaymentStatusBadge>();
        Assert.NotNull(paymentBadge);
    }

    [Fact]
    public void Shows_view_and_edit_links_for_all_bookings()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var bookings = new[]
        {
            BuildBookingDto(
                id: bookingId,
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var viewLink = cut.Find($"a[href='/bookings/{bookingId}']");
        Assert.Contains("View", viewLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-eye", viewLink.InnerHtml, StringComparison.Ordinal);

        var editLink = cut.Find($"a[href='/bookings/{bookingId}/edit']");
        Assert.Contains("Edit", editLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-pencil", editLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_show_paginator_for_10_or_fewer_bookings()
    {
        // Arrange
        var bookings = Enumerable.Range(1, 10)
            .Select(i =>
            {
                var customerName = $"Customer {i}";
                return BuildBookingDto(
                    id: Guid.NewGuid(),
                    tourName: "Tour 1",
                    customerName: customerName,
                    discountType: DiscountTypeDto.None,
                    discountAmount: 0,
                    status: BookingStatusDto.Pending,
                    paymentStatus: PaymentStatusDto.Unpaid);
            })
            .ToArray();

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paginators = cut.FindComponents<Paginator>();
        Assert.Empty(paginators);
    }

    [Fact]
    public void Shows_paginator_for_more_than_10_bookings()
    {
        // Arrange
        var bookings = Enumerable.Range(1, 15)
            .Select(i =>
            {
                var customerName = $"Customer {i}";
                return BuildBookingDto(
                    id: Guid.NewGuid(),
                    tourName: "Tour 1",
                    customerName: customerName,
                    discountType: DiscountTypeDto.None,
                    discountAmount: 0,
                    status: BookingStatusDto.Pending,
                    paymentStatus: PaymentStatusDto.Unpaid);
            })
            .ToArray();

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings));

        // Assert
        var paginators = cut.FindComponents<Paginator>();
        Assert.NotEmpty(paginators);
    }

    [Fact]
    public void Displays_all_column_headers()
    {
        // Arrange
        var bookings = new[]
        {
            BuildBookingDto(
                id: Guid.NewGuid(),
                tourName: "Tour 1",
                customerName: "Customer 1",
                discountType: DiscountTypeDto.None,
                discountAmount: 0,
                status: BookingStatusDto.Pending,
                paymentStatus: PaymentStatusDto.Unpaid)
        };

        // Act
        var cut = Render<BookingsList>(parameters => parameters
            .Add(p => p.Bookings, bookings)
            .Add(p => p.ShowTourInfo, true)
            .Add(p => p.ShowCustomerInfo, true));

        // Assert
        var headers = cut.FindAll("th").Select(h => h.TextContent).ToList();
        Assert.Contains(headers, h => h.Contains("Booking Date", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Tour", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Customer", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Companion", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Total Price", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Discount", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Status", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Payment", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.Contains("Actions", StringComparison.Ordinal));
    }
}
