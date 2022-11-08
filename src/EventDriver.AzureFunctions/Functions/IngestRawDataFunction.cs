using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs.Specialized;
using EventDriver.AzureFunctions.DataTransferObjects;
using EventDriver.AzureFunctions.ExtensionMethods;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventDriver.AzureFunctions.Functions;

public class IngestRawDataFunction
{
    [FunctionName("IngestRawDataFunction")]
    public async Task Run(
        [BlobTrigger("upload/{name}", Connection = "AzureWebJobsStorage")]
        BlockBlobClient myBlob,
        [ServiceBus(Queues.Validation, Connection = "ServiceBusConnection")] ServiceBusSender messageSender,
        string name, ILogger log)
    {
        try
        {
            var importProcessId = Guid.NewGuid();
            Activity.Current?.AddBaggage("ImportProcessId", importProcessId.ToString());

            log.LogInformation("Downloading blob from blob storage");
            var blob = await myBlob.DownloadAsync();
            log.LogInformation("Reading blob");
            using var sr = new StreamReader(blob.Value.Content);
            var jsonContent = await sr.ReadToEndAsync();
            log.LogInformation("Deserialize from JSON");
            var rawUserEntries = JsonConvert.DeserializeObject<List<RawUserData>>(jsonContent);
            if (rawUserEntries?.Count > 0)
            {
                var batch = await messageSender.CreateMessageBatchAsync();
                foreach (var entry in rawUserEntries)
                {
                    if (!batch.TryAddMessage(entry.ToServiceBusMessage(importProcessId.ToString())))
                    {
                        log.LogInformation("Sending batch of {count} users", batch.Count);
                        await messageSender.SendMessagesAsync(batch);
                        batch.Dispose();
                        batch = await messageSender.CreateMessageBatchAsync();
                        if (!batch.TryAddMessage(entry.ToServiceBusMessage(importProcessId.ToString())))
                        {
                            log.LogError("Failed to add message to service bus batch, at least one message is missing");
                        }
                    }

                }

                if (batch.Count > 0)
                {
                    log.LogInformation("Sending batch of {count} users", batch.Count);
                    await messageSender.SendMessagesAsync(batch);
                }
                log.LogInformation("Processing raw incoming data complete");
            }

        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to ingest users from BLOB storage");
        }


        var deleted = await myBlob.DeleteIfExistsAsync();
        log.LogInformation("Deleted BLOB succesfully: {result}", deleted.Value);
    }
    
}
