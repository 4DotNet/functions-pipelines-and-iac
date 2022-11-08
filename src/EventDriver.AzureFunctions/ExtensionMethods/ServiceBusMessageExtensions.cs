using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace EventDriver.AzureFunctions.ExtensionMethods;
    internal static class ServiceBusMessageExtensions
    {
            public static ServiceBusMessage ToServiceBusMessage(
                this object payload,
                string? correlationId = null)
            {
                return new ServiceBusMessage((ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)))
                {
                    CorrelationId = correlationId ?? Guid.NewGuid().ToString()
                };
            }
        }
