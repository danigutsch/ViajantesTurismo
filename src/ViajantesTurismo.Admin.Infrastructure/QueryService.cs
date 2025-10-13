using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class QueryService(ApplicationDbContext dbContext) : IQueryService
{
    public async Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct)
    {
        var tours = await dbContext.Tours.OrderBy(tour => tour.Id).ToListAsync(ct);
        return
        [
            ..tours.Select(tour => new GetTourDto()
            {
                Id = tour.Id,
                Identifier = tour.Identifier,
                Name = tour.Name,
                StartDate = tour.StartDate,
                EndDate = tour.EndDate,
                Price = tour.Price,
                SingleRoomSupplementPrice = tour.SingleRoomSupplementPrice,
                RegularBikePrice = tour.RegularBikePrice,
                EBikePrice = tour.EBikePrice,
                Currency = (CurrencyDto)tour.Currency,
                IncludedServices = [..tour.IncludedServices]
            })
        ];
    }

    public async Task<GetTourDto?> GetTourById(int id, CancellationToken ct)
    {
        var tour = await dbContext.Tours.FindAsync([id], ct);
        if (tour is null)
        {
            return null;
        }

        return new GetTourDto
        {
            Id = tour.Id,
            Identifier = tour.Identifier,
            Name = tour.Name,
            StartDate = tour.StartDate,
            EndDate = tour.EndDate,
            Price = tour.Price,
            SingleRoomSupplementPrice = tour.SingleRoomSupplementPrice,
            RegularBikePrice = tour.RegularBikePrice,
            EBikePrice = tour.EBikePrice,
            Currency = (CurrencyDto)tour.Currency,
            IncludedServices = [..tour.IncludedServices]
        };
    }

    public async Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct)
    {
        var customers = await dbContext.Customers.OrderBy(c => c.Id).ToListAsync(ct);
        return
        [
            ..customers.Select(c => new GetCustomerDto
            {
                Id = c.Id,
                FirstName = c.PersonalInfo.FirstName,
                LastName = c.PersonalInfo.LastName,
                Email = c.ContactInfo.Email,
                Mobile = c.ContactInfo.Mobile,
                Nationality = c.PersonalInfo.Nationality
            })
        ];
    }

    public async Task<CustomerDetailsDto?> GetCustomerDetailsById(int id, CancellationToken ct)
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
                Profession = customer.PersonalInfo.Profession
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
                BikeType = (BikeTypeDto)customer.PhysicalInfo.BikeType
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = (RoomTypeDto)customer.AccommodationPreferences.RoomType,
                BedType = (BedTypeDto)customer.AccommodationPreferences.BedType,
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
}