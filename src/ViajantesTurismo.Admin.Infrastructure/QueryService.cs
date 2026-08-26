using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class QueryService(AdminReadDbContext dbContext) : IQueryService
{
    public async Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct)
    {
        var tours = await dbContext.Tours
            .Include(t => t.Bookings)
            .OrderByDescending(tour => tour.Id)
            .ToArrayAsync(ct);

        return
        [
            ..tours.Select(tour =>
            {
                return new GetTourDto()
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
                    Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency),
                    IncludedServices = [..tour.IncludedServices],
                    MinCustomers = tour.Capacity.MinCustomers,
                    MaxCustomers = tour.Capacity.MaxCustomers,
                    CurrentCustomerCount = tour.CurrentCustomerCount
                };
            })
        ];
    }

    public async Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct)
    {
        var tour = await dbContext.Tours
            .Include(t => t.Bookings)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return tour is null
            ? null
            : new GetTourDto
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
                Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency),
                IncludedServices = [.. tour.IncludedServices],
                MinCustomers = tour.Capacity.MinCustomers,
                MaxCustomers = tour.Capacity.MaxCustomers,
                CurrentCustomerCount = tour.CurrentCustomerCount
            };
    }

    public async Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct)
    {
        var customers = await dbContext.Customers.OrderBy(c => c.PersonalInfo.FirstName).ThenBy(c => c.PersonalInfo.LastName).ToArrayAsync(ct);
        return
        [
            ..customers.Select(c => new GetCustomerDto
            {
                Id = c.Id,
                FirstName = c.PersonalInfo.FirstName,
                LastName = c.PersonalInfo.LastName,
                Email = c.ContactInfo.Email,
                Mobile = c.ContactInfo.Mobile,
                Nationality = c.PersonalInfo.Nationality,
                BikeType = CustomerMapper.MapToBikeTypeDto(c.PhysicalInfo.BikeType)
            })
        ];
    }

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
                BirthDate = customer.PersonalInfo.BirthDate,
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
                Email = customer.ContactInfo.Email,
                Mobile = customer.ContactInfo.Mobile,
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
                Country = customer.Address.Country
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
                Mobile = customer.EmergencyContact.Mobile
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = customer.MedicalInfo.Allergies,
                AdditionalInfo = customer.MedicalInfo.AdditionalInfo
            }
        };
    }

    public async Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct)
    {
        var bookings = await dbContext.Tours
            .SelectMany(tour => tour.Bookings)
            .Include(booking => booking.Payments)
            .OrderByDescending(b => b.Status == BookingStatus.Cancelled)
            .ThenByDescending(b => b.BookingDate)
            .ToArrayAsync(ct);

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
                    PaymentStatus = BookingMapper.MapToPaymentStatusDto(b.PaymentStatus),
                    TotalPrice = b.TotalPrice,
                    DiscountType = BookingMapper.MapToDiscountTypeDto(b.Discount.Type),
                    DiscountAmount = b.Discount.Amount,
                    DiscountReason = b.Discount.Reason,
                    Notes = b.Notes,
                    Payments = [.. b.Payments.Select(BookingMapper.MapToPaymentDto)],
                    AmountPaid = b.AmountPaid,
                    RemainingBalance = b.RemainingBalance,
                    Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency)
                };
            })
        ];
    }

    public async Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct)
    {
        var booking = await dbContext.Tours
            .SelectMany(t => t.Bookings).Include(booking => booking.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (booking is null)
        {
            return null;
        }

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
            PaymentStatus = BookingMapper.MapToPaymentStatusDto(booking.PaymentStatus),
            TotalPrice = booking.TotalPrice,
            DiscountType = BookingMapper.MapToDiscountTypeDto(booking.Discount.Type),
            DiscountAmount = booking.Discount.Amount,
            DiscountReason = booking.Discount.Reason,
            Notes = booking.Notes,
            Payments = [.. booking.Payments.Select(BookingMapper.MapToPaymentDto)],
            AmountPaid = booking.AmountPaid,
            RemainingBalance = booking.RemainingBalance,
            Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency)
        };
    }

    public async Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct)
    {
        var tour = await dbContext.Tours
            .Include(t => t.Bookings).ThenInclude(booking => booking.Payments)
            .FirstOrDefaultAsync(t => t.Id == tourId, ct);

        if (tour is null)
        {
            return [];
        }

        var customerIds = tour.Bookings.Select(b => b.PrincipalCustomer.CustomerId)
            .Concat(tour.Bookings.Where(b => b.CompanionCustomer != null).Select(b => b.CompanionCustomer!.CustomerId))
            .Distinct()
            .ToArray();

        var customers = await dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return
        [
            ..tour.Bookings
                .OrderByDescending(b => b.Status == BookingStatus.Cancelled)
                .ThenByDescending(b => b.BookingDate)
                .Select(b =>
                {
                    var customer = customers[b.PrincipalCustomer.CustomerId];
                    var companion = b.CompanionCustomer is not null ? customers[b.CompanionCustomer.CustomerId] : null;

                    return new GetBookingDto
                    {
                        Id = b.Id,
                        TourId = b.TourId,
                        TourIdentifier = tour.Identifier,
                        TourName = tour.Name,
                        CustomerId = b.PrincipalCustomer.CustomerId,
                        CustomerName = $"{customer.PersonalInfo.FirstName} {customer.PersonalInfo.LastName}",
                        CompanionId = b.CompanionCustomer?.CustomerId,
                        CompanionName = companion is not null
                            ? $"{companion.PersonalInfo.FirstName} {companion.PersonalInfo.LastName}"
                            : null,
                        RoomType = BookingMapper.MapToRoomTypeDto(b.RoomType),
                        PrincipalBikeType = BookingMapper.MapToBikeTypeDto(b.PrincipalCustomer.BikeType),
                        CompanionBikeType = b.CompanionCustomer is not null
                            ? BookingMapper.MapToBikeTypeDto(b.CompanionCustomer.BikeType)
                            : null,
                        BookingDate = b.BookingDate,
                        Status = BookingMapper.MapToBookingStatusDto(b.Status),
                        PaymentStatus = BookingMapper.MapToPaymentStatusDto(b.PaymentStatus),
                        TotalPrice = b.TotalPrice,
                        DiscountType = BookingMapper.MapToDiscountTypeDto(b.Discount.Type),
                        DiscountAmount = b.Discount.Amount,
                        DiscountReason = b.Discount.Reason,
                        Notes = b.Notes,
                        Payments = [.. b.Payments.Select(BookingMapper.MapToPaymentDto)],
                        AmountPaid = b.AmountPaid,
                        RemainingBalance = b.RemainingBalance,
                        Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency)
                    };
                })
        ];
    }

    public async Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct)
    {
        var bookings = await dbContext.Tours
            .SelectMany(t => t.Bookings)
            .Where(b => b.PrincipalCustomer.CustomerId == customerId ||
                        (b.CompanionCustomer != null && b.CompanionCustomer.CustomerId == customerId))
            .OrderByDescending(b => b.BookingDate).Include(booking => booking.Payments)
            .ToArrayAsync(ct);

        var tourIds = bookings.Select(b => b.TourId).Distinct().ToList();
        var customerIds = bookings.Select(b => b.PrincipalCustomer.CustomerId)
            .Concat(bookings.Where(b => b.CompanionCustomer != null).Select(b => b.CompanionCustomer!.CustomerId))
            .Distinct()
            .ToArray();

        var tours = await dbContext.Tours
            .Where(t => tourIds.Contains(t.Id))
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
                    PaymentStatus = BookingMapper.MapToPaymentStatusDto(b.PaymentStatus),
                    TotalPrice = b.TotalPrice,
                    DiscountType = BookingMapper.MapToDiscountTypeDto(b.Discount.Type),
                    DiscountAmount = b.Discount.Amount,
                    DiscountReason = b.Discount.Reason,
                    Notes = b.Notes,
                    Payments = [.. b.Payments.Select(BookingMapper.MapToPaymentDto)],
                    AmountPaid = b.AmountPaid,
                    RemainingBalance = b.RemainingBalance,
                    Currency = TourMapper.MapToCurrencyDto(tour.Pricing.Currency)
                };
            })
        ];
    }
}
