namespace SampleApp.Functions
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using SampleApp.Events;

    public static class Function
    {
        private const string databaseId = "SampleApp";
        private const string eventsCollectionId = "events";
        private const string viewCollectionId = "view";

        [FunctionName("ViewProcessor")]
        public static async Task RunAsync(
            [CosmosDBTrigger(
                databaseName: databaseId,
                collectionName: eventsCollectionId,
                ConnectionStringSetting = "CosmosDBConnectionString",
                CreateLeaseCollectionIfNotExists = true)
            ]IReadOnlyList<Document> input,
            [CosmosDB(
                databaseName: databaseId,
                collectionName: viewCollectionId,
                ConnectionStringSetting = "CosmosDBConnectionString"
            )]DocumentClient client,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                await CreateViewCollectionIfNotExistsAsync(client);

                var processor = new ViewProcessor(client, databaseId, viewCollectionId, log);

                log.LogInformation($"Processing {input.Count} events");

                foreach (var p in input)
                {
                    var @event = DeviceEvent.FromDocument(p);

                    if (@event != null)
                    {
                        var tasks = new List<Task>();

                        tasks.Add(processor.UpdateViewAsync(@event));

                        // Update other views....

                        await Task.WhenAll(tasks);
                    }
                }
            }
        }

        private static async Task CreateViewCollectionIfNotExistsAsync(DocumentClient client)
        {
            var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string>() { "/DeviceId" } };
            var documentCollection = await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId),
                new DocumentCollection { Id = viewCollectionId, PartitionKey = partitionKeyDefinition },
                new RequestOptions { OfferThroughput = 1000 });

            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseId), documentCollection);
        }
    }
}
