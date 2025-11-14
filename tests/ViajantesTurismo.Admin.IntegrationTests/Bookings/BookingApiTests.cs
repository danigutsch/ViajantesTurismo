using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class BookingApiTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal DoubleRoomSupplement = 500m;
    private const decimal RegularBikePrice = 100m;
    private const decimal ValidPercentageDiscount = 10m;
    private const decimal ValidAbsoluteDiscount = 150m;
    private const decimal OverAllowedPercentageDiscount = 150m;
    private const decimal AbsoluteDiscountExceedingSubtotal = 3000m;
    private const decimal FirstPaymentAmount = 1000m;
    private const decimal PaymentAmountExceedingRemainingBalance = 3000m;

    [Fact]
    public async Task Can_Get_Booking_By_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("David", "Wilson");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking =
            await response.Content.ReadFromJsonAsync<GetBookingDto>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(createdBooking.Id, booking.Id);
        var expectedPrice = CalculateExpectedPrice(
            basePrice: 2000m,
            roomSupplement: 0m,
            principalBikePrice: 100m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task Get_Booking_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        // Act
        var response = await Client.GetAsync(new Uri("/bookings/99999", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Get_Bookings_By_Tour_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customer1 = await CreateTestCustomer("Emma", "Davis");
        var customer2 = await CreateTestCustomer("Frank", "Miller");

        await CreateTestBooking(tourDto.Id, customer1.Id);
        await CreateTestBooking(tourDto.Id, customer2.Id);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{tourDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(tourDto.Id, b.TourId));
    }

    [Fact]
    public async Task Can_Get_Bookings_By_Customer_Id()
    {
        // Arrange
        var tour1 = await CreateTestTour();
        var tour2 = await CreateTestTour("CUBA2025", "Cuba Adventure 2025");
        var customerDto = await CreateTestCustomer("Grace", "Lee");

        await CreateTestBooking(tour1.Id, customerDto.Id);
        await CreateTestBooking(tour2.Id, customerDto.Id);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/customer/{customerDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(customerDto.Id, b.CustomerId));
    }

    [Fact]
    public async Task Get_Bookings_By_Customer_Id_Includes_Bookings_As_Companion()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var primaryCustomer = await CreateTestCustomer("Henry", "Taylor");
        var companionCustomer = await CreateTestCustomer("Iris", "Anderson");

        await CreateTestBooking(tourDto.Id, primaryCustomer.Id, companionCustomer.Id);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/customer/{companionCustomer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Single(bookings);
        Assert.Equal(companionCustomer.Id, bookings[0].CompanionId);
    }

    [Fact]
    public async Task Can_Update_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Jack", "Martin");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id);

        // Act
        var notesRequest = new UpdateBookingNotesDto { Notes = "Updated notes" };
        var notesResponse = await Client.PatchAsJsonAsync(
            new Uri($"/bookings/{createdBooking.Id}/notes", UriKind.Relative),
            notesRequest,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        var updatedBooking = await notesResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Updated notes", updatedBooking.Notes);

        var confirmResponse = await Client.PostAsync(
            new Uri($"/bookings/{createdBooking.Id}/confirm", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmedBooking = await confirmResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(confirmedBooking);
        Assert.Equal("Updated notes", confirmedBooking.Notes);
        Assert.Equal(BookingStatusDto.Confirmed, confirmedBooking.Status);
    }

    [Fact]
    public async Task Update_Booking_Notes_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var updateRequest = new UpdateBookingNotesDto
        {
            Notes = "Test"
        };

        // Act
        var response = await Client.PatchAsJsonAsync(new Uri("/bookings/99999/notes", UriKind.Relative), updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Delete_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Kate", "White");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id);

        // Act
        var response = await Client.DeleteAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        // Act
        var response = await Client.DeleteAsync(new Uri("/bookings/99999", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Cancel_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Laura", "Brown");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id);

        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await Client.PostAsync(new Uri($"/bookings/{createdBooking.Id}/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelledBooking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatusDto.Cancelled, cancelledBooking.Status);
        Assert.Equal(createdBooking.TotalPrice, cancelledBooking.TotalPrice);
        Assert.Equal(createdBooking.PaymentStatus, cancelledBooking.PaymentStatus);
    }

    [Fact]
    public async Task Cancel_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        // Act
        var response = await Client.PostAsync(new Uri("/bookings/99999/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Booking_Discount_Percentage()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Perc", "Entage");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = ValidPercentageDiscount,
            DiscountReason = "Seasonal offer"
        };

        // Act
        var updateResponse = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Percentage, updated.DiscountType);
        Assert.Equal(10m, updated.DiscountAmount);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, ValidPercentageDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Discount_Absolute()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Abs", "Olute");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = ValidAbsoluteDiscount,
            DiscountReason = "VIP customer"
        };

        // Act
        var updateResponse = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Absolute, updated.DiscountType);
        Assert.Equal(150m, updated.DiscountAmount);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, null, ValidAbsoluteDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Discount_Invalid_Percentage_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Inval", "Percent");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = OverAllowedPercentageDiscount,
            DiscountReason = "Too big"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Discount_Absolute_ExceedsSubtotal_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Exceed", "Subtotal");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = AbsoluteDiscountExceedingSubtotal,
            DiscountReason = "Impossible"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Booking_Details_Add_Companion_And_RoomSupplement()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Prim", "Ary");
        var companion = await CreateTestCustomer("Comp", "Anion");
        var booking = await CreateTestBooking(tour.Id, principal.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.DoubleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companion.Id,
            CompanionBikeType = BikeTypeDto.Regular
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(companion.Id, updated.CompanionId);
        var expected = CalculateExpectedPrice(BaseTourPrice, DoubleRoomSupplement, RegularBikePrice, RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Details_Remove_Companion_And_Switch_To_Single()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Switch", "Down");
        var companion = await CreateTestCustomer("To", "Single");
        var booking = await CreateTestBooking(tour.Id, principal.Id, companion.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Null(updated.CompanionId);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Details_SingleRoomWithCompanion_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Bad", "Combo");
        var companion = await CreateTestCustomer("Wrong", "Room");
        var booking = await CreateTestBooking(tour.Id, principal.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companion.Id,
            CompanionBikeType = BikeTypeDto.Regular
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Details_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Record_Payment_And_Update_Status_To_PartiallyPaid()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Pay", "Part");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.BankTransfer,
            ReferenceNumber = "REF-500",
            Notes = "Down payment"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
        Assert.Single(updated.Payments);
        Assert.Equal(500m, updated.Payments.First().Amount);
    }

    [Fact]
    public async Task Can_Record_Multiple_Payments_And_Update_Status_To_Paid()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Pay", "Full");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var expectedTotal = booking.TotalPrice;
        var secondPaymentAmount = expectedTotal - FirstPaymentAmount;

        var payment1 = new CreatePaymentDto
        {
            Amount = FirstPaymentAmount,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.CreditCard,
            ReferenceNumber = "REF-1"
        };
        var response1 = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), payment1, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var payment2 = new CreatePaymentDto
        {
            Amount = secondPaymentAmount,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.CreditCard,
            ReferenceNumber = "REF-2"
        };
        var response2 = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), payment2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);

        Assert.Equal(PaymentStatusDto.Paid, updated.PaymentStatus);
        Assert.Equal(2, updated.Payments.Count);
        Assert.Equal(expectedTotal, updated.Payments.Sum(p => p.Amount));
    }

    [Fact]
    public async Task Record_Payment_Exceeds_RemainingBalance_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Bad", "Pay");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = PaymentAmountExceedingRemainingBalance,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_Returns_NotFound_For_Invalid_Booking_Id()
    {
        // Arrange
        var paymentDto = new CreatePaymentDto
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash,
            ReferenceNumber = "REF-123",
            Notes = "Test payment"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Complete_Booking_After_Confirm()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Finish", "Me");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Act
        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(completed);
        Assert.Equal(BookingStatusDto.Completed, completed.Status);
    }

    [Fact]
    public async Task Complete_Booking_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        // Act
        var response = await Client.PostAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Discount_On_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Lock", "Discount");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 50m,
            DiscountReason = "Late attempt"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Bookings_By_Tour_Id_Returns_Empty_For_Invalid_Tour()
    {
        // Arrange
        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task Get_Bookings_By_Customer_Id_Returns_Empty_For_Invalid_Customer()
    {
        // Arrange
        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/customer/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }


    [Fact]
    public async Task Record_Payment_With_Negative_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Neg", "Payment");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = -100m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash,
            Notes = "Invalid negative"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_With_Zero_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Zero", "Payment");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = 0m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Booking_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        // Act
        var response = await Client.PostAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Cancelled_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Cancel", "Then Confirm");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var cancelResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // Act
        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, confirmResponse.StatusCode);
    }

    [Fact]
    public async Task Cancel_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Complete", "Then Cancel");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Act
        var cancelResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task Complete_Pending_Booking_Without_Confirm_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Skip", "Confirm");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        // Act
        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);
    }


    [Fact]
    public async Task Record_Payment_All_Methods_Updates_Status()
    {
        // Arrange
        var paymentMethods = new[]
        {
            PaymentMethodDto.CreditCard,
            PaymentMethodDto.BankTransfer,
            PaymentMethodDto.Cash,
            PaymentMethodDto.Check,
            PaymentMethodDto.PayPal,
            PaymentMethodDto.Other
        };

        foreach (var method in paymentMethods)
        {
            // Arrange
            var tour = await CreateTestTour($"CUBA2024-{method}", $"Cuba Adventure 2024 - {method}");
            var customer = await CreateTestCustomer($"Pay{method}", "Test");
            var booking = await CreateTestBooking(tour.Id, customer.Id);

            var paymentDto = new CreatePaymentDto
            {
                Amount = 500m,
                PaymentDate = DateTime.UtcNow.Date,
                Method = method,
                ReferenceNumber = $"REF-{method}",
                Notes = $"Payment via {method}"
            };

            // Act
            var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
            var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
            Assert.Single(updated.Payments);
            Assert.Equal(method, updated.Payments.First().Method);
        }
    }

    private async Task<GetTourDto> CreateTestTour(string identifier = "CUBA2024", string name = "Cuba Adventure 2024")
    {
        var tourRequest = new CreateTourDto
        {
            Identifier = identifier,
            Name = name,
            StartDate = DateTime.UtcNow.AddMonths(2),
            EndDate = DateTime.UtcNow.AddMonths(2).AddDays(10),
            Price = BaseTourPrice,
            DoubleRoomSupplementPrice = DoubleRoomSupplement,
            RegularBikePrice = RegularBikePrice,
            EBikePrice = 200.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel", "Breakfast", "Bike"]
        };

        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), tourRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await Client.GetAsync(location, TestContext.Current.CancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(TestContext.Current.CancellationToken))!;
    }

    private static decimal CalculateExpectedPrice(
        decimal basePrice,
        decimal roomSupplement,
        decimal principalBikePrice,
        decimal? companionBikePrice = null,
        decimal? discountPercentage = null,
        decimal? absoluteDiscount = null)
    {
        var totalPrice = basePrice + roomSupplement + principalBikePrice;

        if (companionBikePrice.HasValue)
        {
            totalPrice += companionBikePrice.Value;
        }

        if (discountPercentage.HasValue)
        {
            totalPrice -= totalPrice * (discountPercentage.Value / 100m);
        }

        if (absoluteDiscount.HasValue)
        {
            totalPrice -= absoluteDiscount.Value;
        }

        return totalPrice;
    }

    private async Task<GetCustomerDto> CreateTestCustomer(string firstName, string lastName)
    {
        var customerRequest = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = firstName,
                LastName = lastName,
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = $"{firstName}{lastName}{Random.Shared.Next(1000, 9999)}",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                Mobile = $"+1555{Random.Shared.Next(1000000, 9999999)}",
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "12345",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 75.0m,
                HeightCentimeters = 180,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };

        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), customerRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(TestContext.Current.CancellationToken))!;
    }

    private async Task<GetBookingDto> CreateTestBooking(Guid tourId, Guid customerId, Guid? companionId = null)
    {
        var bookingRequest = new CreateBookingDto
        {
            TourId = tourId,
            PrincipalCustomerId = customerId,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companionId,
            CompanionBikeType = companionId.HasValue ? BikeTypeDto.Regular : null,
            RoomType = companionId.HasValue ? RoomTypeDto.DoubleRoom : RoomTypeDto.SingleRoom,
            Notes = "Test booking"
        };

        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken))!;
    }
}
