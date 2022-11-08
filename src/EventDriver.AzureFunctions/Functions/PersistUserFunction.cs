using System;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using EventDriver.AzureFunctions.DataTransferObjects;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Azure;
using EventDriver.AzureFunctions.Entities;

namespace EventDriver.AzureFunctions.Functions;

public class PersistUserFunction
{
    [FunctionName("PersistUserFunction")]
    public async Task Run(
        [ServiceBusTrigger(Queues.Persistence, Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        [Table(Tables.Users)] TableClient usersClient,
        ILogger log)
    {
        var payloadString = Encoding.UTF8.GetString(message.Body);
        var payload = JsonConvert.DeserializeObject<RawUserData>(payloadString);

        if (payload != null)
        {
            log.LogInformation("Received user {userId} for import", payload.Id);
            var entity = new UserTableEntity
            {
                PartitionKey = "user",
                RowKey = payload.Id.ToString(),
                Address = $"{payload.Street} {payload.HouseNumber}",
                DateOfBirth = payload.DateOfBirth,
                SocialSecurityNumber = payload.SocialSecurityNumber,
                PostalCode = payload.PostalCode,
                City = payload.City,
                StateProvince = payload.StateProvince,
                Country = payload.Country,
                DisplayName = $"{payload.Lastname}, {payload.Firstname}",
                ETag = ETag.All,
                Timestamp = DateTimeOffset.UtcNow
            };
            log.LogInformation("Transformed user {userId} into user data entity", entity.RowKey);
            var response = await usersClient.UpsertEntityAsync(entity);
            if (response.Status == 204)
            {
                log.LogInformation("Successfully stored user in persistence");
            }
            else
            {
                log.LogError("Failed to store user {userId} in persistence", entity.RowKey);
            }
        }

    }
}