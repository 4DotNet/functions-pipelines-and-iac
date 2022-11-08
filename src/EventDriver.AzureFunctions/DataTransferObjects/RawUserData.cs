using System;

namespace EventDriver.AzureFunctions.DataTransferObjects;

public class RawUserData
{
    public Guid Id { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public DateTimeOffset DateOfBirth { get; set; }
    public string SocialSecurityNumber { get; set; }
    public string Street { get; set; }
    public string HouseNumber { get; set; }
    public string PostalCode { get; set; }
    public string City { get; set; }
    public string StateProvince { get; set; }
    public string Country { get; set; }
}