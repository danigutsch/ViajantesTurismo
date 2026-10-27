using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using PandaBear.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Initializes the Admin database with synthetic development data.
/// </summary>
public sealed class DevelopmentDataInitializer
{
    private readonly AdminWriteDbContext dbContext;
    private readonly TimeProvider timeProvider;

    private const string BreakfastService = "Breakfast";
    private const string BrazilianNationality = "Brazilian";
    private const string FemaleGender = "Female";
    private const string HotelService = "Hotel";
    private const string MaleGender = "Male";

    private static readonly Tour[] Tours =
    [
        Tour.Create(new TourDefinition(
            "CITY001",
            "City Highlights",
            new TourScheduleDefinition(DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(1)), DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(7))),
            new TourPricingDefinition(1500m, 300m, 100m, 200m, Currency.Real),
            new TourCapacityDefinition(6, 15),
            [HotelService, BreakfastService, "City Tour"])).Value,
        Tour.Create(new TourDefinition(
            "HIST002",
            "Historical Landmarks",
            new TourScheduleDefinition(DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(4)), DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(10))),
            new TourPricingDefinition(2000m, 400m, 150m, 250m, Currency.Euro),
            new TourCapacityDefinition(8, 20),
            [HotelService, BreakfastService, "Museum Tickets"])).Value,
        Tour.Create(new TourDefinition(
            "CULT001",
            "Cultural Experience",
            new TourScheduleDefinition(DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(7)), DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(13))),
            new TourPricingDefinition(1800m, 350m, 120m, 220m, Currency.UsDollar),
            new TourCapacityDefinition(4, 12),
            [HotelService, BreakfastService, "Cultural Show"])).Value,
        Tour.Create(new TourDefinition(
            "NATR001",
            "Nature and Adventure",
            new TourScheduleDefinition(DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(11)), DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(17))),
            new TourPricingDefinition(2200m, 450m, 180m, 280m, Currency.Real),
            new TourCapacityDefinition(5, 18),
            [HotelService, BreakfastService, "Hiking Tour"])).Value,
        Tour.Create(new TourDefinition(
            "FOWI003",
            "Food and Wine Tour",
            new TourScheduleDefinition(DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(16)), DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(22))),
            new TourPricingDefinition(2500m, 500m, 200m, 300m, Currency.Euro),
            new TourCapacityDefinition(6, 16),
            [HotelService, BreakfastService, "Wine Tasting"])).Value
    ];

    private static readonly Customer[] Customers =
    [
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Alice", "Smith", FemaleGender, new DateOnly(1990, 1, 1), BrazilianNationality, "Engineer"),
            new IdentificationInfoDefinition("123456789", "Brazilian"),
            new ContactInfoDefinition("alice@example.com", "+5511999999999", "@alice", "alice.fb"),
            new AddressDefinition("Rua A, 123", "Apt 1", "Centro", "01234-567", "São Paulo", "SP", "Brazil"),
            new PhysicalInfoDefinition(60, 165, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Bob Smith", "+5511988888888", "Peanuts", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Bob", "Johnson", MaleGender, new DateOnly(1985, 5, 15), "American", "Teacher"),
            new IdentificationInfoDefinition("987654321", "American"),
            new ContactInfoDefinition("bob@example.com", "+15551234567", null, "bob.johnson"),
            new AddressDefinition("456 Elm St", null, "Manhattan", "10001", "New York", "NY", "USA"),
            new PhysicalInfoDefinition(75, 180, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Jane Johnson", "+15559876543", null, null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Carla", "Santos", FemaleGender, new DateOnly(1995, 10, 20), "Portuguese", "Doctor"),
            new IdentificationInfoDefinition("456789123", "Portuguese"),
            new ContactInfoDefinition("carla@example.com", "+351912345678", "@carla_santos", null),
            new AddressDefinition("Rua B, 456", null, "Alfama", "1100-001", "Lisbon", "Lisbon", "Portugal"),
            new PhysicalInfoDefinition(55, 160, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Pedro Santos", "+351987654321", "Shellfish", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("David", "Lee", MaleGender, new DateOnly(1980, 3, 10), "Korean", "Chef"),
            new IdentificationInfoDefinition("789123456", "Korean"),
            new ContactInfoDefinition("david@example.com", "+821012345678", null, "david.lee"),
            new AddressDefinition("Gangnam-daero 789", null, "Gangnam-gu", "06234", "Seoul", "Seoul", "South Korea"),
            new PhysicalInfoDefinition(70, 175, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Sarah Lee", "+821098765432", "Dairy", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Elena", "Rodriguez", FemaleGender, new DateOnly(1992, 7, 5), "Spanish", "Artist"),
            new IdentificationInfoDefinition("321654987", "Spanish"),
            new ContactInfoDefinition("elena@example.com", "+34612345678", "@elena_art", null),
            new AddressDefinition("Calle C, 789", "Piso 2", "Centro", "28001", "Madrid", "Madrid", "Spain"),
            new PhysicalInfoDefinition(58, 168, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Miguel Rodriguez", "+34698765432", "Pollen", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Frank", "Muller", MaleGender, new DateOnly(1975, 12, 25), "German", "Mechanic"),
            new IdentificationInfoDefinition("654987321", "German"),
            new ContactInfoDefinition("frank@example.com", "+491512345678", null, "frank.muller"),
            new AddressDefinition("Hauptstr. 101", null, "Mitte", "10117", "Berlin", "Berlin", "Germany"),
            new PhysicalInfoDefinition(80, 185, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Anna Muller", "+491598765432", null, null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Gina", "Patel", FemaleGender, new DateOnly(1988, 9, 30), "Indian", "Accountant"),
            new IdentificationInfoDefinition("147258369", "Indian"),
            new ContactInfoDefinition("gina@example.com", "+919876543210", "@gina_patel", null),
            new AddressDefinition("MG Road, 202", null, "Bandra", "400050", "Mumbai", "Maharashtra", "India"),
            new PhysicalInfoDefinition(62, 162, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Raj Patel", "+919876543211", "Nuts", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Hans", "Nielsen", MaleGender, new DateOnly(1998, 4, 14), "Danish", "Student"),
            new IdentificationInfoDefinition("963852741", "Danish"),
            new ContactInfoDefinition("hans@example.com", "+4520123456", null, "hans.nielsen"),
            new AddressDefinition("Vesterbrogade 303", null, "Vesterbro", "1620", "Copenhagen", "Capital Region", "Denmark"),
            new PhysicalInfoDefinition(68, 178, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Lise Nielsen", "+4520987654", "Gluten", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Irina", "Petrov", FemaleGender, new DateOnly(1983, 11, 8), "Russian", "Scientist"),
            new IdentificationInfoDefinition("852741963", "Russian"),
            new ContactInfoDefinition("irina@example.com", "+79123456789", "@irina_petrov", null),
            new AddressDefinition("Tverskaya Ulitsa, 404", null, "Tverskoy", "125009", "Moscow", "Moscow", "Russia"),
            new PhysicalInfoDefinition(56, 170, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Alex Petrov", "+79234567890", null, null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Jack", "Brown", MaleGender, new DateOnly(1991, 6, 22), "Australian", "Photographer"),
            new IdentificationInfoDefinition("741963852", "Australian"),
            new ContactInfoDefinition("jack@example.com", "+61412345678", null, "jack.brown"),
            new AddressDefinition("Collins Street, 505", null, "CBD", "3000", "Melbourne", "Victoria", "Australia"),
            new PhysicalInfoDefinition(72, 182, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Emma Brown", "+61498765432", "Seafood", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Karen", "Tanaka", FemaleGender, new DateOnly(1987, 2, 14), "Japanese", "Architect"),
            new IdentificationInfoDefinition("159357486", "Japanese"),
            new ContactInfoDefinition("karen@example.com", "+81901234567", "@karen_tanaka", null),
            new AddressDefinition("Shibuya 1-2-3", null, "Shibuya-ku", "150-0002", "Tokyo", "Tokyo", "Japan"),
            new PhysicalInfoDefinition(52, 158, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Yuki Tanaka", "+81909876543", null, null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Leo", "Costa", MaleGender, new DateOnly(1993, 8, 3), BrazilianNationality, "Software Developer"),
            new IdentificationInfoDefinition("264835791", "Brazilian"),
            new ContactInfoDefinition("leo@example.com", "+5521999887766", null, "leo.costa"),
            new AddressDefinition("Av. Paulista, 1000", "Sala 10", "Bela Vista", "01310-100", "São Paulo", "SP", "Brazil"),
            new PhysicalInfoDefinition(78, 176, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Ana Costa", "+5521988776655", "Latex", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Maria", "Gonzalez", FemaleGender, new DateOnly(1989, 12, 1), "Mexican", "Journalist"),
            new IdentificationInfoDefinition("375924681", "Mexican"),
            new ContactInfoDefinition("maria@example.com", "+521234567890", "@maria_g", null),
            new AddressDefinition("Reforma 222", null, "Juárez", "06600", "Mexico City", "CDMX", "Mexico"),
            new PhysicalInfoDefinition(60, 163, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Carlos Gonzalez", "+521987654321", "Penicillin", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Nora", "Eriksson", FemaleGender, new DateOnly(1996, 5, 18), "Swedish", "Nurse"),
            new IdentificationInfoDefinition("486135792", "Swedish"),
            new ContactInfoDefinition("nora@example.com", "+46701234567", null, "nora.eriksson"),
            new AddressDefinition("Kungsgatan 44", null, "Norrmalm", "111 35", "Stockholm", "Stockholm", "Sweden"),
            new PhysicalInfoDefinition(64, 172, BikeType.EBike),
            new AccommodationPreferencesDefinition(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            new CustomerHealthDefinition("Erik Eriksson", "+46709876543", null, null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value,
        Customer.Create(new CustomerDefinition(
            new PersonalInfoDefinition("Oscar", "Fischer", MaleGender, new DateOnly(1982, 9, 27), "Austrian", "Musician"),
            new IdentificationInfoDefinition("597246813", "Austrian"),
            new ContactInfoDefinition("oscar@example.com", "+43664123456", "@oscar_music", null),
            new AddressDefinition("Mariahilfer Straße, 88", null, "Mariahilf", "1060", "Vienna", "Vienna", "Austria"),
            new PhysicalInfoDefinition(74, 179, BikeType.Regular),
            new AccommodationPreferencesDefinition(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            new CustomerHealthDefinition("Sabine Fischer", "+43664987654", "Aspirin", null)),
            DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value
    ];

    private static readonly string[] BaselineTourIdentifiers =
        Tours.Select(static tour => tour.Identifier).ToArray();

    private static readonly string[] BaselineCustomerNationalIds =
        Customers.Select(static customer => customer.IdentificationInfo.NationalId).ToArray();

    /// <summary>Creates the initializer over the write context and time source.</summary>
    /// <param name="dbContext">The write database context.</param>
    /// <param name="timeProvider">The clock used for development-data timestamps.</param>
    internal DevelopmentDataInitializer(AdminWriteDbContext dbContext, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Resumes recognized development-data checkpoints inside one transaction.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async Task Initialize(CancellationToken ct)
    {
        var concurrentRetries = 0;
        while (true)
        {
            try
            {
                _ = await EfCoreCommandTransactionScope.Execute(
                    dbContext,
                    () => InitializeCore(ct),
                    ct);
                return;
            }
            catch (Exception exception) when (
                exception is DbUpdateException
                or InvalidOperationException { InnerException: DbUpdateException })
            {
                dbContext.ChangeTracker.Clear();
                if (await IsInitialized(ct))
                {
                    return;
                }

                if (concurrentRetries++ >= 1)
                {
                    throw;
                }
            }
        }
    }

    private async ValueTask<bool> InitializeCore(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        dbContext.ChangeTracker.Clear();

        var existingCustomerNationalIds = await dbContext.Customers
            .Select(static customer => customer.IdentificationInfo.NationalId)
            .ToArrayAsync(ct);
        var existingTourIdentifiers = await dbContext.Tours
            .Select(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        var hasNoCustomers = existingCustomerNationalIds.Length == 0;
        var hasExactBaselineCustomers = HasExactValues(
            existingCustomerNationalIds,
            BaselineCustomerNationalIds);
        var hasNoTours = existingTourIdentifiers.Length == 0;
        var hasExactBaselineTours = HasExactValues(existingTourIdentifiers, BaselineTourIdentifiers);
        var isRecognizedCheckpoint = (
            hasNoTours,
            hasExactBaselineTours,
            hasNoCustomers,
            hasExactBaselineCustomers) switch
        {
            (true, _, true, _) => true,
            (false, true, true, _) => true,
            (false, true, false, true) => true,
            _ => false
        };
        if (!isRecognizedCheckpoint)
        {
            return false;
        }

        var baselineTours = await ResolveBaselineTours(hasNoTours, ct);
        if (baselineTours is null)
        {
            return false;
        }

        Customer[] baselineCustomers;
        if (hasNoCustomers)
        {
            baselineCustomers = Customers
                .Select(static customer => Customer.Create(
                    CustomerMapper.MapToDefinition(customer),
                    DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime)).Value)
                .OrderBy(static customer => customer.Id)
                .ToArray();
        }
        else
        {
            baselineCustomers = await dbContext.Customers
                .Where(customer => BaselineCustomerNationalIds.Contains(customer.IdentificationInfo.NationalId))
                .OrderBy(static customer => customer.Id)
                .ToArrayAsync(ct);
        }

        if (baselineCustomers.Length != Customers.Length ||
            baselineCustomers.Any(static customer => !MatchesBaselineCustomer(customer)))
        {
            return false;
        }

        if (hasNoCustomers)
        {
            dbContext.Customers.AddRange(baselineCustomers);
        }

        var baselineTourIds = baselineTours.Select(static tour => tour.Id).ToArray();
        var hasBaselineBookings = await dbContext.Tours
            .Where(tour => baselineTourIds.Contains(tour.Id))
            .SelectMany(static tour => tour.Bookings)
            .AnyAsync(ct);
        if (!hasBaselineBookings)
        {
            SeedBookings(baselineTours, baselineCustomers);
            await dbContext.SaveChangesAsync(ct);
            return true;
        }

        baselineTours = await dbContext.Tours
            .Where(tour => baselineTourIds.Contains(tour.Id))
            .Include(static tour => tour.Bookings)

            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        var baselineBookings = baselineTours
            .SelectMany(static tour => tour.Bookings)
            .ToArray();
        var bookingIds = baselineBookings.Select(booking => booking.Id).ToArray();
        var hasFinancialHistory = await dbContext.Set<PaymentHistoryRow>().AnyAsync(row => bookingIds.Contains(row.BookingId), ct);
        var isPendingBookingCheckpoint = !hasFinancialHistory && baselineBookings.Length == 10 &&
            baselineBookings.All(static booking =>
                booking.Status == BookingStatus.Pending);
        if (!isPendingBookingCheckpoint || !TryCompleteBookingStates(baselineTours, baselineCustomers))
        {
            return false;
        }

        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    private async ValueTask<bool> IsInitialized(CancellationToken ct)
    {
        var customerNationalIds = await dbContext.Customers
            .Select(static customer => customer.IdentificationInfo.NationalId)
            .ToArrayAsync(ct);
        var tourIdentifiers = await dbContext.Tours
            .Select(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        if (!HasExactValues(customerNationalIds, BaselineCustomerNationalIds)
            || !HasExactValues(tourIdentifiers, BaselineTourIdentifiers))
        {
            return false;
        }

        var bookingCount = await dbContext.Tours.SelectMany(static tour => tour.Bookings).CountAsync(ct);
        return bookingCount == 10;
    }

    private static bool HasExactValues(string[] actual, string[] expected) =>
        actual.Length == expected.Length &&
        actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected);

    private async Task<Tour[]?> ResolveBaselineTours(bool hasNoTours, CancellationToken ct)
    {
        if (hasNoTours)
        {
            var newTours = Tours
                .Select(static tour => Tour.Create(new TourDefinition(
                    tour.Identifier,
                    tour.Name,
                    new TourScheduleDefinition(tour.Schedule.StartDate, tour.Schedule.EndDate),
                    new TourPricingDefinition(
                        tour.Pricing.BasePrice,
                        tour.Pricing.SingleRoomSupplementPrice,
                        tour.Pricing.RegularBikePrice,
                        tour.Pricing.EBikePrice,
                        tour.Pricing.Currency),
                    new TourCapacityDefinition(tour.Capacity.MinCustomers, tour.Capacity.MaxCustomers),
                    tour.IncludedServices)).Value)
                .OrderBy(static tour => tour.Identifier)
                .ToArray();
            dbContext.Tours.AddRange(newTours);
            return newTours;
        }

        var existingTours = await dbContext.Tours
            .Where(tour => BaselineTourIdentifiers.Contains(tour.Identifier))
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        return existingTours.Length == Tours.Length &&
            existingTours.All(static tour => MatchesBaselineTour(tour))
                ? existingTours
                : null;
    }

    private void SeedBookings(
        Tour[] tours,
        Customer[] customers)
    {
        if (tours.Length < 5 || customers.Length < 15)
        {
            return;
        }

        _ = tours[0].AddBooking(TourBookingRequest.CreateSingle(
            customers[0].Id,
            customers[0].PhysicalInfo.BikeType,
            customers[0].AccommodationPreferences.RoomType,
            notes: "Early bird discount applied"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[1].AddBooking(TourBookingRequest.CreateDouble(
            customers[1].Id,
            customers[1].PhysicalInfo.BikeType,
            customers[0].Id,
            customers[0].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Traveling together as a couple"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[2].AddBooking(TourBookingRequest.CreateSingle(
            customers[2].Id,
            customers[2].PhysicalInfo.BikeType,
            customers[2].AccommodationPreferences.RoomType,
            notes: "Pending with partial payment, awaiting full payment"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[3].AddBooking(TourBookingRequest.CreateDouble(
            customers[3].Id,
            customers[3].PhysicalInfo.BikeType,
            customers[4].Id,
            customers[4].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Upgraded to premium accommodation"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[4].AddBooking(TourBookingRequest.CreateSingle(
            customers[5].Id,
            customers[5].PhysicalInfo.BikeType,
            customers[5].AccommodationPreferences.RoomType,
            notes: "Excellent tour experience"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[0].AddBooking(TourBookingRequest.CreateSingle(
            customers[6].Id,
            customers[6].PhysicalInfo.BikeType,
            customers[6].AccommodationPreferences.RoomType,
            notes: "Cancelled due to personal reasons"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[1].AddBooking(TourBookingRequest.CreateDouble(
            customers[7].Id,
            customers[7].PhysicalInfo.BikeType,
            customers[8].Id,
            customers[8].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Special dietary requirements noted"), timeProvider.GetUtcNow().UtcDateTime).Value;
        tours[3].AddBooking(TourBookingRequest.CreateSingle(
            customers[9].Id,
            customers[9].PhysicalInfo.BikeType,
            customers[9].AccommodationPreferences.RoomType,
            notes: "Interested in photography opportunities"), timeProvider.GetUtcNow().UtcDateTime);
        _ = tours[0].AddBooking(TourBookingRequest.CreateSingle(
            customers[4].Id,
            customers[4].PhysicalInfo.BikeType,
            RoomType.SingleOccupancy,
            notes: "Solo traveler, single room supplement included"), timeProvider.GetUtcNow().UtcDateTime).Value;
        _ = tours[4].AddBooking(TourBookingRequest.CreateSingle(
            customers[8].Id,
            customers[8].PhysicalInfo.BikeType,
            customers[8].AccommodationPreferences.RoomType,
            notes: "Payment pending bank transfer"), timeProvider.GetUtcNow().UtcDateTime).Value;

        if (!TryCompleteBookingStates(tours, customers))
        {
            throw new InvalidOperationException("The baseline booking graph could not be completed.");
        }
    }

    private static bool TryCompleteBookingStates(Tour[] tours, Customer[] customers)
    {
        var booking1 = tours[0].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[0],
                customers[0],
                null,
                customers[0].AccommodationPreferences.RoomType,
                "Early bird discount applied"));
        var booking2 = tours[1].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[1],
                customers[1],
                customers[0],
                RoomType.DoubleOccupancy,
                "Traveling together as a couple"));
        var booking3 = tours[2].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[2],
                customers[2],
                null,
                customers[2].AccommodationPreferences.RoomType,
                "Pending with partial payment, awaiting full payment"));
        var booking4 = tours[3].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[3],
                customers[3],
                customers[4],
                RoomType.DoubleOccupancy,
                "Upgraded to premium accommodation"));
        var booking5 = tours[4].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[4],
                customers[5],
                null,
                customers[5].AccommodationPreferences.RoomType,
                "Excellent tour experience"));
        var booking6 = tours[0].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[0],
                customers[6],
                null,
                customers[6].AccommodationPreferences.RoomType,
                "Cancelled due to personal reasons"));
        var booking7 = tours[1].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[1],
                customers[7],
                customers[8],
                RoomType.DoubleOccupancy,
                "Special dietary requirements noted"));
        var booking8 = tours[3].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[3],
                customers[9],
                null,
                customers[9].AccommodationPreferences.RoomType,
                "Interested in photography opportunities"));
        var booking9 = tours[0].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[0],
                customers[4],
                null,
                RoomType.SingleOccupancy,
                "Solo traveler, single room supplement included"));
        var booking10 = tours[4].Bookings.FirstOrDefault(
            booking => MatchesBaselineBooking(
                booking,
                tours[4],
                customers[8],
                null,
                customers[8].AccommodationPreferences.RoomType,
                "Payment pending bank transfer"));
        if (booking1 is null ||
            booking2 is null ||
            booking3 is null ||
            booking4 is null ||
            booking5 is null ||
            booking6 is null ||
            booking7 is null ||
            booking8 is null ||
            booking9 is null ||
            booking10 is null)
        {
            return false;
        }


        EnsureSeedOperationSucceeded(tours[0].ConfirmBooking(booking1.Id));

        EnsureSeedOperationSucceeded(tours[1].ConfirmBooking(booking2.Id));

        EnsureSeedOperationSucceeded(tours[3].ConfirmBooking(booking4.Id));

        EnsureSeedOperationSucceeded(tours[4].ConfirmBooking(booking5.Id));
        EnsureSeedOperationSucceeded(tours[4].CompleteBooking(booking5.Id));
        EnsureSeedOperationSucceeded(tours[0].CancelBooking(booking6.Id));

        EnsureSeedOperationSucceeded(tours[1].ConfirmBooking(booking7.Id));

        EnsureSeedOperationSucceeded(tours[0].ConfirmBooking(booking9.Id));

        EnsureSeedOperationSucceeded(tours[4].ConfirmBooking(booking10.Id));


        return true;
    }

    private static void EnsureSeedOperationSucceeded(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException("A baseline booking operation failed.");
        }
    }

    private static void EnsureSeedOperationSucceeded<T>(Result<T> result)
        where T : notnull
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException("A baseline booking operation failed.");
        }
    }

    private static bool MatchesBaselineBooking(
        Booking booking,
        Tour tour,
        Customer principalCustomer,
        Customer? companionCustomer,
        RoomType roomType,
        string notes)
    {
        var expectedPrincipalBikePrice = principalCustomer.PhysicalInfo.BikeType switch
        {
            BikeType.Regular => tour.Pricing.RegularBikePrice,
            BikeType.EBike => tour.Pricing.EBikePrice,
            _ => 0m
        };
        var principalMatches = booking.PrincipalCustomer.CustomerId == principalCustomer.Id &&
            booking.PrincipalCustomer.BikeType == principalCustomer.PhysicalInfo.BikeType &&
            booking.PrincipalCustomer.BikePrice == expectedPrincipalBikePrice;
        var companionMatches = companionCustomer is null
            ? booking.CompanionCustomer is null
            : booking.CompanionCustomer is not null &&
              booking.CompanionCustomer.CustomerId == companionCustomer.Id &&
              booking.CompanionCustomer.BikeType == companionCustomer.PhysicalInfo.BikeType &&
              booking.CompanionCustomer.BikePrice == (companionCustomer.PhysicalInfo.BikeType switch
              {
                  BikeType.Regular => tour.Pricing.RegularBikePrice,
                  BikeType.EBike => tour.Pricing.EBikePrice,
                  _ => 0m
              });
        var expectedRoomAdditionalCost = roomType == RoomType.SingleOccupancy
            ? tour.Pricing.SingleRoomSupplementPrice
            : 0m;

        return booking.TourId == tour.Id &&
            booking.BasePrice == tour.Pricing.BasePrice &&
            principalMatches &&
            companionMatches &&
            booking.RoomType == roomType &&
            booking.RoomAdditionalCost == expectedRoomAdditionalCost &&
            booking.Discount.Type == DiscountType.None &&
            booking.Discount.Amount == 0m &&
            booking.Discount.Reason is null &&
            string.Equals(booking.Notes, notes, StringComparison.Ordinal);
    }

    private static bool MatchesBaselineTour(Tour actual)
    {
        var expected = Tours.FirstOrDefault(
            tour => string.Equals(tour.Identifier, actual.Identifier, StringComparison.Ordinal));
        if (expected is null)
        {
            return false;
        }

        var durationMatches = actual.Schedule.DurationDays == expected.Schedule.DurationDays;

        return string.Equals(actual.Name, expected.Name, StringComparison.Ordinal) &&
            durationMatches &&
            actual.Pricing.BasePrice == expected.Pricing.BasePrice &&
            actual.Pricing.SingleRoomSupplementPrice == expected.Pricing.SingleRoomSupplementPrice &&
            actual.Pricing.RegularBikePrice == expected.Pricing.RegularBikePrice &&
            actual.Pricing.EBikePrice == expected.Pricing.EBikePrice &&
            actual.Pricing.Currency == expected.Pricing.Currency &&
            actual.Capacity.MinCustomers == expected.Capacity.MinCustomers &&
            actual.Capacity.MaxCustomers == expected.Capacity.MaxCustomers &&
            actual.IncludedServices.SequenceEqual(expected.IncludedServices, StringComparer.Ordinal);
    }

    private static bool MatchesBaselineCustomer(Customer actual)
    {
        var expected = Customers.FirstOrDefault(customer => string.Equals(
            customer.IdentificationInfo.NationalId,
            actual.IdentificationInfo.NationalId,
            StringComparison.Ordinal));
        if (expected is null)
        {
            return false;
        }

        return string.Equals(actual.PersonalInfo.FirstName, expected.PersonalInfo.FirstName, StringComparison.Ordinal) &&
            string.Equals(actual.PersonalInfo.LastName, expected.PersonalInfo.LastName, StringComparison.Ordinal) &&
            string.Equals(actual.PersonalInfo.Gender, expected.PersonalInfo.Gender, StringComparison.Ordinal) &&
            actual.PersonalInfo.BirthDate == expected.PersonalInfo.BirthDate &&
            string.Equals(actual.PersonalInfo.Nationality, expected.PersonalInfo.Nationality, StringComparison.Ordinal) &&
            string.Equals(actual.PersonalInfo.Occupation, expected.PersonalInfo.Occupation, StringComparison.Ordinal) &&
            string.Equals(
                actual.IdentificationInfo.IdNationality,
                expected.IdentificationInfo.IdNationality,
                StringComparison.Ordinal) &&
            string.Equals(actual.ContactInfo.Email, expected.ContactInfo.Email, StringComparison.Ordinal) &&
            string.Equals(actual.ContactInfo.Mobile, expected.ContactInfo.Mobile, StringComparison.Ordinal) &&
            string.Equals(actual.ContactInfo.Instagram, expected.ContactInfo.Instagram, StringComparison.Ordinal) &&
            string.Equals(actual.ContactInfo.Facebook, expected.ContactInfo.Facebook, StringComparison.Ordinal) &&
            string.Equals(actual.Address.Street, expected.Address.Street, StringComparison.Ordinal) &&
            string.Equals(actual.Address.Complement, expected.Address.Complement, StringComparison.Ordinal) &&
            string.Equals(actual.Address.Neighborhood, expected.Address.Neighborhood, StringComparison.Ordinal) &&
            string.Equals(actual.Address.PostalCode, expected.Address.PostalCode, StringComparison.Ordinal) &&
            string.Equals(actual.Address.City, expected.Address.City, StringComparison.Ordinal) &&
            string.Equals(actual.Address.State, expected.Address.State, StringComparison.Ordinal) &&
            string.Equals(actual.Address.Country, expected.Address.Country, StringComparison.Ordinal) &&
            actual.PhysicalInfo.WeightKg == expected.PhysicalInfo.WeightKg &&
            actual.PhysicalInfo.HeightCentimeters == expected.PhysicalInfo.HeightCentimeters &&
            actual.PhysicalInfo.BikeType == expected.PhysicalInfo.BikeType &&
            actual.AccommodationPreferences.RoomType == expected.AccommodationPreferences.RoomType &&
            actual.AccommodationPreferences.BedType == expected.AccommodationPreferences.BedType &&
            actual.AccommodationPreferences.CompanionId == expected.AccommodationPreferences.CompanionId &&
            string.Equals(actual.EmergencyContact.Name, expected.EmergencyContact.Name, StringComparison.Ordinal) &&
            string.Equals(actual.EmergencyContact.Mobile, expected.EmergencyContact.Mobile, StringComparison.Ordinal) &&
            string.Equals(actual.MedicalInfo.Allergies, expected.MedicalInfo.Allergies, StringComparison.Ordinal) &&
            string.Equals(actual.MedicalInfo.AdditionalInfo, expected.MedicalInfo.AdditionalInfo, StringComparison.Ordinal);
    }
}
