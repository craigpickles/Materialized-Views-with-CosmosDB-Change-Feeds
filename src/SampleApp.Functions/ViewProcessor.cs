namespace SampleApp.Functions
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Logging;
    using Polly;
    using SampleApp.Events;

    public class ViewProcessor
    {
        private readonly string databaseId;
        private readonly string collectionId;
        private readonly DocumentClient client;
        private readonly ILogger log;

        public ViewProcessor(DocumentClient client, string databaseId, string collectionId, ILogger log)
        {
            this.databaseId = databaseId;
            this.collectionId = collectionId;
            this.client = client;
            this.log = log;
        }

        public async Task UpdateViewAsync(DeviceEvent @event)
        {
            log.LogInformation("Updating view");

            var optionsSingle = new RequestOptions { PartitionKey = new PartitionKey(@event.DeviceId) };

            var view = await GetViewAsync(@event.DeviceId, optionsSingle);
            if (view == null)
            {
                view = new DeviceMaterializedView
                {
                    DeviceId = @event.DeviceId
                };
            }

            if (@event is DeviceSensorEvent sensorEvent)
            {
                view.SensorMeasurements++;
                view.SensorAggregationSum += sensorEvent.Value;
                view.SensorLastValue = sensorEvent.Value;
                view.TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK");

                await UpsertDocumentAsync(view, optionsSingle);
            }
            else if (@event is DeviceBatteryEvent batteryEvent)
            {
                view.BatteryLevel = $"{batteryEvent.Value}%";
                view.TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK");

                await UpsertDocumentAsync(view, optionsSingle);
            }
        }
        private async Task<ResourceResponse<Document>> UpsertDocumentAsync(DeviceMaterializedView document, RequestOptions options)
        {
            var policy = Policy
                  .Handle<DocumentClientException>(e => { return e.StatusCode == HttpStatusCode.TooManyRequests; })
                  .WaitAndRetryAsync(3,
                    sleepDurationProvider: (retryAttempt, context) =>
                    {
                        if (context?.ContainsKey("RetryAfter") ?? false)
                            return (TimeSpan)context["RetryAfter"];

                        return TimeSpan.FromMilliseconds(10);
                    },
                    onRetryAsync: (exception, timespan, retryAttempt, context) =>
                    {
                        context["RetryAfter"] = (exception as DocumentClientException).RetryAfter;
                        log.LogWarning($"Waiting for {context["RetryAfter"]} msec...");
                        return Task.CompletedTask;
                    });

            return await policy.ExecuteAsync(async () =>
            {
                var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
                var result = await client.UpsertDocumentAsync(collectionUri, document, options);
                log.LogInformation($"{options.PartitionKey} RU Used: {result.RequestCharge:0.0}");
                return result;
            });
        }

        private async Task<DeviceMaterializedView> GetViewAsync(string deviceId, RequestOptions options)
        {
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, deviceId);

                log.LogInformation($"Materialized view: {documentUri.ToString()}");

                return await client.ReadDocumentAsync<DeviceMaterializedView>(documentUri, options);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != HttpStatusCode.NotFound)
                    throw ex;
            }

            return null;
        }
    }
}
