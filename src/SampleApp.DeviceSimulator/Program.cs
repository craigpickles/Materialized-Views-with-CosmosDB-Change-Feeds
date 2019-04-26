using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.DeviceSimulator
{
    class Program
    {
        private const string cosmoDBEndpoint = "";
        private const string cosmosDBKey = "";
        private const string database = "SampleApp";
        private const string eventsCollectionId = "events";
        static async Task Main(string[] args)
        {
            if (args.Length == 0) args = new[] { "10" };

            if (!int.TryParse(args[0], out int deviceCount) && deviceCount > 1)
            {
                Console.WriteLine("Please specify the nuber of devices you want to simulate. ie. 1-10");
                return;
            }

            var tasks = new List<Task>();
            var cancellationTokenSource = new CancellationTokenSource();

            var simulator = new Simulator(cosmoDBEndpoint, cosmosDBKey, database, eventsCollectionId, cancellationTokenSource.Token);

            await simulator.SetupIfNotExistsAsync();

            foreach (int i in Enumerable.Range(1, deviceCount))
            {
                tasks.Add(new Task(async () => await simulator.Run(i)));
            }

            tasks.ForEach(t => t.Start());

            Console.WriteLine("Press any key to exit the simulator");
            Console.ReadKey(true);

            cancellationTokenSource.Cancel();
            await Task.WhenAll(tasks.ToArray());

            Console.WriteLine("Done.");
        }
    }
}