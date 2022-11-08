using System;
using Azure;
using Azure.Data.Tables;

namespace EventDriver.AzureFunctions.Entities
{
    internal class UserTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string DisplayName { get; set; }
        public DateTimeOffset DateOfBirth { get; set; }
        public string SocialSecurityNumber { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string Country { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
