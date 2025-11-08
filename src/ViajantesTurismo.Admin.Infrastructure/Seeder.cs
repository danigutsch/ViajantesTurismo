using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class Seeder(ApplicationDbContext dbContext) : ISeeder
{
    private static readonly Tour[] Tours =
    [
        Tour.Create(
            "CITY001",
            "City Highlights",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7),
            1500m,
            300m,
            100m,
            200m,
            Currency.Real,
            6,
            15,
            ["Hotel", "Breakfast", "City Tour"]
        ).Value,
        Tour.Create(
            "HIST002",
            "Historical Landmarks",
            DateTime.UtcNow.AddDays(4),
            DateTime.UtcNow.AddDays(10),
            2000m,
            400m,
            150m,
            250m,
            Currency.Euro,
            8,
            20,
            ["Hotel", "Breakfast", "Museum Tickets"]
        ).Value,
        Tour.Create(
            "CULT001",
            "Cultural Experience",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(13),
            1800m,
            350m,
            120m,
            220m,
            Currency.UsDollar,
            4,
            12,
            ["Hotel", "Breakfast", "Cultural Show"]
        ).Value,
        Tour.Create(
            "NATR001",
            "Nature and Adventure",
            DateTime.UtcNow.AddDays(11),
            DateTime.UtcNow.AddDays(17),
            2200m,
            450m,
            180m,
            280m,
            Currency.Real,
            5,
            18,
            ["Hotel", "Breakfast", "Hiking Tour"]
        ).Value,
        Tour.Create(
            "FOWI003",
            "Food and Wine Tour",
            DateTime.UtcNow.AddDays(16),
            DateTime.UtcNow.AddDays(22),
            2500m,
            500m,
            200m,
            300m,
            Currency.Euro,
            6,
            16,
            ["Hotel", "Breakfast", "Wine Tasting"]
        ).Value
    ];

    private static readonly Customer[] Customers =
    [
        new(
            PersonalInfo.Create("Alice", "Smith", "Female", new DateTime(1990, 1, 1).ToUniversalTime(), "Brazilian", "Engineer", TimeProvider.System).Value,
            IdentificationInfo.Create("123456789", "Brazilian").Value,
            ContactInfo.Create("alice@example.com", "+5511999999999", "@alice", "alice.fb").Value,
            Address.Create("Rua A, 123", "Apt 1", "Centro", "01234-567", "São Paulo", "SP", "Brazil").Value,
            PhysicalInfo.Create(60, 165, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value,
            EmergencyContact.Create("Bob Smith", "+5511988888888").Value,
            MedicalInfo.Create("Peanuts", null).Value
        ),
        new(
            PersonalInfo.Create("Bob", "Johnson", "Male", new DateTime(1985, 5, 15).ToUniversalTime(), "American", "Teacher", TimeProvider.System).Value,
            IdentificationInfo.Create("987654321", "American").Value,
            ContactInfo.Create("bob@example.com", "+15551234567", null, "bob.johnson").Value,
            Address.Create("456 Elm St", null, "Manhattan", "10001", "New York", "NY", "USA").Value,
            PhysicalInfo.Create(75, 180, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null).Value,
            EmergencyContact.Create("Jane Johnson", "+15559876543").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Carla", "Santos", "Female", new DateTime(1995, 10, 20).ToUniversalTime(), "Portuguese", "Doctor", TimeProvider.System).Value,
            IdentificationInfo.Create("456789123", "Portuguese").Value,
            ContactInfo.Create("carla@example.com", "+351912345678", "@carla_santos", null).Value,
            Address.Create("Rua B, 456", null, "Alfama", "1100-001", "Lisbon", "Lisbon", "Portugal").Value,
            PhysicalInfo.Create(55, 160, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value,
            EmergencyContact.Create("Pedro Santos", "+351987654321").Value,
            MedicalInfo.Create("Shellfish", null).Value
        ),
        new(
            PersonalInfo.Create("David", "Lee", "Male", new DateTime(1980, 3, 10).ToUniversalTime(), "Korean", "Chef", TimeProvider.System).Value,
            IdentificationInfo.Create("789123456", "Korean").Value,
            ContactInfo.Create("david@example.com", "+821012345678", null, "david.lee").Value,
            Address.Create("Gangnam-daero 789", null, "Gangnam-gu", "06234", "Seoul", "Seoul", "South Korea").Value,
            PhysicalInfo.Create(70, 175, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null).Value,
            EmergencyContact.Create("Sarah Lee", "+821098765432").Value,
            MedicalInfo.Create("Dairy", null).Value
        ),
        new(
            PersonalInfo.Create("Elena", "Rodriguez", "Female", new DateTime(1992, 7, 5).ToUniversalTime(), "Spanish", "Artist", TimeProvider.System).Value,
            IdentificationInfo.Create("321654987", "Spanish").Value,
            ContactInfo.Create("elena@example.com", "+34612345678", "@elena_art", null).Value,
            Address.Create("Calle C, 789", "Piso 2", "Centro", "28001", "Madrid", "Madrid", "Spain").Value,
            PhysicalInfo.Create(58, 168, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value,
            EmergencyContact.Create("Miguel Rodriguez", "+34698765432").Value,
            MedicalInfo.Create("Pollen", null).Value
        ),
        new(
            PersonalInfo.Create("Frank", "Muller", "Male", new DateTime(1975, 12, 25).ToUniversalTime(), "German", "Mechanic", TimeProvider.System).Value,
            IdentificationInfo.Create("654987321", "German").Value,
            ContactInfo.Create("frank@example.com", "+491512345678", null, "frank.muller").Value,
            Address.Create("Hauptstr. 101", null, "Mitte", "10117", "Berlin", "Berlin", "Germany").Value,
            PhysicalInfo.Create(80, 185, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null).Value,
            EmergencyContact.Create("Anna Muller", "+491598765432").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Gina", "Patel", "Female", new DateTime(1988, 9, 30).ToUniversalTime(), "Indian", "Accountant", TimeProvider.System).Value,
            IdentificationInfo.Create("147258369", "Indian").Value,
            ContactInfo.Create("gina@example.com", "+919876543210", "@gina_patel", null).Value,
            Address.Create("MG Road, 202", null, "Bandra", "400050", "Mumbai", "Maharashtra", "India").Value,
            PhysicalInfo.Create(62, 162, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value,
            EmergencyContact.Create("Raj Patel", "+919876543211").Value,
            MedicalInfo.Create("Nuts", null).Value
        ),
        new(
            PersonalInfo.Create("Hans", "Nielsen", "Male", new DateTime(1998, 4, 14).ToUniversalTime(), "Danish", "Student", TimeProvider.System).Value,
            IdentificationInfo.Create("963852741", "Danish").Value,
            ContactInfo.Create("hans@example.com", "+4520123456", null, "hans.nielsen").Value,
            Address.Create("Vesterbrogade 303", null, "Vesterbro", "1620", "Copenhagen", "Capital Region", "Denmark").Value,
            PhysicalInfo.Create(68, 178, BikeType.EBike).Value,
            AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null).Value,
            EmergencyContact.Create("Lise Nielsen", "+4520987654").Value,
            MedicalInfo.Create("Gluten", null).Value
        ),
        new(
            PersonalInfo.Create("Irina", "Petrov", "Female", new DateTime(1983, 11, 8).ToUniversalTime(), "Russian", "Scientist", TimeProvider.System).Value,
            IdentificationInfo.Create("852741963", "Russian").Value,
            ContactInfo.Create("irina@example.com", "+79123456789", "@irina_petrov", null).Value,
            Address.Create("Tverskaya Ulitsa, 404", null, "Tverskoy", "125009", "Moscow", "Moscow", "Russia").Value,
            PhysicalInfo.Create(56, 170, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value,
            EmergencyContact.Create("Alex Petrov", "+79234567890").Value,
            MedicalInfo.Create(null, null).Value
        ),
        new(
            PersonalInfo.Create("Jack", "Brown", "Male", new DateTime(1991, 6, 22).ToUniversalTime(), "Australian", "Photographer", TimeProvider.System).Value,
            IdentificationInfo.Create("741963852", "Australian").Value,
            ContactInfo.Create("jack@example.com", "+61412345678", null, "jack.brown").Value,
            Address.Create("Collins Street, 505", null, "CBD", "3000", "Melbourne", "Victoria", "Australia").Value,
            PhysicalInfo.Create(72, 182, BikeType.Regular).Value,
            AccommodationPreferences.Create(RoomType.DoubleRoom, BedType.DoubleBed, null).Value,
            EmergencyContact.Create("Emma Brown", "+61498765432").Value,
            MedicalInfo.Create("Seafood", null).Value
        )
    ];

    public void Seed()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var toursToAdd = Tours.Select(t => Tour.Create(
            t.Identifier,
            t.Name,
            t.Schedule.StartDate,
            t.Schedule.EndDate,
            t.Pricing.BasePrice,
            t.Pricing.DoubleRoomSupplementPrice,
            t.Pricing.RegularBikePrice,
            t.Pricing.EBikePrice,
            t.Pricing.Currency,
            t.Capacity.MinCustomers,
            t.Capacity.MaxCustomers,
            t.IncludedServices
        ).Value);

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

        var toursToAdd = Tours.Select(t => Tour.Create(
            t.Identifier,
            t.Name,
            t.Schedule.StartDate,
            t.Schedule.EndDate,
            t.Pricing.BasePrice,
            t.Pricing.DoubleRoomSupplementPrice,
            t.Pricing.RegularBikePrice,
            t.Pricing.EBikePrice,
            t.Pricing.Currency,
            t.Capacity.MinCustomers,
            t.Capacity.MaxCustomers,
            t.IncludedServices
        ).Value);

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

        var booking1 = tours[0].AddBooking(customers[0].Id, customers[0].PhysicalInfo.BikeType, null, null, customers[0].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Early bird discount applied").Value;
        var booking2 = tours[1].AddBooking(customers[1].Id, customers[1].PhysicalInfo.BikeType, customers[0].Id, customers[0].PhysicalInfo.BikeType, customers[1].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Traveling together as a couple").Value;
        tours[2].AddBooking(customers[2].Id, customers[2].PhysicalInfo.BikeType, null, null, customers[2].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Requested vegetarian meals");
        var booking4 = tours[3].AddBooking(customers[3].Id, customers[3].PhysicalInfo.BikeType, customers[4].Id, customers[4].PhysicalInfo.BikeType, customers[3].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Upgraded to premium accommodation").Value;
        var booking5 = tours[4].AddBooking(customers[5].Id, customers[5].PhysicalInfo.BikeType, null, null, customers[5].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Excellent tour experience").Value;
        var booking6 = tours[0].AddBooking(customers[6].Id, customers[6].PhysicalInfo.BikeType, null, null, customers[6].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Cancelled due to personal reasons").Value;
        var booking7 = tours[1].AddBooking(customers[7].Id, customers[7].PhysicalInfo.BikeType, customers[8].Id, customers[8].PhysicalInfo.BikeType, customers[7].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Special dietary requirements noted").Value;
        tours[3].AddBooking(customers[9].Id, customers[9].PhysicalInfo.BikeType, null, null, customers[9].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Interested in photography opportunities");
        var booking9 = tours[0].AddBooking(customers[4].Id, customers[4].PhysicalInfo.BikeType, null, null, customers[4].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Solo traveler, single room supplement included").Value;
        var booking10 = tours[4].AddBooking(customers[8].Id, customers[8].PhysicalInfo.BikeType, null, null, customers[8].AccommodationPreferences.RoomType, DiscountType.None, 0m, null, "Payment pending bank transfer").Value;

        dbContext.SaveChanges();

        _ = tours[0].UpdateBookingNotes(booking1.Id, "Early bird discount applied");
        _ = tours[0].ConfirmBooking(booking1.Id);
        _ = tours[0].UpdateBookingPaymentStatus(booking1.Id, PaymentStatus.Paid);

        _ = tours[1].UpdateBookingNotes(booking2.Id, "Traveling together as a couple");
        _ = tours[1].ConfirmBooking(booking2.Id);
        _ = tours[1].UpdateBookingPaymentStatus(booking2.Id, PaymentStatus.PartiallyPaid);

        _ = tours[3].UpdateBookingNotes(booking4.Id, "Upgraded to premium accommodation");
        _ = tours[3].ConfirmBooking(booking4.Id);
        _ = tours[3].UpdateBookingPaymentStatus(booking4.Id, PaymentStatus.Paid);

        _ = tours[4].CompleteBooking(booking5.Id);
        _ = tours[0].CancelBooking(booking6.Id);

        _ = tours[1].UpdateBookingNotes(booking7.Id, "Special dietary requirements noted");
        _ = tours[1].ConfirmBooking(booking7.Id);
        _ = tours[1].UpdateBookingPaymentStatus(booking7.Id, PaymentStatus.PartiallyPaid);

        _ = tours[0].UpdateBookingNotes(booking9.Id, "Solo traveler, single room supplement included");
        _ = tours[0].ConfirmBooking(booking9.Id);
        _ = tours[0].UpdateBookingPaymentStatus(booking9.Id, PaymentStatus.Paid);

        _ = tours[4].ConfirmBooking(booking10.Id);
    }
}