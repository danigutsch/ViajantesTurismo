using ViajantesTurismo.Admin.Application.Bookings;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.BuildingBlocks;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Read-side query service backed by the Admin read database context.</summary>
/// <param name="dbContext">The Admin read database context.</param>
/// <param name="histories">Payment history store used to attach booking financial state.</param>
internal sealed class QueryService(AdminReadDbContext dbContext, PaymentHistoryStore histories) : IQueryService
{
    /// <inheritdoc />
    public async Task<GetTourPageDto> GetTourPage(string? page, TourPageSort sort, bool descending, CancellationToken ct)
    {
        var tours = dbContext.Tours.Where(tour => tour.RetirementId == null);
        var total = await tours.CountAsync(ct);
        var window = PageWindow.Create(page, ContractConstants.TourListPageSize, total);
        var summaries = tours.Select(tour => new
        {
            tour.Id,
            tour.Identifier,
            tour.Name,
            tour.Schedule.StartDate,
            tour.Schedule.EndDate,
            tour.Pricing.BasePrice,
            tour.Pricing.SingleRoomSupplementPrice,
            tour.Pricing.RegularBikePrice,
            tour.Pricing.EBikePrice,
            tour.Pricing.Currency,
            tour.Capacity,
            // Match Tour.CurrentCustomerCount when counting confirmed travelers in SQL.
            CurrentCustomerCount = tour.Bookings.Where(booking => booking.Status == BookingStatus.Confirmed)
                .Sum(booking => booking.CompanionCustomer == null ? 1 : 2)
        });
        var ordered = sort switch
        {
            TourPageSort.Identifier => OrderBy(summaries, tour => tour.Identifier, descending),
            TourPageSort.Name => OrderBy(summaries, tour => tour.Name, descending),
            TourPageSort.StartDate => OrderBy(summaries, tour => tour.StartDate, descending),
            TourPageSort.EndDate => OrderBy(summaries, tour => tour.EndDate, descending),
            TourPageSort.Price => OrderBy(summaries, tour => tour.BasePrice, descending),
            TourPageSort.CurrentCustomerCount => OrderBy(summaries, tour => tour.CurrentCustomerCount, descending),
            _ => summaries.OrderByDescending(tour => tour.Id)
        };
        if (sort != TourPageSort.Default)
        {
            ordered = ordered.ThenByDescending(tour => tour.Id);
        }

        var rows = await ordered.Skip(window.Skip).Take(window.Take).ToArrayAsync(ct);
        var items = rows.Select(row => new GetTourListItemDto(row.Id, row.Identifier, row.Name, row.StartDate, row.EndDate,
            row.BasePrice, row.SingleRoomSupplementPrice, row.RegularBikePrice, row.EBikePrice, row.Currency.Code,
            row.Capacity.MinCustomers, row.Capacity.MaxCustomers, row.CurrentCustomerCount,
            row.Capacity.AvailableSpotsAt(row.CurrentCustomerCount), row.Capacity.IsFullAt(row.CurrentCustomerCount),
            row.Capacity.HasReachedMinimum(row.CurrentCustomerCount))).ToArray();
        return new GetTourPageDto(items, Math.Max(total, items.Length));
    }

    /// <inheritdoc />
    public async Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tours = dbContext.Tours.Where(tour => tour.RetirementId == null);
        var key = SearchKey.Of(request.Query);
        if (key.Length > 0)
        {
            var pattern = SqlLikePattern.ContainsLiteral(key);
            tours = tours.Where(tour =>
                EF.Functions.ILike(EF.Functions.Unaccent(tour.Name), EF.Functions.Unaccent(pattern), SqlLikePattern.EscapeCharacter) ||
                EF.Functions.ILike(EF.Functions.Unaccent(tour.Identifier), EF.Functions.Unaccent(pattern), SqlLikePattern.EscapeCharacter));
        }

        var total = await tours.CountAsync(ct);
        var options = await tours
            .OrderByDescending(tour => tour.Id)
            .Take(Math.Clamp(request.Limit, 1, ContractConstants.MaxTourOptions))
            .Select(tour => new GetTourOptionDto(tour.Id, tour.Name, tour.Identifier, tour.Schedule.StartDate))
            .ToArrayAsync(ct);
        return new GetTourOptionsDto(options, Math.Max(total, options.Length));
    }

    /// <inheritdoc />
    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) =>
        ReadTourDetail(dbContext.Tours.Where(tour => tour.Id == id), ct);

    /// <inheritdoc />
    public Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct) =>
        ReadTourDetail(dbContext.Tours.Where(tour => tour.Id == id && tour.RetirementId == null), ct);

    private static async Task<GetTourDto?> ReadTourDetail(IQueryable<Tour> tours, CancellationToken ct)
    {
        var row = await tours.Select(tour => new
        {
            Tour = tour,
            // Match Tour.CurrentCustomerCount without materializing the booking history.
            CurrentCustomerCount = tour.Bookings.Where(booking => booking.Status == BookingStatus.Confirmed)
                .Sum(booking => booking.CompanionCustomer == null ? 1 : 2)
        }).SingleOrDefaultAsync(ct);
        return row is null ? null : MapTour(row.Tour, row.CurrentCustomerCount);
    }

    /// <inheritdoc />
    public async Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct)
    {
        return await dbContext.Tours
            .Where(tour => tour.Id == id && tour.RetiredAt.HasValue)
            .Select(tour => new GetTourRetirementDto
            {
                TourId = tour.Id,
                RetiredAt = tour.RetiredAt.GetValueOrDefault(),
                CompletedAt = tour.CatalogRetirementAppliedAt
            })
            .SingleOrDefaultAsync(ct);
    }

    private static GetTourDto MapTour(Tour tour, int currentCustomerCount) => new()
    {
        Id = tour.Id,
        Identifier = tour.Identifier,
        Name = tour.Name,
        StartDate = tour.Schedule.StartDate,
        EndDate = tour.Schedule.EndDate,
        Price = tour.Pricing.BasePrice,
        SingleRoomSupplementPrice = tour.Pricing.SingleRoomSupplementPrice,
        RegularBikePrice = tour.Pricing.RegularBikePrice,
        EBikePrice = tour.Pricing.EBikePrice,
        Currency = tour.Pricing.Currency.Code,
        IncludedServices = [.. tour.IncludedServices],
        MinCustomers = tour.Capacity.MinCustomers,
        MaxCustomers = tour.Capacity.MaxCustomers,
        CurrentCustomerCount = currentCustomerCount,
        AvailableSpots = tour.Capacity.AvailableSpotsAt(currentCustomerCount),
        IsFullyBooked = tour.Capacity.IsFullAt(currentCustomerCount),
        IsAtMinimumCapacity = tour.Capacity.HasReachedMinimum(currentCustomerCount)
    };

    /// <inheritdoc />
    public async Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct)
    {
        var customers = dbContext.Customers;
        var total = await customers.CountAsync(ct);
        var window = PageWindow.Create(page, ContractConstants.CustomerListPageSize, total);
        var ordered = sort switch
        {
            CustomerPageSort.Email => OrderBy(customers, customer => customer.ContactInfo.Email, descending),
            CustomerPageSort.Nationality => OrderBy(customers, customer => customer.PersonalInfo.Nationality, descending),
            _ => descending
                ? customers.OrderByDescending(customer => customer.PersonalInfo.FirstName)
                    .ThenByDescending(customer => customer.PersonalInfo.LastName)
                : customers.OrderBy(customer => customer.PersonalInfo.FirstName)
                    .ThenBy(customer => customer.PersonalInfo.LastName)
        };

        var rows = await ordered.ThenBy(customer => customer.Id)
            .Skip(window.Skip)
            .Take(window.Take)
            .Select(customer => new
            {
                customer.Id,
                customer.PersonalInfo.FirstName,
                customer.PersonalInfo.LastName,
                customer.ContactInfo.Email,
                customer.ContactInfo.Mobile,
                customer.PersonalInfo.Nationality
            })
            .ToArrayAsync(ct);
        var items = rows.Select(row => new GetCustomerListItemDto(
            row.Id, row.FirstName, row.LastName, row.Email.Value, row.Mobile.Value, row.Nationality)).ToArray();
        return new GetCustomerPageDto(items, Math.Max(total, items.Length));
    }

    /// <inheritdoc />
    public async Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customers = dbContext.Customers.AsQueryable();
        if (request.ExcludedCustomerId is { } excludedId)
        {
            customers = customers.Where(customer => customer.Id != excludedId);
        }

        var key = SearchKey.Of(request.Query);
        if (Guid.TryParse(key, out var id))
        {
            customers = customers.Where(customer => customer.Id == id);
        }
        else if (key.Length > 0)
        {
            var pattern = SqlLikePattern.ContainsLiteral(key);
            customers = customers.Where(customer => EF.Functions.ILike(
                EF.Functions.Unaccent(customer.PersonalInfo.FirstName + " " + customer.PersonalInfo.LastName),
                EF.Functions.Unaccent(pattern), SqlLikePattern.EscapeCharacter));
        }

        var total = await customers.CountAsync(ct);
        var rows = await customers
            .OrderBy(customer => customer.PersonalInfo.FirstName)
            .ThenBy(customer => customer.PersonalInfo.LastName)
            .ThenBy(customer => customer.Id)
            .Take(Math.Clamp(request.Limit, 1, ContractConstants.MaxCustomerOptions))
            .Select(customer => new
            {
                customer.Id,
                customer.PersonalInfo.FirstName,
                customer.PersonalInfo.LastName,
                customer.ContactInfo.Email,
                customer.PhysicalInfo.BikeType
            })
            .ToArrayAsync(ct);
        var items = rows.Select(row => new GetCustomerOptionDto(
            row.Id, row.FirstName, row.LastName, row.Email.Value, CustomerMapper.MapToBikeTypeDto(row.BikeType))).ToArray();
        return new GetCustomerOptionsDto(items, Math.Max(total, items.Length));
    }

    /// <inheritdoc />
    public async Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct)
    {
        var customer = await dbContext.Customers.FindAsync([id], ct);
        if (customer is null)
        {
            return null;
        }

        return new CustomerDetailsDto
        {
            Id = customer.Id,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = customer.PersonalInfo.FirstName,
                LastName = customer.PersonalInfo.LastName,
                BirthDate = customer.PersonalInfo.BirthDate.Value,
                Gender = customer.PersonalInfo.Gender,
                Nationality = customer.PersonalInfo.Nationality,
                Occupation = customer.PersonalInfo.Occupation
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = customer.IdentificationInfo.NationalId,
                IdNationality = customer.IdentificationInfo.IdNationality
            },
            ContactInfo = new ContactInfoDto
            {
                Email = customer.ContactInfo.Email.Value,
                Mobile = customer.ContactInfo.Mobile.Value,
                Instagram = customer.ContactInfo.Instagram,
                Facebook = customer.ContactInfo.Facebook
            },
            Address = new AddressDto
            {
                Street = customer.Address.Street,
                Complement = customer.Address.Complement,
                Neighborhood = customer.Address.Neighborhood,
                PostalCode = customer.Address.PostalCode,
                City = customer.Address.City,
                State = customer.Address.State,
                Country = customer.Address.Country.Value
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = customer.PhysicalInfo.WeightKg,
                HeightCentimeters = customer.PhysicalInfo.HeightCentimeters,
                BikeType = BookingMapper.MapToBikeTypeDto(customer.PhysicalInfo.BikeType)
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = BookingMapper.MapToRoomTypeDto(customer.AccommodationPreferences.RoomType),
                BedType = BookingMapper.MapToBedTypeDto(customer.AccommodationPreferences.BedType),
                CompanionId = customer.AccommodationPreferences.CompanionId
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = customer.EmergencyContact.Name,
                Mobile = customer.EmergencyContact.Mobile.Value
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = customer.MedicalInfo.Allergies,
                AdditionalInfo = customer.MedicalInfo.AdditionalInfo
            }
        };
    }

    /// <inheritdoc />
    public async Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct)
    {
        var bookings = dbContext.Tours.SelectMany(tour => tour.Bookings);
        var counts = await bookings
            .GroupBy(booking => booking.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, ct);
        var overview = new BookingOverviewCountsDto(
            counts.Values.Sum(),
            counts.GetValueOrDefault(BookingStatus.Pending),
            counts.GetValueOrDefault(BookingStatus.Confirmed),
            counts.GetValueOrDefault(BookingStatus.Completed),
            counts.GetValueOrDefault(BookingStatus.Cancelled));
        var window = PageWindow.Create(page, ContractConstants.BookingListPageSize, overview.Total);

        var rows = await OrderBookingPage(bookings, sort, descending)
            .Skip(window.Skip)
            .Take(window.Take)
            .ToArrayAsync(ct);

        return new GetBookingPageDto(await ReadBookingRows(rows, ct), overview);
    }

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetTourBookingPage(Guid tourId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) =>
        ReadBookingHistoryPage(dbContext.Tours.Where(tour => tour.Id == tourId).SelectMany(tour => tour.Bookings),
            page, sort, descending, ct);

    /// <inheritdoc />
    public Task<GetBookingHistoryPageDto> GetCustomerBookingPage(Guid customerId, string? page, BookingPageSort sort, bool descending,
        CancellationToken ct) =>
        ReadBookingHistoryPage(BookingsForCustomer(customerId), page,
            sort == BookingPageSort.Default ? BookingPageSort.BookingDate : sort,
            sort == BookingPageSort.Default || descending, ct);

    private IQueryable<Booking> BookingsForCustomer(Guid customerId) =>
        dbContext.Tours.SelectMany(tour => tour.Bookings)
            .Where(booking => booking.PrincipalCustomer.CustomerId == customerId ||
                (booking.CompanionCustomer != null && booking.CompanionCustomer.CustomerId == customerId));

    private async Task<GetBookingHistoryPageDto> ReadBookingHistoryPage(
        IQueryable<Booking> bookings, string? page, BookingPageSort sort, bool descending, CancellationToken ct)
    {
        var total = await bookings.CountAsync(ct);
        if (total == 0)
        {
            return new GetBookingHistoryPageDto([], 0);
        }

        var window = PageWindow.Create(page, ContractConstants.BookingListPageSize, total);
        var rows = await OrderBookingPage(bookings, sort, descending)
            .Skip(window.Skip)
            .Take(window.Take)
            .ToArrayAsync(ct);
        var items = await ReadBookingRows(rows, ct);
        return new GetBookingHistoryPageDto(items, Math.Max(total, items.Count));
    }

    private IQueryable<Booking> OrderBookingPage(IQueryable<Booking> bookings, BookingPageSort sort, bool descending)
    {
        if (sort == BookingPageSort.TotalPrice)
        {
            // SQL ordering mirrors the booking price breakdown. The DTO still reads the domain amount.
            var priced = bookings.Select(booking => new
            {
                Booking = booking,
                Subtotal = booking.BasePrice + booking.RoomAdditionalCost + booking.PrincipalCustomer.BikePrice +
                           (booking.CompanionCustomer == null ? 0m : booking.CompanionCustomer.BikePrice)
            });

            return OrderBy(priced, item => item.Subtotal -
                    (item.Booking.Discount.Type == DiscountType.Percentage
                        ? item.Subtotal * (item.Booking.Discount.Amount / 100m)
                        : item.Booking.Discount.Type == DiscountType.Absolute ? item.Booking.Discount.Amount : 0m), descending)
                .ThenBy(item => item.Booking.Id)
                .Select(item => item.Booking);
        }

        var ordered = sort switch
        {
            BookingPageSort.BookingDate => OrderBy(bookings, booking => booking.BookingDate, descending),
            BookingPageSort.TourName => OrderBy(bookings, booking => dbContext.Tours
                .Where(tour => tour.Id == booking.TourId)
                .Select(tour => tour.Name)
                .FirstOrDefault(), descending),
            BookingPageSort.CustomerName => OrderBy(bookings, booking => dbContext.Customers
                .Where(customer => customer.Id == booking.PrincipalCustomer.CustomerId)
                .Select(customer => customer.PersonalInfo.FirstName + " " + customer.PersonalInfo.LastName)
                .FirstOrDefault(), descending),
            _ => bookings.OrderByDescending(booking => booking.Status == BookingStatus.Cancelled)
                .ThenByDescending(booking => booking.BookingDate),
        };
        return ordered.ThenBy(booking => booking.Id);
    }

    private static IOrderedQueryable<T> OrderBy<T, TKey>(
        IQueryable<T> query, Expression<Func<T, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private async Task<IReadOnlyList<GetBookingDto>> ReadBookingRows(IReadOnlyList<Booking> bookings, CancellationToken ct)
    {
        await histories.Attach(bookings, ct);
        var tourIds = bookings.Select(b => b.TourId).Distinct().ToArray();
        var customerIds = bookings.Select(b => b.PrincipalCustomer.CustomerId)
            .Concat(bookings.Where(b => b.CompanionCustomer != null).Select(b => b.CompanionCustomer!.CustomerId))
            .Distinct();

        var tours = await dbContext.Tours
            .Where(t => tourIds.AsEnumerable().Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var customers = await dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return
        [
            ..bookings.Select(b =>
            {
                var tour = tours[b.TourId];
                var customer = customers[b.PrincipalCustomer.CustomerId];
                var companion = b.CompanionCustomer != null ? customers[b.CompanionCustomer.CustomerId] : null;

                return new GetBookingDto
                {
                    Id = b.Id,
                    TourId = b.TourId,
                    TourIdentifier = tour.Identifier,
                    TourName = tour.Name,
                    CustomerId = b.PrincipalCustomer.CustomerId,
                    CustomerName = $"{customer.PersonalInfo.FirstName} {customer.PersonalInfo.LastName}",
                    CompanionId = b.CompanionCustomer?.CustomerId,
                    CompanionName = companion != null
                        ? $"{companion.PersonalInfo.FirstName} {companion.PersonalInfo.LastName}"
                        : null,
                    RoomType = BookingMapper.MapToRoomTypeDto(b.RoomType),
                    PrincipalBikeType = BookingMapper.MapToBikeTypeDto(b.PrincipalCustomer.BikeType),
                    CompanionBikeType = b.CompanionCustomer is not null
                        ? BookingMapper.MapToBikeTypeDto(b.CompanionCustomer.BikeType)
                        : null,
                    BookingDate = b.BookingDate,
                    Status = BookingMapper.MapToBookingStatusDto(b.Status),
                    CanModifyBookedServices = b.CanModifyBookedServices,
                    CanBeConfirmed = b.CanBeConfirmed,
                    CanBeCompleted = b.CanBeCompleted,
                    CanBeCancelled = b.CanBeCancelled,
                    CanBeRemoved = b.CanBeRemoved,
                    PaymentStatus = BookingMapper.MapToPaymentStatusDto(b.PaymentStatus),
                    TotalPrice = b.TotalPrice,
                    DiscountType = BookingMapper.MapToDiscountTypeDto(b.Discount.Type),
                    DiscountAmount = b.Discount.Amount,
                    DiscountReason = b.Discount.Reason,
                    Notes = b.Notes,

                    FinancialVersion = b.FinancialHistory?.Version,
                    ApprovedObligationEuro = b.FinancialHistory?.ApprovedSchedule?.Obligation.Amount,
                    AmountPaid = b.AmountPaid,
                    RemainingBalance = b.RemainingBalance,
                    Currency = tour.Pricing.Currency.Code
                };
            })
        ];
    }

    /// <inheritdoc />
    public async Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        var booking = await dbContext.Tours
            .SelectMany(t => t.Bookings)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (booking is null)
        {
            return null;
        }

        await histories.Attach([booking], ct);
        var tour = await dbContext.Tours.FindAsync([booking.TourId], ct)
                   ?? throw new InvalidOperationException($"Tour {booking.TourId} not found for booking {booking.Id}");
        var customer = await dbContext.Customers.FindAsync([booking.PrincipalCustomer.CustomerId], ct)
                       ?? throw new InvalidOperationException($"Customer {booking.PrincipalCustomer.CustomerId} not found for booking {booking.Id}");
        var companion = booking.CompanionCustomer is not null
            ? await dbContext.Customers.FindAsync([booking.CompanionCustomer.CustomerId], ct)
              ?? throw new InvalidOperationException($"Companion customer {booking.CompanionCustomer.CustomerId} not found for booking {booking.Id}")
            : null;

        return new GetBookingDto
        {
            Id = booking.Id,
            TourId = booking.TourId,
            TourIdentifier = tour.Identifier,
            TourName = tour.Name,
            CustomerId = booking.PrincipalCustomer.CustomerId,
            CustomerName = $"{customer.PersonalInfo.FirstName} {customer.PersonalInfo.LastName}",
            CompanionId = booking.CompanionCustomer?.CustomerId,
            CompanionName = companion is not null
                ? $"{companion.PersonalInfo.FirstName} {companion.PersonalInfo.LastName}"
                : null,
            RoomType = BookingMapper.MapToRoomTypeDto(booking.RoomType),
            PrincipalBikeType = BookingMapper.MapToBikeTypeDto(booking.PrincipalCustomer.BikeType),
            CompanionBikeType = booking.CompanionCustomer is not null
                ? BookingMapper.MapToBikeTypeDto(booking.CompanionCustomer.BikeType)
                : null,
            BookingDate = booking.BookingDate,
            Status = BookingMapper.MapToBookingStatusDto(booking.Status),
            CanModifyBookedServices = booking.CanModifyBookedServices,
            CanBeConfirmed = booking.CanBeConfirmed,
            CanBeCompleted = booking.CanBeCompleted,
            CanBeCancelled = booking.CanBeCancelled,
            CanBeRemoved = booking.CanBeRemoved,
            PaymentStatus = BookingMapper.MapToPaymentStatusDto(booking.PaymentStatus),
            TotalPrice = booking.TotalPrice,
            DiscountType = BookingMapper.MapToDiscountTypeDto(booking.Discount.Type),
            DiscountAmount = booking.Discount.Amount,
            DiscountReason = booking.Discount.Reason,
            Notes = booking.Notes,

            FinancialVersion = booking.FinancialHistory?.Version,
            ApprovedObligationEuro = booking.FinancialHistory?.ApprovedSchedule?.Obligation.Amount,
            AmountPaid = booking.AmountPaid,
            RemainingBalance = booking.RemainingBalance,
            Currency = tour.Pricing.Currency.Code
        };
    }

}
