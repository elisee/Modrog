using DeepSwarmCommon;
using System.Globalization;

namespace DeepSwarmClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var engine = new Engine();

            if (args.Length >= 1)
            {
                if (args[0] == "--scenario")
                {
                    engine.State.StartServer(args[1]);
                }
                else
                {
                    var pieces = args[0].Split(":");
                    var hostname = pieces[0];
                    var port = pieces.Length > 1 ? int.Parse(pieces[1], CultureInfo.InvariantCulture) : Protocol.Port;
                    var scenario = args.Length == 2 ? args[1] : null;
                    engine.State.Connect(hostname, port, scenario);
                }
            }

            engine.Run();
        }
    }
}
