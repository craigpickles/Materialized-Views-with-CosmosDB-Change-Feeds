namespace SampleApp.DeviceSimulator
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Polly;
    using SampleApp.Events;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    public class Simulator
    {
        private string databaseId;
        private string collectionId;
        private CancellationToken token;
        private DocumentClient client;

        public Simulator(string cosmoDBEndpoint, string comsoDBAuthKey, string databaseId, string collectionId, CancellationToken token)
        {
            client = new DocumentClient(new Uri(cosmoDBEndpoint), comsoDBAuthKey);
            this.databaseId = databaseId;
            this.collectionId = collectionId;
            this.token = token;
        }

        public async Task SetupIfNotExistsAsync()
        {
            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseId });

            var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string>() { "/DeviceId" } };
            var documentCollection = await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId), 
                new DocumentCollection { Id = collectionId, PartitionKey = partitionKeyDefinition }, 
                new RequestOptions { OfferThroughput = 1000 });

            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseId), documentCollection);
        }

        public async Task Run(int deviceId)
        {

            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            var random = new Random();
            var count = 0;

            while (!token.IsCancellationRequested)
            {
                var deviceData = new DeviceSensorEvent
                {
                    DeviceId = deviceId.ToString("###"),
                    Value = 100 + random.NextDouble() * 100,
                    TimeStamp = DateTime.UtcNow.ToString("o")
                };

                Console.WriteLine(deviceData);

                var policy = Policy
                  .Handle<DocumentClientException>()
                  .Retry(3);

                _ = policy.Execute(async () =>
                  {
                      await client.CreateDocumentAsync(collectionUri, deviceData);
                      count++;
                  });

                if (count % 4 == 0)
                {
                    var batteryData = new DeviceBatteryEvent
                    {
                        DeviceId = deviceId.ToString("###"),
                        Value = Math.Round(random.NextDouble() * 100, 2),
                        TimeStamp = DateTime.UtcNow.ToString("o"),
                    };

                    Console.WriteLine(batteryData);

                    _ = policy.Execute(async () =>
                    {
                        await client.CreateDocumentAsync(collectionUri, batteryData);
                        count++;
                    });
                }

                await Task.Delay(random.Next(1000) + 1000);
            }
        }
    }
}
