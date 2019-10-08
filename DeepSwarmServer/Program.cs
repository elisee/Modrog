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

            using (var cancelTokenSource = new CancellationTokenSource())
            {
                serverState.Start();

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
            }
        }
    }
}
