using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class Seeder(ApplicationDbContext dbContext) : ISeeder
{
    private static readonly Tour[] Tours =
    [
        new(
            "CITY001",
            "City Highlights",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3),
            1500m,
            300m,
            100m,
            200m,
            Currency.Real,
            ["Hotel", "Breakfast", "City Tour"]
        ),
        new(
            "HIST002",
            "Historical Landmarks",
            DateTime.UtcNow.AddDays(4),
            DateTime.UtcNow.AddDays(6),
            2000m,
            400m,
            150m,
            250m,
            Currency.Euro,
            ["Hotel", "Breakfast", "Museum Tickets"]
        ),
        new(
            "CULT001",
            "Cultural Experience",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(10),
            1800m,
            350m,
            120m,
            220m,
            Currency.UsDollar,
            ["Hotel", "Breakfast", "Cultural Show"]
        ),
        new(
            "NATR001",
            "Nature and Adventure",
            DateTime.UtcNow.AddDays(11),
            DateTime.UtcNow.AddDays(15),
            2200m,
            450m,
            180m,
            280m,
            Currency.Real,
            ["Hotel", "Breakfast", "Hiking Tour"]
        ),
        new(
            "FOWI003",
            "Food and Wine Tour",
            DateTime.UtcNow.AddDays(16),
            DateTime.UtcNow.AddDays(20),
            2500m,
            500m,
            200m,
            300m,
            Currency.Euro,
            ["Hotel", "Breakfast", "Wine Tasting"]
        )
    ];

    private static readonly Customer[] Customers =
    [
        new(
            PersonalInfo.Create("Alice", "Smith", "Female", new DateTime(1990, 1, 1).ToUniversalTime(), "Brazilian", "Engineer", TimeProvider.System).Value,
            new IdentificationInfo("123456789", "Brazilian"),
            new ContactInfo("alice@example.com", "+5511999999999", "@alice", "alice.fb"),
            new Address("Rua A, 123", "Apt 1", "Centro", "01234-567", "São Paulo", "SP", "Brazil"),
            new PhysicalInfo(60, 165, BikeType.Regular),
            new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null),
            new EmergencyContact("Bob Smith", "+5511988888888"),
            new MedicalInfo("Peanuts", null)
        ),
        new(
            PersonalInfo.Create("Bob", "Johnson", "Male", new DateTime(1985, 5, 15).ToUniversalTime(), "American", "Teacher", TimeProvider.System).Value,
            new IdentificationInfo("987654321", "American"),
            new ContactInfo("bob@example.com", "+15551234567", null, "bob.johnson"),
            new Address("456 Elm St", null, "Manhattan", "10001", "New York", "NY", "USA"),
            new PhysicalInfo(75, 180, BikeType.EBike),
            new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null),
            new EmergencyContact("Jane Johnson", "+15559876543"),
            new MedicalInfo(null, null)
        ),
        new(
            PersonalInfo.Create("Carla", "Santos", "Female", new DateTime(1995, 10, 20).ToUniversalTime(), "Portuguese", "Doctor", TimeProvider.System).Value,
            new IdentificationInfo("456789123", "Portuguese"),
            new ContactInfo("carla@example.com", "+351912345678", "@carla_santos", null),
            new Address("Rua B, 456", null, "Alfama", "1100-001", "Lisbon", "Lisbon", "Portugal"),
            new PhysicalInfo(55, 160, BikeType.Regular),
            new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null),
            new EmergencyContact("Pedro Santos", "+351987654321"),
            new MedicalInfo("Shellfish", null)
        ),
        new(
            PersonalInfo.Create("David", "Lee", "Male", new DateTime(1980, 3, 10).ToUniversalTime(), "Korean", "Chef", TimeProvider.System).Value,
            new IdentificationInfo("789123456", "Korean"),
            new ContactInfo("david@example.com", "+821012345678", null, "david.lee"),
            new Address("Gangnam-daero 789", null, "Gangnam-gu", "06234", "Seoul", "Seoul", "South Korea"),
            new PhysicalInfo(70, 175, BikeType.None),
            new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null),
            new EmergencyContact("Sarah Lee", "+821098765432"),
            new MedicalInfo("Dairy", null)
        ),
        new(
            PersonalInfo.Create("Elena", "Rodriguez", "Female", new DateTime(1992, 7, 5).ToUniversalTime(), "Spanish", "Artist", TimeProvider.System).Value,
            new IdentificationInfo("321654987", "Spanish"),
            new ContactInfo("elena@example.com", "+34612345678", "@elena_art", null),
            new Address("Calle C, 789", "Piso 2", "Centro", "28001", "Madrid", "Madrid", "Spain"),
            new PhysicalInfo(58, 168, BikeType.EBike),
            new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null),
            new EmergencyContact("Miguel Rodriguez", "+34698765432"),
            new MedicalInfo("Pollen", null)
        ),
        new(
            PersonalInfo.Create("Frank", "Muller", "Male", new DateTime(1975, 12, 25).ToUniversalTime(), "German", "Mechanic", TimeProvider.System).Value,
            new IdentificationInfo("654987321", "German"),
            new ContactInfo("frank@example.com", "+491512345678", null, "frank.muller"),
            new Address("Hauptstr. 101", null, "Mitte", "10117", "Berlin", "Berlin", "Germany"),
            new PhysicalInfo(80, 185, BikeType.Regular),
            new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null),
            new EmergencyContact("Anna Muller", "+491598765432"),
            new MedicalInfo(null, null)
        ),
        new(
            PersonalInfo.Create("Gina", "Patel", "Female", new DateTime(1988, 9, 30).ToUniversalTime(), "Indian", "Accountant", TimeProvider.System).Value,
            new IdentificationInfo("147258369", "Indian"),
            new ContactInfo("gina@example.com", "+919876543210", "@gina_patel", null),
            new Address("MG Road, 202", null, "Bandra", "400050", "Mumbai", "Maharashtra", "India"),
            new PhysicalInfo(62, 162, BikeType.None),
            new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null),
            new EmergencyContact("Raj Patel", "+919876543211"),
            new MedicalInfo("Nuts", null)
        ),
        new(
            PersonalInfo.Create("Hans", "Nielsen", "Male", new DateTime(1998, 4, 14).ToUniversalTime(), "Danish", "Student", TimeProvider.System).Value,
            new IdentificationInfo("963852741", "Danish"),
            new ContactInfo("hans@example.com", "+4520123456", null, "hans.nielsen"),
            new Address("Vesterbrogade 303", null, "Vesterbro", "1620", "Copenhagen", "Capital Region", "Denmark"),
            new PhysicalInfo(68, 178, BikeType.EBike),
            new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null),
            new EmergencyContact("Lise Nielsen", "+4520987654"),
            new MedicalInfo("Gluten", null)
        ),
        new(
            PersonalInfo.Create("Irina", "Petrov", "Female", new DateTime(1983, 11, 8).ToUniversalTime(), "Russian", "Scientist", TimeProvider.System).Value,
            new IdentificationInfo("852741963", "Russian"),
            new ContactInfo("irina@example.com", "+79123456789", "@irina_petrov", null),
            new Address("Tverskaya Ulitsa, 404", null, "Tverskoy", "125009", "Moscow", "Moscow", "Russia"),
            new PhysicalInfo(56, 170, BikeType.Regular),
            new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null),
            new EmergencyContact("Alex Petrov", "+79234567890"),
            new MedicalInfo(null, null)
        ),
        new(
            PersonalInfo.Create("Jack", "Brown", "Male", new DateTime(1991, 6, 22).ToUniversalTime(), "Australian", "Photographer", TimeProvider.System).Value,
            new IdentificationInfo("741963852", "Australian"),
            new ContactInfo("jack@example.com", "+61412345678", null, "jack.brown"),
            new Address("Collins Street, 505", null, "CBD", "3000", "Melbourne", "Victoria", "Australia"),
            new PhysicalInfo(72, 182, BikeType.None),
            new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null),
            new EmergencyContact("Emma Brown", "+61498765432"),
            new MedicalInfo("Seafood", null)
        )
    ];

    public void Seed()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var toursToAdd = Tours.Select(t => new Tour(
            t.Identifier,
            t.Name,
            t.StartDate,
            t.EndDate,
            t.Price,
            t.SingleRoomSupplementPrice,
            t.RegularBikePrice,
            t.EBikePrice,
            t.Currency,
            t.IncludedServices
        ));

        dbContext.Tours.AddRange(toursToAdd);

        dbContext.SaveChanges();

        var customersToAdd = Customers.Select(c => new Customer(
            c.PersonalInfo,
            c.IdentificationInfo,
            c.ContactInfo,
            c.Address,
            c.PhysicalInfo,
            c.AccommodationPreferences,
            c.EmergencyContact,
            c.MedicalInfo
        ));

        dbContext.Customers.AddRange(customersToAdd);

        dbContext.SaveChanges();

        SeedBookings();

        dbContext.SaveChanges();
    }

    public async Task Seed(CancellationToken ct)
    {
        await dbContext.Database.EnsureDeletedAsync(ct);
        await dbContext.Database.EnsureCreatedAsync(ct);

        var toursToAdd = Tours.Select(t => new Tour(
            t.Identifier,
            t.Name,
            t.StartDate,
            t.EndDate,
            t.Price,
            t.SingleRoomSupplementPrice,
            t.RegularBikePrice,
            t.EBikePrice,
            t.Currency,
            t.IncludedServices
        ));

        dbContext.Tours.AddRange(toursToAdd);

        await dbContext.SaveChangesAsync(ct);

        var customersToAdd = Customers.Select(c => new Customer(
            c.PersonalInfo,
            c.IdentificationInfo,
            c.ContactInfo,
            c.Address,
            c.PhysicalInfo,
            c.AccommodationPreferences,
            c.EmergencyContact,
            c.MedicalInfo
        ));

        dbContext.Customers.AddRange(customersToAdd);

        await dbContext.SaveChangesAsync(ct);

        SeedBookings();

        await dbContext.SaveChangesAsync(ct);
    }

    private void SeedBookings()
    {
        var tours = dbContext.Tours.ToArray();
        var customers = dbContext.Customers.ToArray();

        if (tours.Length < 5 || customers.Length < 10)
        {
            return;
        }

        var booking1 = tours[0].AddBooking(customers[0].Id, null, 1800m, "Early bird discount applied");
        var booking2 = tours[1].AddBooking(customers[1].Id, customers[0].Id, 4400m, "Traveling together as a couple");
        tours[2].AddBooking(customers[2].Id, null, 1920m, "Requested vegetarian meals");
        var booking4 = tours[3].AddBooking(customers[3].Id, customers[4].Id, 4950m, "Upgraded to premium accommodation");
        var booking5 = tours[4].AddBooking(customers[5].Id, null, 2900m, "Excellent tour experience");
        var booking6 = tours[0].AddBooking(customers[6].Id, null, 1500m, "Cancelled due to personal reasons");
        var booking7 = tours[1].AddBooking(customers[7].Id, customers[8].Id, 4400m, "Special dietary requirements noted");
        tours[3].AddBooking(customers[9].Id, null, 2200m, "Interested in photography opportunities");
        var booking9 = tours[0].AddBooking(customers[4].Id, null, 1650m, "Solo traveler, single room supplement included");
        var booking10 = tours[4].AddBooking(customers[8].Id, null, 2750m, "Payment pending bank transfer");

        dbContext.SaveChanges();

        tours[0].UpdateBookingPrice(booking1.Id, 1800m);
        tours[0].UpdateBookingNotes(booking1.Id, "Early bird discount applied");
        _ = tours[0].ConfirmBooking(booking1.Id);
        _ = tours[0].UpdateBookingPaymentStatus(booking1.Id, PaymentStatus.Paid);

        tours[1].UpdateBookingPrice(booking2.Id, 4400m);
        tours[1].UpdateBookingNotes(booking2.Id, "Traveling together as a couple");
        _ = tours[1].ConfirmBooking(booking2.Id);
        _ = tours[1].UpdateBookingPaymentStatus(booking2.Id, PaymentStatus.PartiallyPaid);

        tours[3].UpdateBookingPrice(booking4.Id, 4950m);
        tours[3].UpdateBookingNotes(booking4.Id, "Upgraded to premium accommodation");
        _ = tours[3].ConfirmBooking(booking4.Id);
        _ = tours[3].UpdateBookingPaymentStatus(booking4.Id, PaymentStatus.Paid);

        tours[4].CompleteBooking(booking5.Id);
        _ = tours[0].CancelBooking(booking6.Id);

        tours[1].UpdateBookingPrice(booking7.Id, 4400m);
        tours[1].UpdateBookingNotes(booking7.Id, "Special dietary requirements noted");
        _ = tours[1].ConfirmBooking(booking7.Id);
        _ = tours[1].UpdateBookingPaymentStatus(booking7.Id, PaymentStatus.PartiallyPaid);

        tours[0].UpdateBookingPrice(booking9.Id, 1650m);
        tours[0].UpdateBookingNotes(booking9.Id, "Solo traveler, single room supplement included");
        _ = tours[0].ConfirmBooking(booking9.Id);
        _ = tours[0].UpdateBookingPaymentStatus(booking9.Id, PaymentStatus.Paid);

        _ = tours[4].ConfirmBooking(booking10.Id);
    }
}