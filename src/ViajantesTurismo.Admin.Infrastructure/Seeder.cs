using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Common;
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
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Alice",
                LastName = "Smith",
                Gender = "Female",
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Nationality = "Brazilian",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "123456789",
                IdNationality = "Brazilian"
            },
            ContactInfo = new ContactInfo
            {
                Email = "alice@example.com",
                Mobile = "+5511999999999",
                Instagram = "@alice",
                Facebook = "alice.fb"
            },
            Address = new Address
            {
                Street = "Rua A, 123",
                Complement = "Apt 1",
                Neighborhood = "Centro",
                PostalCode = "01234-567",
                City = "São Paulo",
                State = "SP",
                Country = "Brazil"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 60,
                HeightCentimeters = 165,
                BikeType = BikeType.Regular
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.SingleRoom,
                BedType = BedType.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Bob Smith",
                Mobile = "+5511988888888"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Peanuts",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Bob",
                LastName = "Johnson",
                Gender = "Male",
                BirthDate = new DateTime(1985, 5, 15).ToUniversalTime(),
                Nationality = "American",
                Profession = "Teacher"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "987654321",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfo
            {
                Email = "bob@example.com",
                Mobile = "+15551234567",
                Instagram = null,
                Facebook = "bob.johnson"
            },
            Address = new Address
            {
                Street = "456 Elm St",
                Complement = null,
                Neighborhood = "Manhattan",
                PostalCode = "10001",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 75,
                HeightCentimeters = 180,
                BikeType = BikeType.EBike
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.DoubleRoom,
                BedType = BedType.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Jane Johnson",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = null,
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Carla",
                LastName = "Santos",
                Gender = "Female",
                BirthDate = new DateTime(1995, 10, 20).ToUniversalTime(),
                Nationality = "Portuguese",
                Profession = "Doctor"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "456789123",
                IdNationality = "Portuguese"
            },
            ContactInfo = new ContactInfo
            {
                Email = "carla@example.com",
                Mobile = "+351912345678",
                Instagram = "@carla_santos",
                Facebook = null
            },
            Address = new Address
            {
                Street = "Rua B, 456",
                Complement = null,
                Neighborhood = "Alfama",
                PostalCode = "1100-001",
                City = "Lisbon",
                State = "Lisbon",
                Country = "Portugal"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 55,
                HeightCentimeters = 160,
                BikeType = BikeType.Regular
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.SingleRoom,
                BedType = BedType.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Pedro Santos",
                Mobile = "+351987654321"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Shellfish",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "David",
                LastName = "Lee",
                Gender = "Male",
                BirthDate = new DateTime(1980, 3, 10).ToUniversalTime(),
                Nationality = "Korean",
                Profession = "Chef"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "789123456",
                IdNationality = "Korean"
            },
            ContactInfo = new ContactInfo
            {
                Email = "david@example.com",
                Mobile = "+821012345678",
                Instagram = null,
                Facebook = "david.lee"
            },
            Address = new Address
            {
                Street = "Gangnam-daero 789",
                Complement = null,
                Neighborhood = "Gangnam-gu",
                PostalCode = "06234",
                City = "Seoul",
                State = "Seoul",
                Country = "South Korea"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 70,
                HeightCentimeters = 175,
                BikeType = BikeType.None
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.DoubleRoom,
                BedType = BedType.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Sarah Lee",
                Mobile = "+821098765432"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Dairy",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Elena",
                LastName = "Rodriguez",
                Gender = "Female",
                BirthDate = new DateTime(1992, 7, 5).ToUniversalTime(),
                Nationality = "Spanish",
                Profession = "Artist"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "321654987",
                IdNationality = "Spanish"
            },
            ContactInfo = new ContactInfo
            {
                Email = "elena@example.com",
                Mobile = "+34612345678",
                Instagram = "@elena_art",
                Facebook = null
            },
            Address = new Address
            {
                Street = "Calle C, 789",
                Complement = "Piso 2",
                Neighborhood = "Centro",
                PostalCode = "28001",
                City = "Madrid",
                State = "Madrid",
                Country = "Spain"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 58,
                HeightCentimeters = 168,
                BikeType = BikeType.EBike
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.SingleRoom,
                BedType = BedType.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Miguel Rodriguez",
                Mobile = "+34698765432"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Pollen",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Frank",
                LastName = "Muller",
                Gender = "Male",
                BirthDate = new DateTime(1975, 12, 25).ToUniversalTime(),
                Nationality = "German",
                Profession = "Mechanic"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "654987321",
                IdNationality = "German"
            },
            ContactInfo = new ContactInfo
            {
                Email = "frank@example.com",
                Mobile = "+491512345678",
                Instagram = null,
                Facebook = "frank.muller"
            },
            Address = new Address
            {
                Street = "Hauptstr. 101",
                Complement = null,
                Neighborhood = "Mitte",
                PostalCode = "10117",
                City = "Berlin",
                State = "Berlin",
                Country = "Germany"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 80,
                HeightCentimeters = 185,
                BikeType = BikeType.Regular
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.DoubleRoom,
                BedType = BedType.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Anna Muller",
                Mobile = "+491598765432"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = null,
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Gina",
                LastName = "Patel",
                Gender = "Female",
                BirthDate = new DateTime(1988, 9, 30).ToUniversalTime(),
                Nationality = "Indian",
                Profession = "Accountant"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "147258369",
                IdNationality = "Indian"
            },
            ContactInfo = new ContactInfo
            {
                Email = "gina@example.com",
                Mobile = "+919876543210",
                Instagram = "@gina_patel",
                Facebook = null
            },
            Address = new Address
            {
                Street = "MG Road, 202",
                Complement = null,
                Neighborhood = "Bandra",
                PostalCode = "400050",
                City = "Mumbai",
                State = "Maharashtra",
                Country = "India"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 62,
                HeightCentimeters = 162,
                BikeType = BikeType.None
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.SingleRoom,
                BedType = BedType.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Raj Patel",
                Mobile = "+919876543211"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Nuts",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Hans",
                LastName = "Nielsen",
                Gender = "Male",
                BirthDate = new DateTime(1998, 4, 14).ToUniversalTime(),
                Nationality = "Danish",
                Profession = "Student"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "963852741",
                IdNationality = "Danish"
            },
            ContactInfo = new ContactInfo
            {
                Email = "hans@example.com",
                Mobile = "+4520123456",
                Instagram = null,
                Facebook = "hans.nielsen"
            },
            Address = new Address
            {
                Street = "Vesterbrogade 303",
                Complement = null,
                Neighborhood = "Vesterbro",
                PostalCode = "1620",
                City = "Copenhagen",
                State = "Capital Region",
                Country = "Denmark"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 68,
                HeightCentimeters = 178,
                BikeType = BikeType.EBike
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.DoubleRoom,
                BedType = BedType.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Lise Nielsen",
                Mobile = "+4520987654"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Gluten",
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Irina",
                LastName = "Petrov",
                Gender = "Female",
                BirthDate = new DateTime(1983, 11, 8).ToUniversalTime(),
                Nationality = "Russian",
                Profession = "Scientist"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "852741963",
                IdNationality = "Russian"
            },
            ContactInfo = new ContactInfo
            {
                Email = "irina@example.com",
                Mobile = "+79123456789",
                Instagram = "@irina_petrov",
                Facebook = null
            },
            Address = new Address
            {
                Street = "Tverskaya Ulitsa, 404",
                Complement = null,
                Neighborhood = "Tverskoy",
                PostalCode = "125009",
                City = "Moscow",
                State = "Moscow",
                Country = "Russia"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 56,
                HeightCentimeters = 170,
                BikeType = BikeType.Regular
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.SingleRoom,
                BedType = BedType.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Alex Petrov",
                Mobile = "+79234567890"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = null,
                AdditionalInfo = null
            }
        },
        new()
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "Jack",
                LastName = "Brown",
                Gender = "Male",
                BirthDate = new DateTime(1991, 6, 22).ToUniversalTime(),
                Nationality = "Australian",
                Profession = "Photographer"
            },
            IdentificationInfo = new IdentificationInfo
            {
                NationalId = "741963852",
                IdNationality = "Australian"
            },
            ContactInfo = new ContactInfo
            {
                Email = "jack@example.com",
                Mobile = "+61412345678",
                Instagram = null,
                Facebook = "jack.brown"
            },
            Address = new Address
            {
                Street = "Collins Street, 505",
                Complement = null,
                Neighborhood = "CBD",
                PostalCode = "3000",
                City = "Melbourne",
                State = "Victoria",
                Country = "Australia"
            },
            PhysicalInfo = new PhysicalInfo
            {
                WeightKg = 72,
                HeightCentimeters = 182,
                BikeType = BikeType.None
            },
            AccommodationPreferences = new AccommodationPreferences
            {
                RoomType = RoomType.DoubleRoom,
                BedType = BedType.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContact
            {
                Name = "Emma Brown",
                Mobile = "+61498765432"
            },
            MedicalInfo = new MedicalInfo
            {
                Allergies = "Seafood",
                AdditionalInfo = null
            }
        }
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

        var customersToAdd = Customers.Select(c => new Customer
        {
            PersonalInfo = c.PersonalInfo,
            IdentificationInfo = c.IdentificationInfo,
            ContactInfo = c.ContactInfo,
            Address = c.Address,
            PhysicalInfo = c.PhysicalInfo,
            AccommodationPreferences = c.AccommodationPreferences,
            EmergencyContact = c.EmergencyContact,
            MedicalInfo = c.MedicalInfo
        });

        dbContext.Customers.AddRange(customersToAdd);

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

        var customersToAdd = Customers.Select(c => new Customer
        {
            PersonalInfo = c.PersonalInfo,
            IdentificationInfo = c.IdentificationInfo,
            ContactInfo = c.ContactInfo,
            Address = c.Address,
            PhysicalInfo = c.PhysicalInfo,
            AccommodationPreferences = c.AccommodationPreferences,
            EmergencyContact = c.EmergencyContact,
            MedicalInfo = c.MedicalInfo
        });

        dbContext.Customers.AddRange(customersToAdd);

        await dbContext.SaveChangesAsync(ct);
    }
}
