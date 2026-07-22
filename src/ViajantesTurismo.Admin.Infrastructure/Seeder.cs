using Microsoft.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Seeds the Admin database with development baseline data.
/// </summary>
public sealed class Seeder
{
    private readonly AdminWriteDbContext dbContext;

    private const string BreakfastService = "Breakfast";
    private const string BrazilianNationality = "Brazilian";
    private const string FemaleGender = "Female";
    private const string HotelService = "Hotel";
    private const string MaleGender = "Male";

    private static DateTime UtcDate(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Tour[] Tours =
    [
        Tour.Create(new TourDefinition(
            "CITY001",
            "City Highlights",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(7)),
            new TourPricingDefinition(1500m, 300m, 100m, 200m, Currency.Real),
            new TourCapacityDefinition(6, 15),
            [HotelService, BreakfastService, "City Tour"])).Value,
        Tour.Create(new TourDefinition(
            "HIST002",
            "Historical Landmarks",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(4), DateTime.UtcNow.AddDays(10)),
            new TourPricingDefinition(2000m, 400m, 150m, 250m, Currency.Euro),
            new TourCapacityDefinition(8, 20),
            [HotelService, BreakfastService, "Museum Tickets"])).Value,
        Tour.Create(new TourDefinition(
            "CULT001",
            "Cultural Experience",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(13)),
            new TourPricingDefinition(1800m, 350m, 120m, 220m, Currency.UsDollar),
            new TourCapacityDefinition(4, 12),
            [HotelService, BreakfastService, "Cultural Show"])).Value,
        Tour.Create(new TourDefinition(
            "NATR001",
            "Nature and Adventure",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(11), DateTime.UtcNow.AddDays(17)),
            new TourPricingDefinition(2200m, 450m, 180m, 280m, Currency.Real),
            new TourCapacityDefinition(5, 18),
            [HotelService, BreakfastService, "Hiking Tour"])).Value,
        Tour.Create(new TourDefinition(
            "FOWI003",
            "Food and Wine Tour",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(16), DateTime.UtcNow.AddDays(22)),
            new TourPricingDefinition(2500m, 500m, 200m, 300m, Currency.Euro),
            new TourCapacityDefinition(6, 16),
            [HotelService, BreakfastService, "Wine Tasting"])).Value
    ];

    private static readonly Customer[] Customers =
    [
        new(
            PersonalInfo.Create("Alice", "Smith", FemaleGender, UtcDate(1990, 1, 1), BrazilianNationality, "Engineer", TimeProvider.System).Value,
            IdentificationInfo.Create("123456789", "Brazilian").Value,
            ContactInfo.Create("alice@example.com", "+5511999999999", "@alice", "alice.fb").Value,
            Address.Create("Rua A, 123", "Apt 1", "Centro", "01234-567", "São Paulo", "SP", "Brazil").Value,
            PhysicalInfo.Create(60, 165, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Bob Smith", "+5511988888888").Value,
            MedicalInfo.Create("Peanuts", null).Value
        ),
        new(
            PersonalInfo.Create("Bob", "Johnson", MaleGender, UtcDate(1985, 5, 15), "American", "Teacher", TimeProvider.System).Value,
            IdentificationInfo.Create("987654321", "American").Value,
            ContactInfo.Create("bob@example.com", "+15551234567", null, "bob.johnson").Value,
            Address.Create("456 Elm St", null, "Manhattan", "10001", "New York", "NY", "USA").Value,
            PhysicalInfo.Create(75, 180, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Jane Johnson", "+15559876543").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Carla", "Santos", FemaleGender, UtcDate(1995, 10, 20), "Portuguese", "Doctor", TimeProvider.System).Value,
            IdentificationInfo.Create("456789123", "Portuguese").Value,
            ContactInfo.Create("carla@example.com", "+351912345678", "@carla_santos", null).Value,
            Address.Create("Rua B, 456", null, "Alfama", "1100-001", "Lisbon", "Lisbon", "Portugal").Value,
            PhysicalInfo.Create(55, 160, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Pedro Santos", "+351987654321").Value,
            MedicalInfo.Create("Shellfish", null).Value
        ),
        new(
            PersonalInfo.Create("David", "Lee", MaleGender, UtcDate(1980, 3, 10), "Korean", "Chef", TimeProvider.System).Value,
            IdentificationInfo.Create("789123456", "Korean").Value,
            ContactInfo.Create("david@example.com", "+821012345678", null, "david.lee").Value,
            Address.Create("Gangnam-daero 789", null, "Gangnam-gu", "06234", "Seoul", "Seoul", "South Korea").Value,
            PhysicalInfo.Create(70, 175, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Sarah Lee", "+821098765432").Value,
            MedicalInfo.Create("Dairy", null).Value
        ),
        new(
            PersonalInfo.Create("Elena", "Rodriguez", FemaleGender, UtcDate(1992, 7, 5), "Spanish", "Artist", TimeProvider.System).Value,
            IdentificationInfo.Create("321654987", "Spanish").Value,
            ContactInfo.Create("elena@example.com", "+34612345678", "@elena_art", null).Value,
            Address.Create("Calle C, 789", "Piso 2", "Centro", "28001", "Madrid", "Madrid", "Spain").Value,
            PhysicalInfo.Create(58, 168, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Miguel Rodriguez", "+34698765432").Value,
            MedicalInfo.Create("Pollen", null).Value
        ),
        new(
            PersonalInfo.Create("Frank", "Muller", MaleGender, UtcDate(1975, 12, 25), "German", "Mechanic", TimeProvider.System).Value,
            IdentificationInfo.Create("654987321", "German").Value,
            ContactInfo.Create("frank@example.com", "+491512345678", null, "frank.muller").Value,
            Address.Create("Hauptstr. 101", null, "Mitte", "10117", "Berlin", "Berlin", "Germany").Value,
            PhysicalInfo.Create(80, 185, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Anna Muller", "+491598765432").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Gina", "Patel", FemaleGender, UtcDate(1988, 9, 30), "Indian", "Accountant", TimeProvider.System).Value,
            IdentificationInfo.Create("147258369", "Indian").Value,
            ContactInfo.Create("gina@example.com", "+919876543210", "@gina_patel", null).Value,
            Address.Create("MG Road, 202", null, "Bandra", "400050", "Mumbai", "Maharashtra", "India").Value,
            PhysicalInfo.Create(62, 162, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Raj Patel", "+919876543211").Value,
            MedicalInfo.Create("Nuts", null).Value
        ),
        new(
            PersonalInfo.Create("Hans", "Nielsen", MaleGender, UtcDate(1998, 4, 14), "Danish", "Student", TimeProvider.System).Value,
            IdentificationInfo.Create("963852741", "Danish").Value,
            ContactInfo.Create("hans@example.com", "+4520123456", null, "hans.nielsen").Value,
            Address.Create("Vesterbrogade 303", null, "Vesterbro", "1620", "Copenhagen", "Capital Region", "Denmark").Value,
            PhysicalInfo.Create(68, 178, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Lise Nielsen", "+4520987654").Value,
            MedicalInfo.Create("Gluten", null).Value
        ),
        new(
            PersonalInfo.Create("Irina", "Petrov", FemaleGender, UtcDate(1983, 11, 8), "Russian", "Scientist", TimeProvider.System).Value,
            IdentificationInfo.Create("852741963", "Russian").Value,
            ContactInfo.Create("irina@example.com", "+79123456789", "@irina_petrov", null).Value,
            Address.Create("Tverskaya Ulitsa, 404", null, "Tverskoy", "125009", "Moscow", "Moscow", "Russia").Value,
            PhysicalInfo.Create(56, 170, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Alex Petrov", "+79234567890").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Jack", "Brown", MaleGender, UtcDate(1991, 6, 22), "Australian", "Photographer", TimeProvider.System).Value,
            IdentificationInfo.Create("741963852", "Australian").Value,
            ContactInfo.Create("jack@example.com", "+61412345678", null, "jack.brown").Value,
            Address.Create("Collins Street, 505", null, "CBD", "3000", "Melbourne", "Victoria", "Australia").Value,
            PhysicalInfo.Create(72, 182, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Emma Brown", "+61498765432").Value,
            MedicalInfo.Create("Seafood", null).Value
        ),
        new(
            PersonalInfo.Create("Karen", "Tanaka", FemaleGender, UtcDate(1987, 2, 14), "Japanese", "Architect", TimeProvider.System).Value,
            IdentificationInfo.Create("159357486", "Japanese").Value,
            ContactInfo.Create("karen@example.com", "+81901234567", "@karen_tanaka", null).Value,
            Address.Create("Shibuya 1-2-3", null, "Shibuya-ku", "150-0002", "Tokyo", "Tokyo", "Japan").Value,
            PhysicalInfo.Create(52, 158, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Yuki Tanaka", "+81909876543").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Leo", "Costa", MaleGender, UtcDate(1993, 8, 3), BrazilianNationality, "Software Developer", TimeProvider.System).Value,
            IdentificationInfo.Create("264835791", "Brazilian").Value,
            ContactInfo.Create("leo@example.com", "+5521999887766", null, "leo.costa").Value,
            Address.Create("Av. Paulista, 1000", "Sala 10", "Bela Vista", "01310-100", "São Paulo", "SP", "Brazil").Value,
            PhysicalInfo.Create(78, 176, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Ana Costa", "+5521988776655").Value,
            MedicalInfo.Create("Latex", null).Value
        ),
        new(
            PersonalInfo.Create("Maria", "Gonzalez", FemaleGender, UtcDate(1989, 12, 1), "Mexican", "Journalist", TimeProvider.System).Value,
            IdentificationInfo.Create("375924681", "Mexican").Value,
            ContactInfo.Create("maria@example.com", "+521234567890", "@maria_g", null).Value,
            Address.Create("Reforma 222", null, "Juárez", "06600", "Mexico City", "CDMX", "Mexico").Value,
            PhysicalInfo.Create(60, 163, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Carlos Gonzalez", "+521987654321").Value,
            MedicalInfo.Create("Penicillin", null).Value
        ),
        new(
            PersonalInfo.Create("Nora", "Eriksson", FemaleGender, UtcDate(1996, 5, 18), "Swedish", "Nurse", TimeProvider.System).Value,
            IdentificationInfo.Create("486135792", "Swedish").Value,
            ContactInfo.Create("nora@example.com", "+46701234567", null, "nora.eriksson").Value,
            Address.Create("Kungsgatan 44", null, "Norrmalm", "111 35", "Stockholm", "Stockholm", "Sweden").Value,
            PhysicalInfo.Create(64, 172, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleOccupancy, BedType.DoubleBed, null),
            EmergencyContact.Create("Erik Eriksson", "+46709876543").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Oscar", "Fischer", MaleGender, UtcDate(1982, 9, 27), "Austrian", "Musician", TimeProvider.System).Value,
            IdentificationInfo.Create("597246813", "Austrian").Value,
            ContactInfo.Create("oscar@example.com", "+43664123456", "@oscar_music", null).Value,
            Address.Create("Mariahilfer Straße, 88", null, "Mariahilf", "1060", "Vienna", "Vienna", "Austria").Value,
            PhysicalInfo.Create(74, 179, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.SingleBed, null),
            EmergencyContact.Create("Sabine Fischer", "+43664987654").Value,
            MedicalInfo.Create("Aspirin", null).Value
        )
    ];

    private static readonly string[] BaselineTourIdentifiers =
        Tours.Select(static tour => tour.Identifier).ToArray();

    private static readonly string[] BaselineCustomerNationalIds =
        Customers.Select(static customer => customer.IdentificationInfo.NationalId).ToArray();

    internal Seeder(AdminWriteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    /// <summary>
    /// Applies Admin database migrations and resumes known development baseline seeding.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async Task Seed(CancellationToken ct)
    {
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(ct);
        }

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
            return;
        }

        Tour[] baselineTours;
        if (hasNoTours)
        {
            baselineTours = Tours
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
        }
        else
        {
            baselineTours = await dbContext.Tours
                .Where(tour => BaselineTourIdentifiers.Contains(tour.Identifier))
                .OrderBy(static tour => tour.Identifier)
                .ToArrayAsync(ct);
        }

        if (baselineTours.Length != Tours.Length || baselineTours.Any(static tour => !MatchesBaselineTour(tour)))
        {
            return;
        }

        if (hasNoTours)
        {
            dbContext.Tours.AddRange(baselineTours);
        }

        Customer[] baselineCustomers;
        if (hasNoCustomers)
        {
            baselineCustomers = Customers
                .Select(static customer => new Customer(
                    customer.PersonalInfo,
                    customer.IdentificationInfo,
                    customer.ContactInfo,
                    customer.Address,
                    customer.PhysicalInfo,
                    customer.AccommodationPreferences,
                    customer.EmergencyContact,
                    customer.MedicalInfo))
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
            return;
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
            return;
        }

        baselineTours = await dbContext.Tours
            .Where(tour => baselineTourIds.Contains(tour.Id))
            .Include(static tour => tour.Bookings)
            .ThenInclude(static booking => booking.Payments)
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        var baselineBookings = baselineTours
            .SelectMany(static tour => tour.Bookings)
            .ToArray();
        var isPendingBookingCheckpoint = baselineBookings.Length == 10 &&
            baselineBookings.All(static booking =>
                booking.Status == BookingStatus.Pending && booking.Payments.Count == 0);
        if (!isPendingBookingCheckpoint || !TryCompleteBookingStates(baselineTours, baselineCustomers))
        {
            return;
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static bool HasExactValues(string[] actual, string[] expected) =>
        actual.Length == expected.Length &&
        actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected);

    private static void SeedBookings(
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
            notes: "Early bird discount applied")).Value;
        _ = tours[1].AddBooking(TourBookingRequest.CreateDouble(
            customers[1].Id,
            customers[1].PhysicalInfo.BikeType,
            customers[0].Id,
            customers[0].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Traveling together as a couple")).Value;
        _ = tours[2].AddBooking(TourBookingRequest.CreateSingle(
            customers[2].Id,
            customers[2].PhysicalInfo.BikeType,
            customers[2].AccommodationPreferences.RoomType,
            notes: "Pending with partial payment, awaiting full payment")).Value;
        _ = tours[3].AddBooking(TourBookingRequest.CreateDouble(
            customers[3].Id,
            customers[3].PhysicalInfo.BikeType,
            customers[4].Id,
            customers[4].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Upgraded to premium accommodation")).Value;
        _ = tours[4].AddBooking(TourBookingRequest.CreateSingle(
            customers[5].Id,
            customers[5].PhysicalInfo.BikeType,
            customers[5].AccommodationPreferences.RoomType,
            notes: "Excellent tour experience")).Value;
        _ = tours[0].AddBooking(TourBookingRequest.CreateSingle(
            customers[6].Id,
            customers[6].PhysicalInfo.BikeType,
            customers[6].AccommodationPreferences.RoomType,
            notes: "Cancelled due to personal reasons")).Value;
        _ = tours[1].AddBooking(TourBookingRequest.CreateDouble(
            customers[7].Id,
            customers[7].PhysicalInfo.BikeType,
            customers[8].Id,
            customers[8].PhysicalInfo.BikeType,
            RoomType.DoubleOccupancy,
            notes: "Special dietary requirements noted")).Value;
        tours[3].AddBooking(TourBookingRequest.CreateSingle(
            customers[9].Id,
            customers[9].PhysicalInfo.BikeType,
            customers[9].AccommodationPreferences.RoomType,
            notes: "Interested in photography opportunities"));
        _ = tours[0].AddBooking(TourBookingRequest.CreateSingle(
            customers[4].Id,
            customers[4].PhysicalInfo.BikeType,
            RoomType.SingleOccupancy,
            notes: "Solo traveler, single room supplement included")).Value;
        _ = tours[4].AddBooking(TourBookingRequest.CreateSingle(
            customers[8].Id,
            customers[8].PhysicalInfo.BikeType,
            customers[8].AccommodationPreferences.RoomType,
            notes: "Payment pending bank transfer")).Value;

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

        var timeProvider = TimeProvider.System;

        EnsureSeedOperationSucceeded(tours[0].ConfirmBooking(booking1.Id));
        EnsureSeedOperationSucceeded(tours[0].RecordBookingPayment(
            booking1.Id,
            booking1.TotalPrice,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            timeProvider,
            "CC-2024-001"));

        EnsureSeedOperationSucceeded(tours[1].ConfirmBooking(booking2.Id));
        EnsureSeedOperationSucceeded(tours[1].RecordBookingPayment(
            booking2.Id,
            booking2.TotalPrice * 0.5m,
            DateTime.UtcNow,
            PaymentMethod.BankTransfer,
            timeProvider,
            "BT-2024-002",
            "50% deposit paid"));

        EnsureSeedOperationSucceeded(tours[3].ConfirmBooking(booking4.Id));
        EnsureSeedOperationSucceeded(tours[3].RecordBookingPayment(
            booking4.Id,
            booking4.TotalPrice,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            timeProvider,
            "CC-2024-003"));

        EnsureSeedOperationSucceeded(tours[4].ConfirmBooking(booking5.Id));
        EnsureSeedOperationSucceeded(tours[4].CompleteBooking(booking5.Id));
        EnsureSeedOperationSucceeded(tours[0].CancelBooking(booking6.Id));

        EnsureSeedOperationSucceeded(tours[1].ConfirmBooking(booking7.Id));
        EnsureSeedOperationSucceeded(tours[1].RecordBookingPayment(
            booking7.Id,
            booking7.TotalPrice * 0.75m,
            DateTime.UtcNow,
            PaymentMethod.BankTransfer,
            timeProvider,
            "BT-2024-004",
            "75% deposit paid"));

        EnsureSeedOperationSucceeded(tours[0].ConfirmBooking(booking9.Id));
        EnsureSeedOperationSucceeded(tours[0].RecordBookingPayment(
            booking9.Id,
            booking9.TotalPrice,
            DateTime.UtcNow,
            PaymentMethod.Cash,
            timeProvider));

        EnsureSeedOperationSucceeded(tours[4].ConfirmBooking(booking10.Id));

        EnsureSeedOperationSucceeded(tours[2].RecordBookingPayment(
            booking3.Id,
            booking3.TotalPrice * 0.25m,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            timeProvider,
            "CC-2024-006",
            "25% deposit paid"));

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

        var actualDuration = actual.Schedule.EndDate - actual.Schedule.StartDate;
        var expectedDuration = expected.Schedule.EndDate - expected.Schedule.StartDate;
        var actualSubDayTicks = actualDuration.Ticks - TimeSpan.FromDays(actualDuration.Days).Ticks;
        var expectedSubDayTicks = expectedDuration.Ticks - TimeSpan.FromDays(expectedDuration.Days).Ticks;
        var durationMatches = actualDuration.Days == expectedDuration.Days &&
            Math.Abs(actualSubDayTicks) < TimeSpan.TicksPerSecond &&
            Math.Abs(expectedSubDayTicks) < TimeSpan.TicksPerSecond;

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
