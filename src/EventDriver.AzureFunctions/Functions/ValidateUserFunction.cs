using System;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using EventDriver.AzureFunctions.DataTransferObjects;
using EventDriver.AzureFunctions.ExtensionMethods;

namespace EventDriver.AzureFunctions.Functions
{
    public class ValidateUserFunction
    {
        [FunctionName("ValidateUserFunction")]
        public async Task Run(
            [ServiceBusTrigger(Queues.Validation, Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
            [ServiceBus(Queues.Error, Connection = "ServiceBusConnection")] ServiceBusSender errorQueue,
            [ServiceBus(Queues.Persistence, Connection = "ServiceBusConnection")] ServiceBusSender persistenceQueue,
            string correlationId,
            ILogger log)
        {
            var payloadString = Encoding.UTF8.GetString(message.Body);
            var payload = JsonConvert.DeserializeObject<RawUserData>(payloadString);

            if (payload != null)
            {
                var age = DateTimeOffset.UtcNow - payload.DateOfBirth;
                if (age.TotalDays is > 18 * 365 and < 65 * 365)
                {
                    await persistenceQueue.SendMessageAsync(payload.ToServiceBusMessage(correlationId));
                }
                else
                {
                    await errorQueue.SendMessageAsync(payload.ToServiceBusMessage(correlationId));
                }
            }
        }
    }
}
