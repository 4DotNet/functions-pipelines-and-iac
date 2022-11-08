using Bogus;
using EventDriver.AzureFunctions.DataTransferObjects;

namespace EventDriver.AzureFunctions.Tests.Factories;

public static class RawUserDataFactory
{
    private static Faker<RawUserData> RawUserFakeData => ConfigureFakeData();

    public static RawUserData Create()
    {
        return RawUserFakeData.Generate();
    }
    public static List<RawUserData> Create(int count)
    {
        return RawUserFakeData.Generate(count);
    }

    private static Faker<RawUserData> ConfigureFakeData()
    {
        return new Faker<RawUserData>()
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Firstname, f => f.Person.FirstName)
            .RuleFor(p => p.Lastname, f => f.Person.LastName)
            .RuleFor(p => p.DateOfBirth, f => f.Person.DateOfBirth)
            .RuleFor(p => p.SocialSecurityNumber, f => f.Random.String2(12))
            .RuleFor(p => p.Street, f => f.Address.StreetName())
            .RuleFor(p => p.HouseNumber, f => f.Address.BuildingNumber())
            .RuleFor(p => p.PostalCode, f => f.Address.ZipCode())
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.StateProvince, f => f.Address.State())
            .RuleFor(p => p.Country, f => f.Address.Country());
    }

}