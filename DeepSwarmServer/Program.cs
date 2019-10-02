using System;
using System.Diagnostics;
using System.Threading;

namespace DeepSwarmServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Guid hostGuid = Guid.Empty;
            if (args.Length != 0 && !Guid.TryParse(args[0], out hostGuid)) throw new Exception("Failed to parse argument as host Guid.");

            var serverState = new ServerState(hostGuid);
            serverState.Start();

            var cancelTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cancelTokenSource.Cancel(); };

            var stopwatch = Stopwatch.StartNew();

            while (!cancelTokenSource.IsCancellationRequested)
            {
                var deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                serverState.Update(deltaTime);

                Thread.Sleep(1);
            }

            serverState.Stop();
            cancelTokenSource.Dispose();

            /*
            var random = new Random();

            var mapFilePath = Path.Combine(AppContext.BaseDirectory, "Map.dat");
            var map = new Map();

            if (!isNew && File.Exists(mapFilePath))
            {
                Console.WriteLine($"Loading map from {mapFilePath}...");
                map.LoadFromFile(mapFilePath);
                Console.WriteLine($"Done loading map.");
            }
            else
            {
                Console.WriteLine($"Generating map, saving to {mapFilePath}...");
                map.Generate();
                map.SaveToFile(mapFilePath);
                Console.WriteLine($"Done generating map.");
            }


            Console.WriteLine($"Saving map to {mapFilePath} before quitting...");
            map.SaveToFile(mapFilePath);
            Console.WriteLine("Map saved, quitting.");
            */
        }
    }
}
