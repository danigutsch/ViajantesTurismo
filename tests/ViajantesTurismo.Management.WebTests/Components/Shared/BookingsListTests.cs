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
        (alert.TextContent).ShouldContain("No bookings found", StringComparison.Ordinal);
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
        (alert.TextContent).ShouldContain("No bookings found", StringComparison.Ordinal);
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
        _ = (grid).ShouldNotBeNull();
    }

    [Fact]
    public void Shows_tour_info_column_when_showtourinfo_is_true()
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
        (headers).ShouldContain(h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        (cut.Markup).ShouldContain("Tour 1", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("TOUR-001", StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_tour_info_column_when_showtourinfo_is_false()
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
        (headers).ShouldNotContain(h => h.TextContent.Contains("Tour", StringComparison.Ordinal));
        (cut.Markup).ShouldNotContain("Tour 1", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_customer_info_column_when_showcustomerinfo_is_true()
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
        (headers).ShouldContain(h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        (cut.Markup).ShouldContain("John Doe", StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_customer_info_column_when_showcustomerinfo_is_false()
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
        (headers).ShouldNotContain(h => h.TextContent.Contains("Customer", StringComparison.Ordinal));
        (cut.Markup).ShouldNotContain("John Doe", StringComparison.Ordinal);
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
        (companionLink.TextContent).ShouldBe("Jane Doe");
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
        (cut.Markup).ShouldContain("<span class=\"text-muted\">-</span>", StringComparison.Ordinal);
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
        (badge.TextContent).ShouldContain("15.50%", StringComparison.Ordinal);
        (badge.GetAttribute("title")).ShouldBe("Percentage Discount: Early Bird");
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
        (badge.TextContent).ShouldContain("100", StringComparison.Ordinal); // Currency formatting will include the amount
        (badge.GetAttribute("title")).ShouldBe("Absolute Discount: Group Discount");
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
        (badge.TextContent).ShouldContain(expectedSymbol, StringComparison.Ordinal);
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
        (discountCell.TextContent).ShouldContain("-", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_bookingstatusbadge()
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
        _ = (statusBadge).ShouldNotBeNull();
    }

    [Fact]
    public void Displays_paymentstatusbadge()
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
        _ = (paymentBadge).ShouldNotBeNull();
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
        (viewLink.TextContent).ShouldContain("View", StringComparison.Ordinal);
        (viewLink.InnerHtml).ShouldContain("bi-eye", StringComparison.Ordinal);

        var editLink = cut.Find($"a[href='/bookings/{bookingId}/edit']");
        (editLink.TextContent).ShouldContain("Edit", StringComparison.Ordinal);
        (editLink.InnerHtml).ShouldContain("bi-pencil", StringComparison.Ordinal);
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
        (paginators).ShouldBeEmpty();
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
        (paginators).ShouldNotBeEmpty();
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
        (headers).ShouldContain(h => h.Contains("Booking Date", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Tour", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Customer", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Companion", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Total Price", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Discount", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Status", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Payment", StringComparison.Ordinal));
        (headers).ShouldContain(h => h.Contains("Actions", StringComparison.Ordinal));
    }
}
